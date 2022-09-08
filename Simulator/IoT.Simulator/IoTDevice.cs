namespace IoT.Simulator;

/// <summary>
/// This is the specific implementation of this simulated device
/// </summary>
public class IoTDevice : IoTDeviceBase
{
    /// <summary>
    /// Set any callback methods that may be called from the cloud
    /// </summary>
    protected override async Task<bool> SetupCallBackMethods()
    {
        await Task.FromResult(true);
        try
        {
            using var cts = new CancellationTokenSource();
            DeviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            Utilities.DisplayMessage("    Method Calls accepted by this device:", ConsoleColor.Yellow);
            // Setup a callback for the 'WriteToConsole' method to make device do a local action
            Utilities.DisplayMessage("      WriteToConsole: writes a message to the console", ConsoleColor.Yellow);
            DeviceClient.SetMethodHandlerAsync("WriteToConsole", DM_WriteToConsoleAsync, null, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();

            // Setup a callback for the 'GetDeviceName' method to return something to a C2D command call
            Utilities.DisplayMessage("      GetDeviceName: device name is returned to cloud", ConsoleColor.Yellow);
            DeviceClient.SetMethodHandlerAsync(
                 "GetDeviceName",
                 DM_GetDeviceNameAsync,
                 new DeviceData { Name = DeviceId },
                 cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();

            // Setup a callback for the 'Default Method Handler' method to show if unhandled method was called
            // Utilities.DisplayMessage("    C2D Method 'Default Method Handler' defined!", ConsoleColor.Yellow);
            DeviceClient.SetMethodDefaultHandlerAsync(OnDefaultMethodCalled, DeviceClient).ConfigureAwait(false).GetAwaiter().GetResult();

            // set up monitor for device twin
            //var updatedProperties = new TwinCollection();
            //updatedProperties["LastTimeAppLaunched"] = DateTime.Now;
            //DeviceClient.UpdateReportedPropertiesAsync(updatedProperties).ConfigureAwait(false).GetAwaiter().GetResult();
            DeviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null).ConfigureAwait(false).GetAwaiter().GetResult();

            // Setup handler for C2D device messages
            var thread = new Thread(() => OnDeviceMessageReceived(DeviceClient));
            thread.Start();

            return true;
        }
        catch (Exception ex)
        {
            Utilities.DisplayErrorMessage($"Exception in SetupCallBackMethods: {Utilities.GetExceptionMessage(ex)}");
            return false;
        }
    }

    /// <summary>
    /// Set any callback methods that may be called from the cloud
    /// </summary>
    protected override async Task<bool> RemoveCallBackMethods()
    {
        await Task.FromResult(true);
        DeviceClient.SetMethodHandlerAsync("GetDeviceName", null, null).GetAwaiter();
        DeviceClient.SetMethodHandlerAsync("WriteToConsole", null, null).GetAwaiter();
        return true;
    }

    /// <summary>
    /// Handle change in Device Twin Property
    /// </summary>
    protected override async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        await Task.FromResult(true);
        Utilities.DisplayMessage("One or more device twin desired properties changed:", ConsoleColor.Green);
        Utilities.DisplayMessage(" " + JsonConvert.SerializeObject(desiredProperties), ConsoleColor.Blue);

        // put in calls here to check things that might be affected if the device twin changes
        RaiseCommandWithProperties(Constants.Commands.FirmwareUpdate, desiredProperties);
    }

