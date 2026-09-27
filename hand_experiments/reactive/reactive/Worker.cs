namespace reactive;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    Worker(StateService stateService)
    {
        var _stateService = stateService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _stateService.Publish();  
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
