namespace reactive;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                // my guess is that now this will want to emit an event so that our subscribed service is able to 
                // grab it, and 'process' that event.
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
