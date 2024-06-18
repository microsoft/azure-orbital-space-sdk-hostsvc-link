using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx.HostServices.Link;
public class Utils {
    public class PluginDelegates {
        private readonly ILogger<PluginDelegates> _logger;
        private readonly List<Core.Models.PLUG_IN> _plugins;
        public PluginDelegates(ILogger<PluginDelegates> logger, IServiceProvider serviceProvider) {
            _logger = logger;
            _plugins = serviceProvider.GetService<List<Core.Models.PLUG_IN>>() ?? new List<Core.Models.PLUG_IN>();
        }

        internal (MessageFormats.HostServices.Link.LinkRequest? output_request, MessageFormats.HostServices.Link.LinkResponse? output_response) LinkResponse((MessageFormats.HostServices.Link.LinkRequest? input_request, MessageFormats.HostServices.Link.LinkResponse? input_response, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.LinkResponse);
            if (input.input_request is null || input.input_request is default(MessageFormats.HostServices.Link.LinkRequest)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            try {
                Task<(MessageFormats.HostServices.Link.LinkRequest? output_request, MessageFormats.HostServices.Link.LinkResponse? output_response)> pluginTask = input.plugin.LinkResponse(input_request: input.input_request, input_response: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }


        internal MessageFormats.HostServices.Link.LinkRequest? LinkRequest((MessageFormats.HostServices.Link.LinkRequest? input_request, Plugins.PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.LinkRequest);

            if (input.input_request is null || input.input_request is default(MessageFormats.HostServices.Link.LinkRequest)) {
                _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return input.input_request;
            }
            _logger.LogDebug("Plugin {pluginMethod}: START", methodName);

            try {
                Task<MessageFormats.HostServices.Link.LinkRequest?> pluginTask = input.plugin.LinkRequest(input_request: input.input_request);
                pluginTask.Wait();
                input.input_request = pluginTask.Result;
            } catch (Exception ex) {
                _logger.LogError("Plugin {pluginName} / {pluginMethod}: Error: {errorMessage}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: END", input.plugin.ToString(), methodName);
            return input.input_request;
        }
    }
}