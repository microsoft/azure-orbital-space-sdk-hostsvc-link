# test

This directory contains tests, clients, and plug-ins to validate the Sensor Service.

```plaintext
.
├── debugClient              # a .NET application that allows you to interact with Sensor Service
├── integrationTestPlugin    # a .NET application that modifies  responses for tests
├── integrationTests         # a xUnit test suite that validates the end-to-end business-logic of Sensor Service
└── unitTests                # a xUnit test suite that validates Sensor Service components
```

## Run Integration Tests

One way to get started with Sensor Service is to run the [Debug Client](./debugClient/Program.cs) and [Integration Tests](./integrationTests/LogMsg.cs).

1. Open this respository in its Dev Container

1. Open the "Run and Debug" menu (CTRL + SHIFT + D)

2. From the launch configuration dropdown, select "Integration Tests - Run"

    ![Launch Configuration Options](../docs/img/integration-test-select.png)

3. Select the Start Icon ▶ (or press F5) to begin debugging the service and running the tests

4. Your terminal will show the tasks executed as described in [tasks.json](../.vscode/tasks.json)

5. The Debug Console (CTRL + SHIFT + Y) will become active, select the "Integration Tests - Client Run" session from the dropdown

    ![Debug Console Output](../docs/img/integration-test-debugger-output.png)

6. A successful test run should emit something like this to the debug console

    ```plaintext
        A total of 1 test files matched the specified pattern.
        /workspaces/hostsvc-link/test/integrationTests/bin/Debug/net6.0/integrationTests.dll
        [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.4.5+1caef2f33e (64-bit .NET 6.0.16)
        [xUnit.net 00:00:00.79]   Discovering: integrationTests
        [xUnit.net 00:00:00.82]   Discovered:  integrationTests
        [xUnit.net 00:00:00.82]   Starting:    integrationTests
        Waiting for 'hostsvc-link-test-host' to come online...
        hostsvc-link-test-host is online.  Starting tests
        [SensorDataReceived] - START
        [SensorDataReceived] - Sending tasking request for 'DemoTemperatureSensor'
        [SensorDataReceived] - Setting Sensor Data message deadline for 2023-05-24T18:15:09Z (30 seconds)
        [SensorDataReceived] - END
        [SensorDataReceived] - START
        [SensorDataReceived] - Sending tasking request
        [SensorDataReceived] - Setting response message deadline for 2023-05-24T18:15:10Z (30 seconds)
        [SensorDataReceived] - Got a response!  Result status: Successful
        [SensorDataReceived] - END
        [SensorDataReceived] - Setting Sensor Data message deadline for 2023-05-24T18:15:10Z (30 seconds)
        Passed integrationTests.SensorData.SensorDataRecurring [607 ms]
        [SensorDataReceived] - END
        [xUnit.net 00:00:05.60]   Finished:    integrationTests
        Passed integrationTests.SensorData.SensorDataReceived [1 s]
        Passed integrationTests.SensorsAvailable.SensorsAvailableQueryAndResponse [100 ms]
        Passed integrationTests.Tasking.AttemptAuthZBypass [101 ms]
        Passed integrationTests.Tasking.TaskingQueryAndResponse [100 ms]
        Passed integrationTests.TaskingPreCheck.TaskingPreCheckQueryAndResponse [100 ms]

        Test Run Successful.
        Total tests: 6
            Passed: 6
        Total time: 6.2538 Seconds
    ```

1. Stop the debugger by selecting the Stop Icon 🟥 (Shift + F5)

    This is a **required step**, failure to stop the services will result in unknown pod states

## Debugging Integration Tests

