using ASPNETCoreScheduler.BackgroundService;
using Microsoft.Extensions.DependencyInjection;
using NCrontab;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitorScheduler
{
    public abstract class ScheduledProcessor : ScopedProcessor
    {
        private CrontabSchedule _schedule;
        private DateTime _nextRun;

        public int RunScheduleInterval()
        {
            // Return number of milliseconds until between runs.
            DateTime now = DateTime.UtcNow;
            DateTime next = _schedule.GetNextOccurrence(now);
            DateTime first=_schedule.GetNextOccurrence(next);
            DateTime second=_schedule.GetNextOccurrence(first);
            // difference between first and second in milliseconds
            int diff = (int)(second - first).TotalMilliseconds;
            return diff;    

        }
       
  
        public ScheduledProcessor(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
        }

        protected void updateSchedule(string newSchedule)
        {
            _schedule = CrontabSchedule.Parse(newSchedule);
              _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                DateTime now = DateTime.UtcNow;
                if (now > _nextRun)
                {
                    await Process();
                    _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
                }
                await Task.Delay(5000, stoppingToken); //5 seconds delay
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}
