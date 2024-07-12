
namespace Microsoft.Azure.SpaceFx.HostServices.Link;

public partial class Services {
    public class FileMoverService : BackgroundService {
        private readonly ILogger<FileMoverService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
        private readonly Utils.PluginDelegates _pluginDelegates;
        private readonly BlockingCollection<MessageFormats.HostServices.Link.LinkResponse> _linkResponseQueue;
        private readonly Models.APP_CONFIG _appConfig;
        private readonly Core.Client _client;
        private string _all_xfer_dir = string.Empty;
        private readonly string PLATFORM_MTS_ID = $"platform-{nameof(MessageFormats.Common.PlatformServices.Mts).ToLower()}";
        private readonly JsonSerializerOptions jsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, PropertyNameCaseInsensitive = true, MaxDepth = 100, WriteIndented = true };
        public FileMoverService(ILogger<FileMoverService> logger, IServiceProvider serviceProvider, Utils.PluginDelegates pluginDelegates, Core.Services.PluginLoader pluginLoader, Core.Client client) {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _pluginDelegates = pluginDelegates;
            _pluginLoader = pluginLoader;
            _appConfig = new Models.APP_CONFIG();
            _linkResponseQueue = new();
            _client = client;
            _all_xfer_dir = client.GetXFerDirectories().Result.root_directory.Replace("xfer", "allxfer");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                using (var scope = _serviceProvider.CreateScope()) {
                    while (_linkResponseQueue.Count > 0) {
                        MessageFormats.HostServices.Link.LinkResponse linkResponse = _linkResponseQueue.Take();
                        try {

                            if (linkResponse.LinkRequest.LinkType.Equals(MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.Downlink)) {
                                linkResponse.LinkRequest.DestinationAppId = PLATFORM_MTS_ID;
                            }
                            
                            // We're missing a value - send the response as a rejection
                            if (string.IsNullOrWhiteSpace(linkResponse.LinkRequest.DestinationAppId)) {
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InvalidArgument;
                                linkResponse.ResponseHeader.Message = "Missing DestinationAppId.  Unable to process LinkRequest.";
                                await SendResponseToApps(linkResponse);
                                continue;
                            }

                            linkResponse.LinkRequest.DestinationAppId = linkResponse.LinkRequest.DestinationAppId.ToLower();


                            string destDirPath = Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.DestinationAppId, "inbox");
                            string sourceDirPath = Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId, "outbox");

                            _logger.LogDebug("Checking if directory exists ('{xfer_directory}/{sourceAppName}') (trackingId: '{trackingId}' / correlationId: '{correlationId}')", _all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                            if (!Directory.Exists(Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId))) {
                                _logger.LogError("Received a Link Request with an inaccessibale source path.  Source path '{sourcePath}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.NotFound;
                                linkResponse.ResponseHeader.Message = string.Format($"Link Service can't access '{Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId)}'.  Unable to process LinkRequest.");
                                await SendResponseToApps(linkResponse);
                                continue;
                            }

                            // Run through checks for valid Subdirectory
                            if (!string.IsNullOrWhiteSpace(linkResponse.LinkRequest.Subdirectory)) {
                                _logger.LogDebug("SubDirectory '{subDirectory}' specified. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkResponse.LinkRequest.Subdirectory, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                                // Check for invalid characters (like a user is trying to break out of the directory tree)
                                if (linkResponse.LinkRequest.Subdirectory.IndexOfAny(Path.GetInvalidPathChars().ToArray()) > 0) {
                                    _logger.LogError("Invalid characters in subDirectory '{subDirectory}' detected.  SubDirectory can't contain characters '{bad_char_list}'   (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkResponse.LinkRequest.Subdirectory, string.Join(" ", Path.GetInvalidPathChars().ToArray()), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InvalidArgument;
                                    linkResponse.ResponseHeader.Message = string.Format($"Invalid characters in subDirectory '{linkResponse.LinkRequest.Subdirectory}' detected.  SubDirectory can't contain characters '{string.Join(" ", Path.GetInvalidPathChars().ToArray())}'.  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }

                                // Check if someone is trying to access the root
                                if (Path.IsPathRooted(linkResponse.LinkRequest.Subdirectory) || linkResponse.LinkRequest.Subdirectory.StartsWith(@"\\")) {
                                    _logger.LogError("Received a rooted subdirectory of '{subDirectory}'   (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkResponse.LinkRequest.Subdirectory, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InvalidArgument;
                                    linkResponse.ResponseHeader.Message = string.Format($"Rooted subdirectory of '{linkResponse.LinkRequest.Subdirectory}' detected.  Subdirectory must be a relative path (example: 'path/somedirectory/example').  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }

                                _logger.LogDebug("Checking if directory exists ('{sourceDirPath}/{subdirectory}') (trackingId: '{trackingId}' / correlationId: '{correlationId}')", sourceDirPath, linkResponse.LinkRequest.Subdirectory, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                                // Check if we have a valid source directory
                                if (!Directory.Exists(Path.Combine(sourceDirPath, linkResponse.LinkRequest.Subdirectory))) {
                                    _logger.LogError("Received a Link Request with an inaccessibale source path.  Source path '{sourcePath}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(_all_xfer_dir, linkResponse.LinkRequest.RequestHeader.AppId, linkResponse.LinkRequest.Subdirectory), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.NotFound;
                                    linkResponse.ResponseHeader.Message = string.Format($"Link Service can't access '{Path.Combine(sourceDirPath, linkResponse.LinkRequest.Subdirectory)}'; directory doesn't exist..  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }

                                sourceDirPath = Path.Combine(sourceDirPath, linkResponse.LinkRequest.Subdirectory);
                                destDirPath = Path.Combine(destDirPath, linkResponse.LinkRequest.Subdirectory);
                            }


                            _logger.LogDebug("Checking for xfer directory for app at '{destDirPath}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                            if (Directory.Exists(destDirPath)) {
                                _logger.LogDebug("Directory '{destDirPath}' already exists. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                            } else {
                                _logger.LogDebug("Directory '{destDirPath}' not found.  Creating... (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                System.IO.Directory.CreateDirectory(destDirPath);
                                _logger.LogDebug("Directory '{destDirPath}' created. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                            }

                            if (!string.IsNullOrWhiteSpace(linkResponse.LinkRequest.Subdirectory)) {
                                _logger.LogDebug("Checking for destination path '{destDirPath}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                                if (Directory.Exists(destDirPath)) {
                                    _logger.LogDebug("Directory '{destDirPath}' already exists. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                } else {
                                    _logger.LogDebug("Directory '{destDirPath}' not found.  Creating... (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    System.IO.Directory.CreateDirectory(destDirPath);
                                    _logger.LogDebug("Directory '{destDirPath}' created. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", destDirPath, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                }
                            }


                            _logger.LogDebug("Checking for file '{sourceFilePath}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                            if (!File.Exists(Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName))) {
                                _logger.LogError("Received a Link Request.  Source File '{sourcePath}' not found. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.NotFound;
                                linkResponse.ResponseHeader.Message = string.Format($"Source File '{sourceDirPath}' not found.  Unable to process LinkRequest.");
                                await SendResponseToApps(linkResponse);
                                continue;
                            }

                            _logger.LogInformation("File '{sourcePath}' found. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);


                            // Desting File exists
                            if (File.Exists(Path.Combine(destDirPath, linkResponse.LinkRequest.FileName))) {
                                _logger.LogError("Destination file '{destFilePath}' exists.  Overwrite = {overwrite}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.Overwrite.ToString(), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                if (!linkResponse.LinkRequest.Overwrite) {
                                    _logger.LogError("Destination file '{destFilePath}' exists, but OVERWRITE = False. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.NotFound;
                                    linkResponse.ResponseHeader.Message = string.Format($"Destination File '{Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)}' already exists.  Overwrite = False.  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }

                                // Try to delete it
                                try {
                                    _logger.LogDebug("Deleting '{destFilePath}'. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    File.Delete(Path.Combine(destDirPath, linkResponse.LinkRequest.FileName));
                                } catch (Exception ex) {
                                    _logger.LogError("Error deleting file '{destFilePath}'.  Error: {error_msg}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), ex.Message, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InternalServiceError;
                                    linkResponse.ResponseHeader.Message = string.Format($"Error deleting file '{Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)}'.  Error: '{ex.Message}'  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }
                            }



                            // Everything is good to go.  Move the file
                            try {
                                _logger.LogInformation("Copying File '{source}' to '{destination}'. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                File.Move(Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName + ".tmp"));
                                File.Move(Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName + ".tmp"), Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName));
                                File.Copy(Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), Path.Combine(destDirPath, linkResponse.LinkRequest.FileName));

                                linkResponse.FileSizeKB = new FileInfo(Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)).Length / 1024;
                                linkResponse.LinkProcessedTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
                            } catch (Exception ex) {
                                _logger.LogError("Error copying file '{sourceFilePath}' to '{destFilePath}'.  Error: {error_msg}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), Path.Combine(destDirPath, linkResponse.LinkRequest.FileName), ex.Message, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InternalServiceError;
                                linkResponse.ResponseHeader.Message = string.Format($"Error copy '{Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName)}' to '{Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)}'.  Error: '{ex.Message}'  Unable to process LinkRequest.");
                                await SendResponseToApps(linkResponse);
                                continue;
                            }

                            try {
                                _logger.LogInformation("Writing LinkResponse to '{destination}'. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.Successful;

                                string linkRequest_json = Google.Protobuf.JsonFormatter.Default.Format(linkResponse);

                                if (File.Exists(Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")))) {
                                    _logger.LogDebug("Previous file '{filePath}' found.  Moving to '{destination}'. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")), Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.linkResponse")), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    File.Move(Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")), Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.linkResponse")));
                                }

                                File.AppendAllText(Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")), linkRequest_json);

                                _logger.LogInformation("Sending LinkResponse notification to destination app '{destAppId}'. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkResponse.LinkRequest.DestinationAppId, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);

                                await SendResponseToApps(linkResponse);
                            } catch (Exception ex) {
                                _logger.LogError("Error writing LinkResponse file '{destFilePath}'.  Error: {error_msg}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(destDirPath, string.Format($"{linkResponse.LinkRequest.FileName}.linkResponse")), ex.Message, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InternalServiceError;
                                linkResponse.ResponseHeader.Message = string.Format($"Error copy '{Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)}' to '{Path.Combine(destDirPath, linkResponse.LinkRequest.FileName)}'.  Error: '{ex.Message}'  Unable to process LinkRequest.");
                                await SendResponseToApps(linkResponse);
                                continue;
                            }

                            // Check if the configuration is overriding the LeaveSourceFile property
                            if (!string.IsNullOrWhiteSpace(_appConfig.LEAVE_SOURCE_FILE_PROPERTY_VALUE)) {
                                // Calculate the new property value, but default to false incase we get a non-bool value
                                bool overrideValue = bool.TryParse(_appConfig.LEAVE_SOURCE_FILE_PROPERTY_VALUE, out overrideValue) ? overrideValue : false;
                                if (overrideValue != linkResponse.LinkRequest.LeaveSourceFile) {
                                    _logger.LogDebug("LEAVE_SOURCE_FILE_PROPERTY_VALUE in config differs from LinkRequest.LeaveSourceFile.  Overriding LinkRequest.LeaveSourceFile = {overrideValue}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", overrideValue, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.LinkRequest.LeaveSourceFile = overrideValue;
                                }
                            }

                            if (!linkResponse.LinkRequest.LeaveSourceFile) {
                                try {
                                    _logger.LogInformation("LeaveSourceFile = False.  Removing '{source}'.  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    File.Delete(Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName));
                                } catch (Exception ex) {
                                    _logger.LogError("Error deleting source file '{sourceFilePath}'.  Error: {error_msg}. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName), ex.Message, linkResponse.LinkRequest.RequestHeader.TrackingId, linkResponse.LinkRequest.RequestHeader.CorrelationId);
                                    linkResponse.ResponseHeader.Status = MessageFormats.Common.StatusCodes.InternalServiceError;
                                    linkResponse.ResponseHeader.Message = string.Format($"Error deleting source file '{Path.Combine(sourceDirPath, linkResponse.LinkRequest.FileName)}'.  Error: '{ex.Message}'  Unable to process LinkRequest.");
                                    await SendResponseToApps(linkResponse);
                                    continue;
                                }
                            }

                        } catch (Exception ex) {
                            _logger.LogError("Failed to transfer file.  Error: {error}", ex.Message);
                        }
                    }

                    await Task.Delay(_appConfig.FILEMOVER_POLLING_MS, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Queues a log message to be written to disk
        /// </summary>
        /// <returns>void</returns>
        protected internal void QueueFileMove(MessageFormats.HostServices.Link.LinkResponse linkResponse) {
            try {
                _logger.LogTrace("Adding LinkRequest to queue. (trackingId: '{trackingId}' / correlationId: '{correlationId}')", linkResponse.ResponseHeader.TrackingId, linkResponse.ResponseHeader.CorrelationId);
                _linkResponseQueue.Add(linkResponse);
            } catch (Exception ex) {
                _logger.LogError("Failure storing LinkRequest to queue (trackingId: '{trackingId}' / correlationId: '{correlationId}').  Error: {errorMessage}", linkResponse.ResponseHeader.TrackingId, linkResponse.ResponseHeader.CorrelationId, ex.Message);
            }
        }

        /// <summary>
        /// Sends the message back to the requesting user
        /// </summary>
        /// <returns>void</returns>
        private Task SendResponseToApps(MessageFormats.HostServices.Link.LinkResponse message) {
            return Task.Run(async () => {
                MessageFormats.HostServices.Link.LinkResponse? pluginResponse = message;
                MessageFormats.HostServices.Link.LinkRequest? pluginRequest = message.LinkRequest;
                _logger.LogDebug("Passing message '{messageType}' and '{responseType}' to plugins (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.LinkRequest.GetType().Name, message.ResponseHeader.TrackingId, message.ResponseHeader.CorrelationId);

                (MessageFormats.HostServices.Link.LinkRequest? output_request, MessageFormats.HostServices.Link.LinkResponse? output_response) =
                                                _pluginLoader.CallPlugins<MessageFormats.HostServices.Link.LinkRequest?, Plugins.PluginBase, MessageFormats.HostServices.Link.LinkResponse>(
                                                    orig_request: pluginRequest, orig_response: pluginResponse,
                                                    pluginDelegate: _pluginDelegates.LinkResponse);


                _logger.LogDebug("Plugins finished processing '{messageType}' and '{responseType}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.LinkRequest.GetType().Name, message.ResponseHeader.TrackingId, message.ResponseHeader.CorrelationId);

                if (output_response == null || output_request == null) {
                    _logger.LogInformation("Plugins nullified '{messageType}' or '{output_requestMessageType}'.  Dropping Message (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.LinkRequest.GetType().Name, message.GetType().Name, message.ResponseHeader.TrackingId, message.ResponseHeader.CorrelationId);
                    return;
                }

                message = output_response;

                // There was a failure - route error back to calling app
                if (message.ResponseHeader.Status != MessageFormats.Common.StatusCodes.Successful) {
                    _logger.LogWarning("Failure detected in LinkResponse (StatusCode is not 'Successful').  Alerting calling app and discarding message");
                    await _client.DirectToApp(appId: message.LinkRequest.RequestHeader.AppId, message: message);
                    return;
                }

                _logger.LogInformation("Sending successful '{messageType}' to '{sourceAppId}' and '{destinationAppId}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", message.GetType().Name, message.LinkRequest.RequestHeader.AppId, message.LinkRequest.DestinationAppId, message.ResponseHeader.TrackingId, message.ResponseHeader.CorrelationId);
                await _client.DirectToApp(appId: message.LinkRequest.RequestHeader.AppId, message: message);
                await _client.DirectToApp(appId: message.LinkRequest.DestinationAppId, message: message);
            });
        }

    }
}
