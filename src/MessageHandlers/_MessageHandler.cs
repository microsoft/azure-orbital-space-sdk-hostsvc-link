namespace Microsoft.Azure.SpaceFx.HostServices.Link;

public partial class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Utils.PluginDelegates _pluginDelegates;
    private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    private readonly Core.Client _client;
    private readonly Models.APP_CONFIG _appConfig;
    private readonly Services.FileMoverService _fileMoverService;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Utils.PluginDelegates pluginDelegates, Microsoft.Azure.SpaceFx.Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider, Core.Client client) {
        _logger = logger;
        _pluginDelegates = pluginDelegates;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
        _appConfig = new Models.APP_CONFIG();
        _client = client;
        _fileMoverService = _serviceProvider.GetRequiredService<Services.FileMoverService>();
    }

    public void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage) => Task.Run(() => {
        try {
            using (var scope = _serviceProvider.CreateScope()) {

            if (message == null || EqualityComparer<T>.Default.Equals(message, default)) {
                _logger.LogInformation("Received empty message '{messageType}' from '{appId}'.  Discarding message.", typeof(T).Name, fullMessage.SourceAppId);
                return;
            }

            switch (typeof(T).Name) {
                case string messageType when messageType.Equals(typeof(MessageFormats.HostServices.Link.LinkRequest).Name, StringComparison.CurrentCultureIgnoreCase):
                    LinkRequestHandler(message: message as MessageFormats.HostServices.Link.LinkRequest, fullMessage: fullMessage);
                    break;
            }   
            }
        }
        catch (Exception ex) {
            var no_error = true;
        }
        
    });
}
