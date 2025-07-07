namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Extensions.Hosting;

public class CacheInitializerHostedService : IHostedService
{
    private readonly ITransformDataLookupFacade _facade;

    public CacheInitializerHostedService(ITransformDataLookupFacade facade)
    {
        _facade = facade;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _facade.InitAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}