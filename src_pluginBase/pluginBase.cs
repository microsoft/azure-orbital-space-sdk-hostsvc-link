namespace Microsoft.Azure.SpaceFx.HostServices.Link.Plugins;
public abstract class PluginBase : Core.IPluginBase, IPluginBase {
    public abstract ILogger Logger { get; set; }
    public abstract Task BackgroundTask();
    public abstract void ConfigureLogging(ILoggerFactory loggerFactory);
    public abstract Task<PluginHealthCheckResponse> PluginHealthCheckResponse();

    // Link Service Stuff
    public abstract Task<MessageFormats.HostServices.Link.LinkRequest?> LinkRequest(MessageFormats.HostServices.Link.LinkRequest? input_request);
    public abstract Task<(MessageFormats.HostServices.Link.LinkRequest?, MessageFormats.HostServices.Link.LinkResponse?)> LinkResponse(MessageFormats.HostServices.Link.LinkRequest? input_request, MessageFormats.HostServices.Link.LinkResponse? input_response);
}

public interface IPluginBase {
    ILogger Logger { get; set; }
    Task<MessageFormats.HostServices.Link.LinkRequest?> LinkRequest(MessageFormats.HostServices.Link.LinkRequest? input_request);
    Task<(MessageFormats.HostServices.Link.LinkRequest?, MessageFormats.HostServices.Link.LinkResponse?)> LinkResponse(MessageFormats.HostServices.Link.LinkRequest? input_request, MessageFormats.HostServices.Link.LinkResponse? input_response);
}
