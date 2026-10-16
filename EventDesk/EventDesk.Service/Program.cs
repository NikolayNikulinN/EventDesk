using EventDesk.Constants;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Logger.Wrappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = ServiceConstants.ServiceDisplayName);
builder.Services.AddSingleton<ILoggerWrapper, LoggerWrapper>();

using IHost host = builder.Build();
ILoggerWrapper logger = host.Services.GetRequiredService<ILoggerWrapper>();

try
{
    logger.Write(ServiceConstants.ServiceStarting);
    await host.RunAsync();
}
catch (Exception exception)
{
    logger.Write(exception, LogLevel.Fatal, ServiceConstants.ServiceStoppedUnexpectedly);
    throw;
}
finally
{
    logger.Write(ServiceConstants.ServiceStopping);
    logger.CloseAndFlush();
}
