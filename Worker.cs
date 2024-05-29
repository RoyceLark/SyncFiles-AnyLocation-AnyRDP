using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace FileSync
{
    public class WorkerService : BackgroundService
    {

        private readonly ILogger<WorkerService> _logger;
        /// <summary>
        /// initiate Logger
        /// </summary>
        /// <param name="logger"></param>
        public WorkerService(ILogger<WorkerService> logger)
        {
            _logger = logger;
        }



        /// <summary>
        /// IStarted Executing Worker Service
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine(string.Format("BackgroundService initiated 1 minuts Interval.......: {0}", DateTimeOffset.Now));

               
                // please add your user name and password for it.

                new ConnectRDPToSyncFiles(_logger).GetFilesFromRDP("userName",
                                                                "password",
                                                                "domain",
                                                                "remote Ip address");
               

                var periodTimeSpan = TimeSpan.FromMinutes(1);
                await Task.Delay(periodTimeSpan, stoppingToken);
            }
        }

        /// <summary>
        /// Start Background Service
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundService Starting up.......: {time}", DateTimeOffset.Now);
            Console.WriteLine(string.Format("BackgroundService Starting up.......: {0}", DateTimeOffset.Now));
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Stop Background Service
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundService Stopping.......: {time}", DateTimeOffset.Now);
            Console.WriteLine(string.Format("BackgroundService Stopping.......: {0}", DateTimeOffset.Now));
            return base.StopAsync(cancellationToken);
        }

    }
}
