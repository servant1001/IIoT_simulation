using Serilog;
using IIoT.FaultDiagnosis.Application;
using IIoT.FaultDiagnosis.Infrastructure;
using IIoT.FaultDiagnosis.Protocols;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProtocols();

var app = builder.Build();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program { }
