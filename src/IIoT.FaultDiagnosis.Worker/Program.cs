using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using IIoT.FaultDiagnosis.Application;
using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Infrastructure;
using IIoT.FaultDiagnosis.Protocols;
using IIoT.FaultDiagnosis.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
builder.Services.Configure<CollectorOptions>(builder.Configuration.GetSection(CollectorOptions.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProtocols();
builder.Services.AddHostedService<CollectorWorker>();

await builder.Build().RunAsync();
