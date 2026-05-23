using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingWorker : BackgroundService
{
    private readonly BillingRunner _runner;
    private readonly ILogger<BillingWorker> _logger;

    public BillingWorker(BillingRunner runner, ILogger<BillingWorker> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Billing Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _runner.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing cycle failed");
            }

            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        }
    }
}