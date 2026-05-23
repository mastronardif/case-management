using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingWorker : BackgroundService
{
    private readonly BillingRunner _runner;
    private readonly BillingRunOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<BillingWorker> _logger;

    public BillingWorker(
        BillingRunner runner,
        BillingRunOptions options,
        IHostApplicationLifetime lifetime,
        ILogger<BillingWorker> logger)
    {
        _runner = runner;
        _options = options;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Billing Worker started. Mode: {Mode}", _options.Mode);

        if (_options.Mode == BillingRunMode.SingleRun)
        {
            try
            {
                await _runner.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing run failed");
            }
            finally
            {
                _lifetime.StopApplication();
            }
            return;
        }

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