    /// <summary>
    /// Handle any method calls that are not defined
    /// </summary>
    protected override async Task<MethodResponse> OnDefaultMethodCalled(MethodRequest methodRequest, object userContext)
    {
        Utilities.DisplayMessage("");
        Utilities.DisplayMessageInABox($"Method {methodRequest.Name} was called (and was not expected!)", ConsoleColor.Red);
        Utilities.DisplayMessage($"  Data Received: {methodRequest.DataAsJson}\n", ConsoleColor.Blue);
        // methodRequest.DataAsJson
        //SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.Data}").GetAwaiter().GetResult();
        //return Task.FromResult(new MethodResponse(new byte[0], 200));
        await SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.DataAsJson}");
        return new MethodResponse(new byte[0], 200);
    }

    /// <summary>
    /// C2D Test Method to make device perform some local task
    /// </summary>
    public async Task<MethodResponse> DM_WriteToConsoleAsync(MethodRequest methodRequest, object userContext)
    {
        Utilities.DisplayMessage("");
        Utilities.DisplayMessageInABox($" C2D Method '{methodRequest.Name}' was called.", ConsoleColor.Cyan);
        if (!string.IsNullOrEmpty(methodRequest.DataAsJson) && methodRequest.DataAsJson != "\"\"")
        {
            Utilities.DisplayMessage($"  Data Received: {methodRequest.DataAsJson}\n", ConsoleColor.Blue);
        }
        else
        {
            Utilities.DisplayMessage($"  No data received!", ConsoleColor.Blue);
        }
        var result = JsonConvert.SerializeObject("Message sent!");
        //SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.Data}").GetAwaiter().GetResult();
        //return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        await SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.DataAsJson}");
        return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
    }

    /// <summary>
    /// C2D Test Method that will return a value when called from the cloud
    /// </summary>
    public async Task<MethodResponse> DM_GetDeviceNameAsync(MethodRequest methodRequest, object userContext)
    {
        Utilities.DisplayMessage("");
        Utilities.DisplayMessageInABox($" C2D Method '{methodRequest.Name}' was called.", ConsoleColor.Cyan);
        MethodResponse retValue;
        if (userContext == null)
        {
            retValue = new MethodResponse(new byte[0], 500);
        }
        else
        {
            var deviceData = (DeviceData)userContext;
            deviceData.IpAddress = Utilities.GetIPAddress();
            var result = JsonConvert.SerializeObject(deviceData);
            Utilities.DisplayMessage($"  Data Returned: {result}\n", ConsoleColor.Blue);
            retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
        }
        // SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.Data}").GetAwaiter().GetResult();
        //return Task.FromResult(retValue);
        await SendLogEntry(Constants.EventType.Method, methodRequest.Name, $"C2D Method {methodRequest.Name} was called with {methodRequest.DataAsJson}");
        return retValue;
    }

    /// <summary>
    /// Handle Device Messages
    /// </summary>
    protected override async void OnDeviceMessageReceived(DeviceClient client)
    {
        Utilities.DisplayMessage("      Enabling receiver for Cloud to Device messages...", ConsoleColor.Yellow);
        try
        {
            while (true)
            {
                // The following line is blocking until a timeout occurs (?)
                using var message = client.ReceiveAsync().GetAwaiter().GetResult();
                if (message == null)
                {
                    Utilities.DisplayMessage("Command Handler Timeout... command is null!");
                    continue;
                }

                var messageData = Encoding.UTF8.GetString(message.GetBytes());
                Utilities.DisplayMessage(string.Empty);
                Utilities.DisplayMessageInABox($"A message was received!  Data: {messageData}", ConsoleColor.Red);

                if (await ProcessCommand(client, message, messageData, message.Properties))
                {
                    Utilities.DisplayMessage("\n      Message was handled!", ConsoleColor.Green);
                }
                //else
                //{
                //    Utilities.DisplayErrorMessage("\n    Message was rejected!");
                //}
            }
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            if (!msg.StartsWith("The semaphore has been disposed") && !msg.StartsWith("Cannot access a disposed object"))
            {
                Utilities.DisplayErrorMessage($"Exception in OnDeviceMessageReceived: {Utilities.GetExceptionMessage(ex)}");
            }
        }
    }

    /// <summary>
    /// Show list of available commands
    /// </summary>
    protected override void ShowAvailableCommands()
    {
        Utilities.DisplayMessage("\n    Command Messages accepted by this device:\n" +
        $"      {Constants.Commands.IpAddress}: Show IP Address\n" +
        $"      {Constants.Commands.Log}: Upload log file to the cloud\n" +
        $"      {Constants.Commands.Heartbeat}: Send a heartbeat to the cloud\n" +
        $"      {Constants.Commands.PodBayDoors}: Open the Pod Bay Doors\n" +
        $"      {Constants.Commands.ReadTwin}: Read the device twin\n" +
        $"      {Constants.Commands.WriteTwin}: Write to the device twin\n",
        ConsoleColor.Yellow);
    }

    /// <summary>
    /// Process Command/Message received from cloud
    /// </summary>
    private async Task<bool> ProcessCommand(DeviceClient client, Message msg, string data, IDictionary<string, string> messageProperties)
    {
        var messageHandled = false;
        var commandText = string.Empty;
        try
        {
            //var propCount = 0;
            //foreach (var prop in messageProperties)
            //{
            //    Utilities.DisplayMessage($"    Property[{propCount++}] Key={prop.Key} : Value={prop.Value}", ConsoleColor.Yellow);
            //}
            if (data.Trim().StartsWith("{") && data.Trim().EndsWith("}"))
            {
                var commandObject = JObject.Parse(data);
                commandText = commandObject.GetValue("command").ToString();
            }
            if (string.IsNullOrEmpty(commandText))
            {
                Utilities.DisplayMessage("  No valid command structure found!");
                messageHandled = false;
            }
            else
            {
                commandText = Utilities.IsOnlyNumbersOrLetters(commandText, 25).Trim();

                if (commandText.Contains(Constants.Commands.IpAddress, StringComparison.InvariantCultureIgnoreCase))
                {
                    RaiseCommand(Constants.Commands.IpAddress);
                    messageHandled = true;
                }
                if (!messageHandled && commandText.Contains(Constants.Commands.Log, StringComparison.InvariantCultureIgnoreCase))
                {
                    RaiseCommand(Constants.Commands.Log);
                    messageHandled = true;
                }
                if (!messageHandled && (commandText.Contains(Constants.Commands.FirmwareUpdate, StringComparison.InvariantCultureIgnoreCase) || (commandText.Contains("firmware", StringComparison.InvariantCultureIgnoreCase) && commandText.Contains("update", StringComparison.InvariantCultureIgnoreCase))))
                {
                    (var desired, var reported) = await ReadDeviceTwin(false);
                    RaiseCommandWithProperties(Constants.Commands.FirmwareUpdate, desired);
                    messageHandled = true;
                }
                if (!messageHandled && commandText.Contains(Constants.Commands.Heartbeat, StringComparison.InvariantCultureIgnoreCase))
                {
                    RaiseCommand(Constants.Commands.Heartbeat);
                    messageHandled = true;
                }
                if (!messageHandled && (commandText.Contains(Constants.Commands.PodBayDoors, StringComparison.InvariantCultureIgnoreCase) || (commandText.Contains("pod", StringComparison.InvariantCultureIgnoreCase) && commandText.Contains("bay", StringComparison.InvariantCultureIgnoreCase))))
                {
                    RaiseCommand(Constants.Commands.PodBayDoors);
                    messageHandled = true;
                }
                if (!messageHandled && (commandText.Contains(Constants.Commands.ReadTwin, StringComparison.InvariantCultureIgnoreCase) || (commandText.Contains("twin", StringComparison.InvariantCultureIgnoreCase) && commandText.Contains("read", StringComparison.InvariantCultureIgnoreCase))))
                {
                    RaiseCommand(Constants.Commands.ReadTwin);
                    messageHandled = true;
                }
                if (!messageHandled && (commandText.Contains(Constants.Commands.WriteTwin, StringComparison.InvariantCultureIgnoreCase) || (commandText.Contains("twin", StringComparison.InvariantCultureIgnoreCase) && (commandText.Contains("write", StringComparison.InvariantCultureIgnoreCase) || commandText.Contains("update", StringComparison.InvariantCultureIgnoreCase)))))
                {
                    RaiseCommand(Constants.Commands.WriteTwin);
                    messageHandled = true;
                }
            }
            if (!messageHandled)
            {
                RaiseOnMessageReceived(data);
            }

            // mark the message as handled, rejected, or abandoned
            //await client.CompleteAsync(message); // marks the message as handled
            //await client.RejectAsync(message);   // drops the message as unhandled
            //await client.AbandonAsync(message);  // puts message back on queue

            // MQTT protocol does not support complete/reject message
            var isMqtt = TransportProtocol == TransportType.Mqtt || TransportProtocol == TransportType.Mqtt_Tcp_Only || TransportProtocol == TransportType.Mqtt_WebSocket_Only;
            if (!isMqtt)
            {
                if (messageHandled)
                {
                    await client.CompleteAsync(msg);
                }
                else
                {
                    await client.RejectAsync(msg);
                }
            }
            return messageHandled;
        }
        catch (Exception ex)
        {
            Utilities.DisplayErrorMessage($"Exception in ProcessCommand: {Utilities.GetExceptionMessage(ex)}");
            return false;
        }
    }

    /// <summary>
    /// Send Log record to Cloud
    /// </summary>
    private async Task<bool> SendLogEntry(string eventTypeCode, string eventTypeSubCode, string data)
    {
        try
        {
            Utilities.DisplayMessage($"\n      Sending Log Entry...", ConsoleColor.Yellow);
            var logEntry = JsonConvert.SerializeObject(new LogData(DeviceId, eventTypeCode, eventTypeSubCode, data));
            Utilities.DisplayMessage($"        {logEntry}", ConsoleColor.Blue);
            return await SendMessage(logEntry);
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            Utilities.DisplayErrorMessage($"Exception while sending log entry: {Utilities.GetExceptionMessage(ex)}");
            return false;
        }
    }
}
