using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DataServices.Core;
using DataServices.Database;


AccessRule isTrue = i => true;
AccessRule isFalse = i => false;
AuthenticationConfiguration authenticationConfiguration = new AuthenticationConfiguration(isFalse,isFalse,isTrue,isTrue,isTrue);

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .Build();

host.Run();
