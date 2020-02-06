using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Quartz.HostedService
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;

        public QuartzHostedService(IScheduler scheduler, ILogger<QuartzHostedService> logger = null)
        {
            if (logger == null)
            {
                _logger = new NullLogger<QuartzHostedService>();
            }
            else
            {
                _logger = logger;
            }
            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Quartz started...");
            await _scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Quartz stopped...");
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}