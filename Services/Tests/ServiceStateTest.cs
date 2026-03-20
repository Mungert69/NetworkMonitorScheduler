using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Utils.Helpers;
using Xunit;

namespace NetworkMonitorScheduler.Tests.Services
{
    public class ServiceStateTests
    {
        private readonly Mock<ILogger<ServiceState>> _loggerMock = new Mock<ILogger<ServiceState>>();
        private readonly Mock<IConfiguration> _configMock = new Mock<IConfiguration>();
        private readonly Mock<IRabbitRepo> _rabbitRepoMock = new Mock<IRabbitRepo>();
        private readonly Mock<ISystemParamsHelper> _systemParamsHelperMock = new Mock<ISystemParamsHelper>();
        private readonly Mock<IProcessorState> _processorStateMock = new Mock<IProcessorState>();
        private readonly Mock<IFileRepo> _fileRepoMock = new Mock<IFileRepo>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private ServiceState CreateServiceState()
        {
            // Setup minimal config and system params
            _configMock.Setup(c => c[It.IsAny<string>()]).Returns("* * * * *");
            _systemParamsHelperMock.Setup(s => s.GetSystemParams()).Returns(new SystemParams
            {
                PublicIPAddress = "127.0.0.1",
                ThisSystemUrl = new SystemUrl { ExternalUrl = "http://localhost" },
                SystemEmail = "test@example.com",
                SystemPassword = "password",
                SystemUser = "user",
                MailServerPort = 25,
                MailServerUseSSL = false,
                MailServer = "smtp.example.com"
            });
            _fileRepoMock.Setup(f => f.CheckFileExistsWithCreateObject<List<ProcessorObj>>(It.IsAny<string>(), It.IsAny<List<ProcessorObj>>(), It.IsAny<ILogger>()));
            _fileRepoMock.Setup(f => f.GetStateJson<List<ProcessorObj>>(It.IsAny<string>())).Returns(new List<ProcessorObj>());
            _processorStateMock.Setup(p => p.EnabledProcessorList(true)).Returns(new List<ProcessorObj>());
            _processorStateMock.Setup(p => p.EnabledSendAlertProcessorList(true)).Returns(new List<ProcessorObj>());
            _processorStateMock.Setup(p => p.ResetConcurrentProcessorList(It.IsAny<List<ProcessorObj>>()));
            _processorStateMock.Setup(p => p.SetAllProcessorObjsIsReportSent(It.IsAny<bool>()));
            _processorStateMock.Setup(p => p.SetProcessorObjIsReportSent(It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            return new ServiceState(
                _loggerMock.Object,
                _configMock.Object,
                _cts,
                _rabbitRepoMock.Object,
                _systemParamsHelperMock.Object,
                _processorStateMock.Object,
                _fileRepoMock.Object
            );
        }

        [Fact]
        public void IsAlertServiceReady_Setter_UpdatesState()
        {
            var serviceState = CreateServiceState();
            serviceState.IsAlertServiceReady = false;
            Assert.False(serviceState.IsAlertServiceReady);
            serviceState.IsAlertServiceReady = true;
            Assert.True(serviceState.IsAlertServiceReady);
        }

        [Fact]
        public void ResetReportSent_ResetsFlagsAndReturnsSuccess()
        {
            var serviceState = CreateServiceState();
            var result = serviceState.ResetReportSent();
            Assert.True(result.Success);
            Assert.Contains("Reset Report Sent", result.Message);
        }

        [Fact]
        public void SetProcessorReady_ReturnsErrorIfProcessorNotFound()
        {
            var serviceState = CreateServiceState();
            var proc = new ProcessorObj { AppID = "notfound", IsReady = true };
            _processorStateMock.Setup(p => p.SetProcessorObjIsReady(proc.AppID, proc.IsReady)).Returns(false);
            var result = serviceState.SetProcessorReady(proc);
            Assert.False(result.Success);
        }

        [Fact]
        public void SetProcessorReady_ReturnsSuccessIfProcessorFound()
        {
            var serviceState = CreateServiceState();
            var proc = new ProcessorObj { AppID = "found", IsReady = true };
            _processorStateMock.Setup(p => p.SetProcessorObjIsReady(proc.AppID, proc.IsReady)).Returns(true);

            // Safely get the private field and set up the dictionary
            var field = typeof(ServiceState).GetField("_processorStateChanges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            var dict = field.GetValue(serviceState) as Dictionary<string, List<DateTime>>;
            Assert.NotNull(dict);
            dict!["found"] = new List<DateTime>();

            var result = serviceState.SetProcessorReady(proc);

            Assert.True(result.Success);
            // Assert that a new DateTime was added to the list for "found"
            Assert.True(dict["found"].Count > 0);
            Assert.True((DateTime.UtcNow - dict["found"].Last()).TotalSeconds < 5); // The timestamp should be recent
        }

        [Fact]
        public void SetProcessorReady_CreatesStateChangeEntryWhenMissing()
        {
            var serviceState = CreateServiceState();
            var proc = new ProcessorObj { AppID = "new-app-id", IsReady = true };
            _processorStateMock.Setup(p => p.SetProcessorObjIsReady(proc.AppID, proc.IsReady)).Returns(true);

            var result = serviceState.SetProcessorReady(proc);

            Assert.True(result.Success);

            var field = typeof(ServiceState).GetField("_processorStateChanges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);
            var dict = field.GetValue(serviceState) as Dictionary<string, List<DateTime>>;
            Assert.NotNull(dict);
            Assert.True(dict!.ContainsKey(proc.AppID));
            Assert.True(dict[proc.AppID].Count > 0);
        }

        [Fact]
        public void IsSystemProcessor_ReturnsTrueForSystemProcessor()
        {
            var serviceState = CreateServiceState();
            var processor = new ProcessorObj { AppID = "system", IsPrivate = false };
            _processorStateMock.Setup(p => p.GetProcessorFromID("system", false)).Returns(processor);

            var isSystem = serviceState.IsSystemProcessor("system");

            Assert.True(isSystem);
        }

        [Fact]
        public void IsSystemProcessor_ReturnsFalseForUserProcessorOrMissing()
        {
            var serviceState = CreateServiceState();
            var processor = new ProcessorObj { AppID = "user", IsPrivate = true };
            _processorStateMock.Setup(p => p.GetProcessorFromID("user", false)).Returns(processor);

            Assert.False(serviceState.IsSystemProcessor("user"));
            Assert.False(serviceState.IsSystemProcessor("missing"));
        }

        [Fact]
        public void SendHealthReport_ReturnsResult()
        {
            var serviceState = CreateServiceState();
            var result = serviceState.SendHealthReport("test report");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CheckHealth_ReturnsResult()
        {
            var serviceState = CreateServiceState();
            var result = await serviceState.CheckHealth();
            Assert.NotNull(result);
        }
    }
}
