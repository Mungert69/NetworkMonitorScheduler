using Microsoft.Extensions.Logging;
using Moq;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Scheduler.Services;
using Xunit;

namespace NetworkMonitorScheduler.Tests.Services;

public class MqttProcessorReadyTests
{
    private readonly Mock<IServiceState> _state = new();
    private readonly RabbitListener _listener;

    public MqttProcessorReadyTests()
    {
        _listener = new RabbitListener(_state.Object,
            Mock.Of<ILogger<RabbitListenerBase>>(),
            new SystemParams { ThisSystemUrl = new SystemUrl {
                RequirePublisherUserId = true, EnableMqttProcessorIngress = true
            } }, Mock.Of<IBackendMessageHmacService>());
    }

    [Fact]
    public void RejectsIncorrectAuthKey()
    {
        var result = _listener.ProcessorReady(new ProcessorInitObj {
            AppID = "user1-agent", AuthKey = "incorrect", IsProcessorReady = true
        }, mqttIngress: true);

        Assert.False(result.Success);
        _state.Verify(s => s.SetProcessorReady(It.IsAny<ProcessorObj>()), Times.Never);
    }

    [Fact]
    public void AcceptsCurrentAuthKeyWithoutPublisherUserId()
    {
        _state.Setup(s => s.HasCurrentProcessorAuthKey("user1-agent", "current"))
            .Returns(true);
        _state.Setup(s => s.SetProcessorReady(It.IsAny<ProcessorObj>()))
            .Returns(new ResultObj { Success = true, Message = "ready" });

        var result = _listener.ProcessorReady(new ProcessorInitObj {
            AppID = "user1-agent", AuthKey = "current", IsProcessorReady = true
        }, mqttIngress: true);

        Assert.True(result.Success);
        _state.Verify(s => s.SetProcessorReady(It.Is<ProcessorObj>(p =>
            p.AppID == "user1-agent" && p.IsReady)), Times.Once);
    }
}
