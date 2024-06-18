namespace Microsoft.Azure.SpaceFx.HostServices.Link.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class LinkRequestTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public LinkRequestTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void LinkRequestToRootDirectory() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Link.LinkResponse? response = null;
        MessageFormats.HostServices.Link.LinkResponse? processed_response = null;

        string trackingId = Guid.NewGuid().ToString();

        MessageFormats.HostServices.Link.LinkRequest testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            LeaveSourceFile = true,
            FileName = $"{trackingId}.jpg",
            DestinationAppId = TestSharedContext.TARGET_SVC_APP_ID
        };

        // Register a callback event to catch the response
        void LinkResponseEventHandler(object? _, MessageFormats.HostServices.Link.LinkResponse _response) {
            if (_response.ResponseHeader.CorrelationId != testMessage.RequestHeader.CorrelationId) return;  // This is no the log response you're looking for

            if (_response.ResponseHeader.Status == MessageFormats.Common.StatusCodes.Pending) {
                // Response from our initial request
                response = _response;
            } else {
                // Response from the Log File write
                processed_response = _response;
            }
        }

        MessageHandler<MessageFormats.HostServices.Link.LinkResponse>.MessageReceivedEvent += LinkResponseEventHandler;

        Task.Run(async () => {
            Console.WriteLine($"Sending '{testMessage.GetType().Name}' (TrackingId: '{testMessage.RequestHeader.TrackingId}')");
            File.Copy("/workspaces/hostsvc-link/test/sampleData/astronaut.jpg", string.Format($"{(await TestSharedContext.SPACEFX_CLIENT.GetXFerDirectories()).outbox_directory}/{trackingId}.jpg"), overwrite: true);
            await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);
        });

        Console.WriteLine($"Waiting for response (TrackingId: '{testMessage.RequestHeader.TrackingId}')");

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);

        Console.WriteLine($"Waiting for FileMoverService to move file and alert on update...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (processed_response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (processed_response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.Equal(MessageFormats.Common.StatusCodes.Successful, processed_response.ResponseHeader.Status);

        Assert.NotNull(processed_response);
    }

    [Fact]
    public void LinkRequestToSubDirectoryDirectory() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Link.LinkResponse? response = null;
        MessageFormats.HostServices.Link.LinkResponse? processed_response = null;

        string sourceDirectory = "integrationTest/alpha/bravo";

        string trackingId = Guid.NewGuid().ToString();

        MessageFormats.HostServices.Link.LinkRequest testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            LeaveSourceFile = true,
            FileName = $"{trackingId}.jpg",
            DestinationAppId = TestSharedContext.TARGET_SVC_APP_ID,
            Subdirectory = sourceDirectory
        };

        // Register a callback event to catch the response
        void LinkResponseEventHandler(object? _, MessageFormats.HostServices.Link.LinkResponse _response) {
            if (_response.ResponseHeader.CorrelationId != testMessage.RequestHeader.CorrelationId) return;  // This is no the log response you're looking for

            if (_response.ResponseHeader.Status == MessageFormats.Common.StatusCodes.Pending) {
                // Response from our initial request
                response = _response;
            } else {
                // Response from the Log File write
                processed_response = _response;
            }
        }

        MessageHandler<MessageFormats.HostServices.Link.LinkResponse>.MessageReceivedEvent += LinkResponseEventHandler;

        Task.Run(async () => {
            Console.WriteLine($"Sending '{testMessage.GetType().Name}' (TrackingId: '{testMessage.RequestHeader.TrackingId}')");
            Directory.CreateDirectory($"{(await TestSharedContext.SPACEFX_CLIENT.GetXFerDirectories()).outbox_directory}/{sourceDirectory}");
            File.Copy("/workspaces/hostsvc-link/test/sampleData/astronaut.jpg", string.Format($"{(await TestSharedContext.SPACEFX_CLIENT.GetXFerDirectories()).outbox_directory}/{sourceDirectory}/{trackingId}.jpg"), overwrite: true);
            await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);
        });

        Console.WriteLine($"Waiting for response (TrackingId: '{testMessage.RequestHeader.TrackingId}')");

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);

        Console.WriteLine($"Waiting for FileMoverService to move file and alert on update...");

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (processed_response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (processed_response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.Equal(MessageFormats.Common.StatusCodes.Successful, processed_response.ResponseHeader.Status);

        Assert.NotNull(processed_response);
    }
}