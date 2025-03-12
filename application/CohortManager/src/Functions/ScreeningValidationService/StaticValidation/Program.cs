using Common;
using Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.StaticValidation;

var host = new HostBuilder()
    .AddConfiguration<StaticValidationConfig>(out StaticValidationConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRules, ReadRules>();
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
