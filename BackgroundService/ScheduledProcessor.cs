using Microsoft.Extensions.DependencyInjection;
using NCrontab;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor.BackgroundService
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
            DateTime first = _schedule.GetNextOccurrence(next);
            DateTime second = _schedule.GetNextOccurrence(first);
            // difference between first and second in milliseconds
            int diff = (int)(second - first).TotalMilliseconds;
            return diff;

        }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ScheduledProcessor(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
        }

        protected void updateSchedule(string newSchedule)
        {
            _schedule = CrontabSchedule.Parse(newSchedule);
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan delay = _nextRun - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await Process();
                    _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
                }
            }
        }
    }
}
