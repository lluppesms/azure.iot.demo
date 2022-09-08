namespace IoT.Simulator;
public partial class Simulator
{
    /// <summary>
    /// Prompt User every few seconds to see what actions they want to take...
    /// </summary>
    public async Task PromptUserForActions(ProcessingParameters parms)
    {
        HeartbeatInterval = parms.HeartbeatInterval;
        // Utilities.DisplayMessage("\nHeartbeat will " + (HeartbeatInterval > 0 ? $"be sent every {HeartbeatInterval} seconds" : "not be sent until you request it"), ConsoleColor.Magenta);

        bool continueProcessing;
        do
        {
            DisplayTopLevelChoices();
            continueProcessing = await WaitForUserInput();
        }
        while (continueProcessing);
    }

    /// <summary>
    /// Displays the prompt for top level choices
    /// </summary>
    private static void DisplayTopLevelChoices()
    {
        Utilities.DisplayConsoleOnlyMessage(
             "\nPress Key: ESC=Quit, P=Pause \n" +
             "  D=Send Data, I=IoT Hub Commands, S=System Commands", ConsoleColor.Green);
    }

    /// <summary>
    /// Evaluates user input.
    /// </summary>
    private async Task<bool> ProcessKeyboardInputs(bool continueLooping)
    {
        if (Console.KeyAvailable)
        {
            //var resetsUntilTimeout = maxTimesToReset;
            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.Escape:
                    continueLooping = false;
                    break;

                case ConsoleKey.P:
                    Utilities.DisplayConsoleOnlyMessage("\nPaused... Press any key to continue...", ConsoleColor.Magenta);
                    var subkeyP = Console.ReadKey(true).Key;
                    break;

                // ---- Heartbeat
                case ConsoleKey.D:
                    Utilities.DisplayConsoleOnlyMessage(
                      "\n    Send Data Packet:" +
                      "\n      H=Heartbeat, R=Resend Register, B=Batch of Heartbeats, Z=Batch of Zipped Heartbeats", ConsoleColor.Cyan);
                    var subkeyD = Console.ReadKey(true).Key;
                    switch (subkeyD)
                    {
                        case ConsoleKey.H:
                            Utilities.DisplayConsoleOnlyMessage("\n    Executing Heartbeat Command...", ConsoleColor.Green);
                            await Execute_SendHeartbeat();
                            break;
                        case ConsoleKey.R:
                            Utilities.DisplayConsoleOnlyMessage("\n    Executing Register Command...", ConsoleColor.Green);
                            await Execute_SendRegistration();
                            break;
                        case ConsoleKey.B:
                            Utilities.DisplayConsoleOnlyMessage("\n    Executing Heartbeat Batch Command...", ConsoleColor.Green);
                            await Execute_SendHeartbeatBatch(false);
                            break;
                        case ConsoleKey.Z:
                            Utilities.DisplayConsoleOnlyMessage("\n    Executing Heartbeat Zipped Batch Command...", ConsoleColor.Green);
                            await Execute_SendHeartbeatBatch(true);
                            break;
                    }
                    break;

                // ---- IoT Hub Commands
                case ConsoleKey.I:
                    Utilities.DisplayConsoleOnlyMessage(
                      "\n    Choose an IoT Hub Command:" +
                      "\n      R=Read Device Twin, W=Write to Twin, ", ConsoleColor.Cyan);
                    var subkeyI = Console.ReadKey(true).Key;
                    switch (subkeyI)
                    {
                        case ConsoleKey.R:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling Display Device Twin Command...", ConsoleColor.Green);
                            await Execute_Read_Device_Twin(true);
                            break;
                        case ConsoleKey.W:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling Write to Device Twin Command...", ConsoleColor.Green);
                            await Execute_Write_Device_Twin();
                            break;
                    }
                    DisplayTopLevelChoices();
                    break;

                // ---- System Commands
                case ConsoleKey.S:
                    Utilities.DisplayConsoleOnlyMessage(
                      "\n    Choose a System Command:" +
                      "\n      F=Firmware Check, U=Upload Log File, I=Show IP Address, P=Open Pod Bay Doors", ConsoleColor.Cyan);
                    var subkeyS = Console.ReadKey(true).Key;
                    switch (subkeyS)
                    {
                        case ConsoleKey.F:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling Firmware Check Command...", ConsoleColor.Green);
                            await Execute_Firmware_Update();
                            break;
                        case ConsoleKey.U:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling File Upload...", ConsoleColor.Green);
                            await Execute_Upload_Log_File();
                            break;
                        case ConsoleKey.I:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling Show IP Address Command...", ConsoleColor.Green);
                            Execute_Show_IpAddress();
                            break;
                        case ConsoleKey.P:
                            Utilities.DisplayConsoleOnlyMessage("\n      Calling Open Pod Bay Doors Command...", ConsoleColor.Green);
                            Execute_OpenPodBayDoors();
                            break;
                    }
                    DisplayTopLevelChoices();
                    break;

                ////case ConsoleKey.LeftArrow:
                ////    ProcessJoystickActions(JoystickKey.Left);
                ////    break;
                ////case ConsoleKey.RightArrow:
                ////    ProcessJoystickActions(JoystickKey.Right);
                ////    break;
                ////case ConsoleKey.UpArrow:
                ////    ProcessJoystickActions(JoystickKey.Up);
                ////    break;
                ////case ConsoleKey.DownArrow:
                ////    ProcessJoystickActions(JoystickKey.Down);
                ////    break;
                ////case ConsoleKey.F1:
                ////    break;
                ////case ConsoleKey.F2:
                ////    break;
                ////case ConsoleKey.F3:
                ////    break;
                ////case ConsoleKey.F4:
                ////    break;
                ////case ConsoleKey.F5:
                ////    break;
                ////case ConsoleKey.F6:
                ////    break;
                ////case ConsoleKey.F7:
                ////    break;
                ////case ConsoleKey.F8:
                ////    break;
                ////case ConsoleKey.F9:
                ////    break;
                ////case ConsoleKey.F10:
                ////    break;
                ////case ConsoleKey.F11:
                ////    break;
                ////case ConsoleKey.F12:
                ////    break;

                ////case ConsoleKey.PageDown:
                ////    break;
                ////case ConsoleKey.PageUp:
                ////    break;
                ////case ConsoleKey.LeftArrow:
                ////    break;
                ////case ConsoleKey.RightArrow:
                ////    break;
                ////case ConsoleKey.UpArrow:
                ////    break;
                ////case ConsoleKey.DownArrow:
                ////    break;
                ////case ConsoleKey.Enter:
                ////    break;
                ////case ConsoleKey.End:
                ////    break;
                ////case ConsoleKey.Tab:
                ////    break;

                default:
                    DisplayTopLevelChoices();
                    break;
            }
        }
        return continueLooping;
    }

    /// <summary>
    /// Waits for user to press a key
    /// </summary>
    private async Task<bool> WaitForUserInput()
    {
        var continueProcessing = true;
        TimeSpan timeLeft;

        var waitTime = DateTime.Now.AddSeconds(10);
        do
        {
            if (HeartbeatInterval > 0)
            {
                var timeToNextHeartbeat = NextHeartbeatTime.Subtract(DateTime.Now);
                if (timeToNextHeartbeat.TotalMilliseconds <= 0)
                {
                    await Execute_SendHeartbeat();
                    NextHeartbeatTime = DateTime.Now.AddSeconds(HeartbeatInterval);
                }
            }
            Thread.Sleep(TimeSpan.FromMilliseconds(250));
            continueProcessing = await ProcessKeyboardInputs(continueProcessing);
            timeLeft = waitTime.Subtract(DateTime.Now);
            if (timeLeft.TotalMilliseconds > 0)
            {
                Console.Write(".", ConsoleColor.White);
            }
        }
        while (timeLeft.TotalMilliseconds > 0 && continueProcessing);
        return continueProcessing;
    }
}
