namespace Microsoft.Azure.SpaceFx.HostServices.Link;

public partial class MessageHandler<T> {
    private void LinkRequestHandler(MessageFormats.HostServices.Link.LinkRequest? message, MessageFormats.Common.DirectToApp fullMessage) {
        if (message == null) return;
        using (var scope = _serviceProvider.CreateScope()) {
            MessageFormats.HostServices.Link.LinkResponse returnResponse = new() { };

            _logger.LogInformation("Processing message type '{messageType}' from '{sourceApp}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, fullMessage.SourceAppId, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            returnResponse = new() {
                ResponseHeader = new() {
                    TrackingId = message.RequestHeader.TrackingId,
                    CorrelationId = message.RequestHeader.CorrelationId,
                    Status = MessageFormats.Common.StatusCodes.Pending
                }
            };

            _logger.LogDebug("Passing message '{messageType}' to plugins (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            MessageFormats.HostServices.Link.LinkRequest? pluginResult =
               _pluginLoader.CallPlugins<MessageFormats.HostServices.Link.LinkRequest?, Plugins.PluginBase>(
                   orig_request: message,
                   pluginDelegate: _pluginDelegates.LinkRequest);


            _logger.LogDebug("Plugins finished processing '{messageType}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            // Update the request if our plugins changed it
            if (pluginResult == null) {
                _logger.LogInformation("Plugins nullified '{messageType}'.  Dropping Message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);
                return;
            }

            returnResponse.LinkRequest = pluginResult;

            _logger.LogDebug("Passing '{messageType}' to FileMoverService for processing. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.RequestHeader.TrackingId, message.RequestHeader.CorrelationId);

            _fileMoverService.QueueFileMove(returnResponse);


            _logger.LogDebug("Sending response type '{messageType}' to '{appId}'  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", returnResponse.GetType().Name, fullMessage.SourceAppId, returnResponse.ResponseHeader.TrackingId, returnResponse.ResponseHeader.CorrelationId);

            // Route the response back to the calling user
            _client.DirectToApp(appId: fullMessage.SourceAppId, message: returnResponse);
        };
    }
}