1. Follow the steps to successfully [run the integration tests](#run-integration-tests)

1. Be sure to stop the services and detach the debugger
2. Open the "Run and Debug" menu (CTRL + SHIFT + D)
3. From the launch configuration dropdown, select debugService of choice such as "Integration Tests - Debug"
    ![Launch Configuration Options](../docs/img/integration-test-select.png)

4. Set a breakpoint on [Foreman.MessageReceivedHandler()](../src/Services/Foreman.cs)

    1. Click in the gutter area to the left of a line number, a circle 🔴 will appear for lines with breakpoints set

    ![Breakpoint set](../docs/img/integration-test-breakpoint.png)

5. Select the Continue Icon ⏯️ (or press F5) to run the integration tests debug again

6. The breakpoint will be hit and execution of the application will pause

    ![Breakpoint hit](../docs/img/integration-test-breakpoint-hit.png)

7. You can inspect the state of the application, view the call stack, and interact with the application

9. Remove your breakpoint, or move it around, explore how the application works

10. Select the Continue Icon ⏯️ (or press F5) to continue execution

    ![Continue Icon](../docs/img/integration-test-continue.png)

11. Remember, once finished, stop the debugger by selecting the Stop Icon 🟥 (Shift + F5)

    This is a **required step**, failure to stop the services will result in unknown pod states
    ![Stop Icon](../docs/img/stop-debug.png)

## Run Unit Tests

The unit tests assert that Sensor Service components are in the expected shape and configuration.

1. Open this respository in its Dev Container

1. Open the "Run and Debug" menu (CTRL + SHIFT + D)

1. From the launch configuration dropdown, select "Unit Tests - Run"
    ![Launch Configuration Options](../docs/img/integration-test-select.png)
1. Select the Start Icon ▶ (or press F5) to begin running the tests

1. Your terminal will show the tasks executed as described in [tasks.json](../.vscode/tasks.json)

1. The Debug Console (CTRL + SHIFT + Y) will become active, select the "Unit Tests" session from the dropdown

1. A successful test run should emit something like this to the debug console

    ```plaintext
        A total of 1 test files matched the specified pattern.
        /workspaces/hostsvc-link/test/unitTests/bin/Debug/net6.0/unitTests.dll
        [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.4.5+1caef2f33e (64-bit .NET 6.0.16)
        [xUnit.net 00:00:00.56]   Discovering: unitTests
        [xUnit.net 00:00:00.59]   Discovered:  unitTests
        [xUnit.net 00:00:00.59]   Starting:    unitTests
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingResponse
        .........expected properties: (3): ResponseHeader,SensorID,ResponseData
        .........actual properties: (3): ResponseHeader,SensorID,ResponseData
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.SensorsAvailableResponse
        .........expected properties: (2): ResponseHeader,Sensors
        .........actual properties: (2): ResponseHeader,Sensors
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingRequest
        .........expected properties: (5): RequestHeader,SensorID,RequestTime,ExpirationTime,RequestData
        .........actual properties: (5): RequestHeader,SensorID,RequestTime,ExpirationTime,RequestData
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingPreCheckRequest
        .........expected properties: (5): RequestHeader,SensorID,RequestTime,ExpirationTime,RequestData
        .........expected properties: (5): RequestHeader,SensorID,RequestTime,ExpirationTime,RequestData
        .........actual properties: (5): RequestHeader,SensorID,RequestTime,ExpirationTime,RequestData
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingPreCheckResponse
        .........expected properties: (3): ResponseHeader,SensorID,ResponseData
        .........actual properties: (3): ResponseHeader,SensorID,ResponseData
        ......checking properties for Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.SensorsAvailableRequest
        .........expected properties: (1): RequestHeader
        .........actual properties: (1): RequestHeader
        [xUnit.net 00:00:00.83]   Finished:    unitTests
        Passed unitTests.ProtoTests.TaskingResponse [37 ms]
        Passed unitTests.ProtoTests.SensorsAvailableResponse [< 1 ms]
        Passed unitTests.ProtoTests.TaskingRequest [< 1 ms]
        Passed unitTests.ProtoTests.TaskingPreCheckRequest [< 1 ms]
        Passed unitTests.ProtoTests.TaskingPreCheckResponse [< 1 ms]
        Passed unitTests.ProtoTests.SensorsAvailableRequest [< 1 ms]

        Test Run Successful.
        Total tests: 6
            Passed: 6
        Total time: 1.3316 Seconds
    ```

## Integration Test Plugin

The [Integration Test Plugin](./integrationTestPlugin/integrationTestPlugin.cs) is used to validate that message requests and responses modified by plugins are honored by the Sensor Service.

This plugin is injected into the running  Service during testing by providing the [appsettings.IntegrationTest.json](../src/appsettings.IntegrationTest.json) configuration to the Sensor Service at startup time in the `Integration Tests - Debug` launch configuration in [launch.json](../.vscode/launch.json).
