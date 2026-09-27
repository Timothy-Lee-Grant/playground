using System;

namespace listener1;

public class Listener(ILogger<Listener> logger) : BackgroundService
{
    private Listener(StateService stateService)
    {
        var _stateService = stateService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // We don't need this while loop anymore. I now want a way to be able to subscribe to that delegate

        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     if (logger.IsEnabled(LogLevel.Information))
        //     {
        //         logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        //     }
        //     await Task.Delay(1000, stoppingToken);
        // }

        _stateService.OnEventTrigger += myTriggerFunction;
    }

    private void myTriggerFunction(string message)
    {
        Console.WriteLine($"I have been invoked with {message}");
    }
}
