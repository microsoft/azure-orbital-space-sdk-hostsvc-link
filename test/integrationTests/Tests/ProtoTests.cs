namespace Microsoft.Azure.SpaceFx.HostServices.Link.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class ProtoTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    public ProtoTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void LinkRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "LinkType", "Priority", "FileName", "Subdirectory", "DestinationAppId", "Overwrite", "LeaveSourceFile", "ExpirationTime" };

        var request = new Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest() {
            LinkType = Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App,
            FileName = _context.GenericString,
            Subdirectory = _context.GenericString,
            DestinationAppId = _context.GenericString,
            Overwrite = true,
            LeaveSourceFile = true,
            ExpirationTime = _context.GenericTimeStamp
        };

        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link.LinkRequest.Types.LinkType.App2App, request.LinkType);
        Assert.Equal(_context.GenericString, request.FileName);
        Assert.Equal(_context.GenericString, request.Subdirectory);
        Assert.Equal(_context.GenericString, request.Subdirectory);
        Assert.Equal(_context.GenericString, request.DestinationAppId);
        Assert.Equal(_context.GenericTimeStamp, request.ExpirationTime);
        Assert.True(request.Overwrite);
        Assert.True(request.LeaveSourceFile);


        CheckProperties<MessageFormats.HostServices.Link.LinkRequest>(expectedProperties);
    }


    [Fact]
    public void LinkType_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Downlink", "Uplink", "Crosslink", "App2App", "Unknown" };
        CheckEnumerator<MessageFormats.HostServices.Link.LinkRequest.Types.LinkType>(possibleEnumValues);
    }

    private static void CheckProperties<T>(List<string> expectedProperties) where T : IMessage, new() {
        T testMessage = new T();
        List<string> actualProperties = testMessage.Descriptor.Fields.InFieldNumberOrder().Select(field => field.PropertyName).ToList();

        Console.WriteLine($"......checking properties for {typeof(T)}");

        Console.WriteLine($".........expected properties: ({expectedProperties.Count}): {string.Join(",", expectedProperties)}");
        Console.WriteLine($".........actual properties: ({actualProperties.Count}): {string.Join(",", actualProperties)}");

        Assert.Equal(0, expectedProperties.Count(_prop => !actualProperties.Contains(_prop)));  // Check if there's any properties missing in the message
        Assert.Equal(0, actualProperties.Count(_prop => !expectedProperties.Contains(_prop)));  // Check if there's any properties we aren't expecting
    }

    private static void CheckEnumerator<T>(List<string> expectedEnumValues) where T : System.Enum {
        // Loop through and try to set all the enum values
        foreach (string enumValue in expectedEnumValues) {
            // This will throw a hard exception if we pass an item that doesn't work
            object? parsedEnum = System.Enum.Parse(typeof(T), enumValue);
            Assert.NotNull(parsedEnum);
        }

        // Make sure we don't have any extra values we didn't test
        int currentEnumCount = System.Enum.GetNames(typeof(T)).Length;

        Assert.Equal(expectedEnumValues.Count, currentEnumCount);
    }
}
