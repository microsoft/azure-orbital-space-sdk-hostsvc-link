namespace Microsoft.Azure.SpaceFx.HostServices.Link;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("/workspaces/hostsvc-link-config/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("/workspaces/hostsvc-link/src/appsettings.json", optional: true, reloadOnChange: true)
                             .AddJsonFile("/workspaces/hostsvc-link/src/appsettings.{env:DOTNET_ENVIRONMENT}.json", optional: true, reloadOnChange: true)
                             .Build();


        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.HostServices.Link.LinkRequest>, MessageHandler<MessageFormats.HostServices.Link.LinkRequest>>();

            services.AddSingleton<Utils.PluginDelegates>();

            services.AddSingleton<Services.FileMoverService>();
            services.AddHostedService<Services.FileMoverService>(p => p.GetRequiredService<Services.FileMoverService>());

        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
            logging.AddConsole();
        });

        var app = builder.Build();

        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Microsoft.Azure.SpaceFx.Core.Services.MessageReceiver>();
            endpoints.MapGrpcHealthChecksService();
            endpoints.MapGet("/", async context => {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            });
        });

        // Add a middleware to catch exceptions and stop the host gracefully
        app.Use(async (context, next) => {
            try {
                await next.Invoke();
            } catch (Exception ex) {
                Console.Error.WriteLine($"Triggering shutdown due to exception caught in global exception handler.  Error: {ex.Message}.  Stack Trace: {ex.StackTrace}");

                // Stop the host gracefully so it triggers the pod to error
                var lifetime = context.RequestServices.GetService<IHostApplicationLifetime>();
                lifetime?.StopApplication();
            }
        });

        app.Run();
    }
}