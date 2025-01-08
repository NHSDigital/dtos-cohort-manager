using Microsoft.Extensions.Hosting;
using DataServices.Core;
using DataServices.Database;

AccessRule isTrue = i => true;
AccessRule isFalse = i => false;
AuthenticationConfiguration authenticationConfiguration = new AuthenticationConfiguration(isFalse, isFalse, isTrue, isTrue, isTrue);

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .Build();

await host.RunAsync();
