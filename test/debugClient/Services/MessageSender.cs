namespace PayloadApp.DebugClient;

public class MessageSender : BackgroundService {
    private readonly ILogger<MessageSender> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Microsoft.Azure.SpaceFx.Core.Client _client;
    private readonly string _appId;
    private readonly string _hostSvcAppId;

    public MessageSender(ILogger<MessageSender> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = _serviceProvider.GetService<Microsoft.Azure.SpaceFx.Core.Client>() ?? throw new NullReferenceException($"{nameof(Microsoft.Azure.SpaceFx.Core.Client)} is null");
        _appId = _client.GetAppID().Result;
        _hostSvcAppId = _appId.Replace("-client", "");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation("MessageSender running at: {time}", DateTimeOffset.Now);

            System.Threading.Thread.Sleep(3000);

            Boolean SVC_ONLINE = _client.ServicesOnline().Any(pulse => pulse.AppId.Equals(_hostSvcAppId, StringComparison.CurrentCultureIgnoreCase));

            ListHeardServices();

            await SendFileRootDirectory();
        }
    }

    private void ListHeardServices() {
        System.Threading.Thread.Sleep(250);
        Console.WriteLine("Apps Online:");
        _client.ServicesOnline().ForEach((pulse) => Console.WriteLine($"...{pulse.AppId}..."));
    }

    private async Task SendFileRootDirectory() {
        var (inbox, outbox, root) = _client.GetXFerDirectories().Result;
        System.IO.File.Copy("/workspaces/hostsvc-link/test/sampleData/astronaut.jpg", string.Format($"{outbox}/astronaut.jpg"), overwrite: true);

        Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest request = new() {
            RequestHeader = new() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            FileName = "astronaut.jpg",
            DestinationAppId = "test-output"
        };

        await _client.DirectToApp(appId: _hostSvcAppId, message: request);
    }

}
