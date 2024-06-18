using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link;

namespace Microsoft.Azure.SpaceFx.HostServices.Link.Plugins;
public class IntegrationTestPlugin : Microsoft.Azure.SpaceFx.HostServices.Link.Plugins.PluginBase {

    public override ILogger Logger { get; set; }

    public IntegrationTestPlugin() {
        LoggerFactory loggerFactory = new();
        Logger = loggerFactory.CreateLogger<IntegrationTestPlugin>();
    }

    public override Task BackgroundTask() => Task.Run(async () => {
        Logger.LogInformation("Started background task!");
    });

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => Logger = loggerFactory.CreateLogger<IntegrationTestPlugin>();

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() => Task.FromResult(new MessageFormats.Common.PluginHealthCheckResponse());

    public override Task<LinkRequest?> LinkRequest(LinkRequest? input_request) => Task.FromResult(input_request);

    public override Task<(LinkRequest?, LinkResponse?)> LinkResponse(LinkRequest? input_request, LinkResponse? input_response) => Task.Run(() => {
        if (input_request == null || input_response == null) return (input_request, input_response);

        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;

        return (input_request, input_response);
    });
}
