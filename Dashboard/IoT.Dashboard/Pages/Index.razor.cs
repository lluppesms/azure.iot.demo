namespace IoT.Dashboard.Pages;
using Constants = Models.Constants;

public partial class Dashboard : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] DeviceRepository DeviceService { get; set; }
    [Inject] SweetAlertService Sweet { get; set; }
    [Inject] AppSettings settings { get; set; }
    [Inject] ILocalStorageService LocalStorageSvc { get; set; }
    [Inject] IToaster Toaster { get; set; }
    [Inject] IJSRuntime JS { get; set; }

    protected SessionStorageService SessionStorage = null;
    protected SessionState sessionState = null;

    protected string selectedTab = "deviceState";
    protected string selectedDeviceId = string.Empty;
    protected string lastRefreshTwin = string.Empty;
    protected string lastRefreshData = string.Empty;
    protected string lastRefreshFiles = string.Empty;

    protected List<string> deviceIdList = new();
    protected List<DeviceData> dataList = new();
    protected List<DeviceFile> fileList = new();

    protected DeviceData selectedDataRow = null;
    protected DeviceFile selectedFileRow = null;
    protected string selectedFileName = string.Empty;

    protected string selectedEventType = string.Empty;
    protected string selectedDateRange = "24";
    protected string selectedMaxRows = "50";

    protected bool hideDeviceState = true;
    protected bool hideDeviceChangeProperty = true;
    protected bool hideDeviceData = true;
    protected bool hideDeviceFiles = true;
    protected bool hideCustomCommand = true;

    protected string deviceTwinReportedValue = string.Empty;
    protected string deviceTwinDesiredValue = string.Empty;
    protected string newDesiredPropertyName = string.Empty;
    protected string newDesiredPropertyValue = string.Empty;
    protected string newMessageValue = string.Empty;
    protected string commandSelection = string.Empty;
    protected string customCommandValue = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // --> this always seems to fire twice for Server apps, so move logic to OnAfterRender
        await base.OnInitializedAsync().ConfigureAwait(true);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // --> this also seems to fire twice for Server apps, but there is a flag, so only do this once...
        await Task.FromResult(true);
        if (firstRender)
        {
            await GetSession();
            await GetDeviceList();
            StateHasChanged();
        }
    }
    protected async Task OnSelectedTabChanged(string name)
    {
        selectedTab = name;
        switch (selectedTab)
        {
            case Constants.DashboardTabs.State:
                break;
            case Constants.DashboardTabs.Commands:
                break;
            case Constants.DashboardTabs.Files:
                await GetFiles();
                break;
            case Constants.DashboardTabs.Data:
                await GetDataRecords();
                break;
        }
        return;
    }

    protected async Task GetDeviceList()
    {
        hideDeviceState = true;
        deviceIdList = await DeviceService.GetListOfDevices();
        selectedDeviceId = deviceIdList.FirstOrDefault();

        // find last device from sessionState...
        if (!string.IsNullOrEmpty(sessionState.LastDevice))
        {
            var deviceExists = deviceIdList.Find(d => d == sessionState.LastDevice);
            if (!string.IsNullOrEmpty(deviceExists))
            {
                selectedDeviceId = sessionState.LastDevice;
            }
        }

        await GetTwin(selectedDeviceId);
        hideDeviceState = false;
    }
    protected async Task OnDeviceSelected(ChangeEventArgs e)
    {
        hideDeviceState = true;
        selectedTab = Constants.DashboardTabs.State;
        selectedDeviceId = e?.Value.ToStringNullable();
        newDesiredPropertyName = string.Empty;
        newDesiredPropertyValue = string.Empty;
        deviceTwinReportedValue = string.Empty;
        deviceTwinDesiredValue = string.Empty;
        newMessageValue = string.Empty;
        await GetTwin(selectedDeviceId);
        sessionState.LastDevice = selectedDeviceId;
        await SaveSession();
        hideDeviceState = false;
        return;
    }

    protected async Task GetTwin(string deviceId)
    {
        hideDeviceState = true;
        deviceTwinReportedValue = string.Empty;
        deviceTwinDesiredValue = string.Empty;
        if (!string.IsNullOrEmpty(selectedDeviceId))
        {
            lastRefreshTwin = $"Last Refresh: {DateTime.Now:hh:mm:ss}";
            var twin = await DeviceService.ReadDeviceTwin(deviceId);
            if (twin != null)
            {
                deviceTwinReportedValue = twin.Properties.Reported.ToStringNullable();
                deviceTwinDesiredValue = twin.Properties.Desired.ToStringNullable();
            }
        }
        hideDeviceState = false;
        hideDeviceChangeProperty = false;
    }
    protected async Task UpdateTwin()
    {
        if (string.IsNullOrEmpty(newDesiredPropertyName) || string.IsNullOrEmpty(newDesiredPropertyValue))
        {
            Toaster.Warning("Please enter both a property and value! To remove a property enter 'null' in the value.");
            return;
        }
        hideDeviceChangeProperty = true;
        Toaster.Info($"Updating Device Twin property {newDesiredPropertyName} to '{newDesiredPropertyValue}'!");
        var twin = await DeviceService.WriteDeviceTwinProperty(selectedDeviceId, newDesiredPropertyName, newDesiredPropertyValue);
        lastRefreshTwin = $"Last Refresh: {DateTime.Now:hh:mm:ss}";
        deviceTwinReportedValue = (twin != null) ? twin.Properties.Reported.ToStringNullable() : string.Empty;
        deviceTwinDesiredValue = (twin != null) ? twin.Properties.Desired.ToStringNullable() : string.Empty;
        hideDeviceChangeProperty = false;
    }

    protected void OnCommandChanged(ChangeEventArgs e)
    {
        var currentCommand = e?.Value.ToStringNullable();
        hideCustomCommand = currentCommand != "CUSTOM";
        commandSelection = currentCommand;
    }
    protected async Task CallDirectMethod(string methodName)
    {
        Toaster.Info($"Calling method {methodName} on {selectedDeviceId}!");
        (var success, var methodResult) = await DeviceService.CallDirectMethod(selectedDeviceId, methodName, "Called from Dashboard!");
        Toaster.Info($"Method {methodName} returned: {methodResult}");
    }
    protected async Task SendCommandToDevice()
    {
        if (string.IsNullOrEmpty(commandSelection))
        {
            Toaster.Warning($"Please select a command to send!!!");
            return;
        }
        if (commandSelection == "CUSTOM")
        {
            Toaster.Info($"Send custom command {customCommandValue} to {selectedDeviceId}!");
            var success = await DeviceService.SendCommand(selectedDeviceId, customCommandValue);
        }
        else
        {
            Toaster.Info($"Send command {commandSelection} to {selectedDeviceId}!");
            var success = await DeviceService.SendCommand(selectedDeviceId, commandSelection);
        }
    }
    protected async Task SendMessageToDevice()
    {
        if (string.IsNullOrEmpty(newMessageValue))
        {
            Toaster.Warning($"Please enter a message to send!!!");
            return;
        }
        Toaster.Info($"Sending message to {selectedDeviceId}!");
        var success = await DeviceService.SendMesssage(selectedDeviceId, newMessageValue);
    }

    protected async Task OnDataTypeSelected(ChangeEventArgs e)
    {
        selectedEventType = e?.Value.ToStringNullable();
        await GetDataRecords();
    }
    protected async Task OnDataHoursSelected(ChangeEventArgs e)
    {
        selectedDateRange = e?.Value.ToStringNullable();
        await GetDataRecords();
    }
    protected async Task OnDataCountSelected(ChangeEventArgs e)
    {
        selectedMaxRows = e?.Value.ToStringNullable();
        await GetDataRecords();
    }
    protected async Task GetDataRecords()
    {
        hideDeviceData = true;
        if (dataList == null) { dataList = new List<DeviceData>(); } else { dataList.Clear(); }
        StateHasChanged();
        var maxRows = int.Parse(selectedMaxRows);
        var subtractFromStart = 0;
        var subtractFromEnd = 0;
        if (selectedDateRange.Contains("-"))
        {
            var hyphen = selectedDateRange.IndexOf("-");
            var afterHyphen = hyphen + 1;
            subtractFromStart = int.Parse(selectedDateRange[..hyphen]);
            subtractFromEnd = int.Parse(selectedDateRange[afterHyphen..]);
        }
        else
        {
            subtractFromStart = int.Parse(selectedDateRange);
        }
        string getDataMsg;
        (dataList, getDataMsg) = await DeviceService.GetDataList(selectedDeviceId, selectedEventType, subtractFromStart, subtractFromEnd, maxRows);
        lastRefreshData = $"Last Refresh: {DateTime.Now:hh:mm:ss}  {getDataMsg}";
        StateHasChanged();
        Toaster.Info(getDataMsg);
        hideDeviceData = false;
        return;
    }
    protected async void GetDataRecord(DataGridRowMouseEventArgs<DeviceData> f)
    {
        if (f != null & f.Item != null)
        {
            (var record, var msg) = await DeviceService.GetDataRecord(f.Item.MessageId, f.Item.PartitionKey);
            await Utilities.ShowSweetPopupHtml(Sweet, "Raw Cosmos Data", record);
        }
    }

    protected async Task GetFiles()
    {
        hideDeviceFiles = true;
        if (fileList == null) { fileList = new List<DeviceFile>(); } else { fileList.Clear(); }
        StateHasChanged();
        string getFilesMsg;
        (fileList, getFilesMsg) = await DeviceService.GetRecentFiles(selectedDeviceId);
        lastRefreshFiles = $"Last Refresh: {DateTime.Now:hh:mm:ss}  {getFilesMsg}";
        StateHasChanged();
        Toaster.Info(getFilesMsg);
        hideDeviceFiles = false;
        return;
    }
    protected async void GetFile(DataGridRowMouseEventArgs<DeviceFile> f)
    {
        if (f != null & f.Item != null) 
        {
            var options = new SweetAlertOptions
            {
                Title = "Download this file?",
                Text = f.Item.ShortFileName,
                ShowCancelButton = true,
                ConfirmButtonText = "Download",
                CancelButtonText = "Cancel",
                FocusCancel = true
            };
            if (await Utilities.ShowSweetPrompt(Sweet, options))
            {
                await DownloadFile(f.Item.FileName);
            }
        }
    }
    private async Task DownloadFile(string fileName)
    {
        var fileStream = await DeviceService.GetFileStream(fileName);
        var success = fileStream != null;

        if (success)
        {
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }

    protected async Task<bool> GetSession()
    {
        if (SessionStorage == null) { SessionStorage = new SessionStorageService(LocalStorageSvc); }
        sessionState = await SessionStorage.GetState();
        return true;
    }
    protected async Task<bool> SaveSession()
    {
        if (SessionStorage == null) { SessionStorage = new SessionStorageService(LocalStorageSvc); }
        await SessionStorage.StoreState(sessionState);
        return true;
    }
}
