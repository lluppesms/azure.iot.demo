namespace IoT.Dashboard.Helpers;

using Constants = IoT.Dashboard.Models.Constants;
public class SessionStorageService
{
    readonly ILocalStorageService localStorageSvc;

    public async Task<SessionState> GetState()
    {
        SessionState state = new();
        var json = await localStorageSvc.GetItemAsync<string>(Constants.LocalStorage.SessionObject);
        if (!string.IsNullOrEmpty(json))
        {
            if (json.Contains('~'))
            {
                json = json.Replace("~", "\"");
            }
            state = JsonConvert.DeserializeObject<SessionState>(json);
        }
        return state;
    }

    public async Task StoreState(SessionState state)
    {
        var json = JsonConvert.SerializeObject(state);
        json = json.Replace("\"", "~");
        await localStorageSvc.SetItemAsync(Constants.LocalStorage.SessionObject, json);
    }

    public async Task RemoveState()
    {
        await localStorageSvc.RemoveItemAsync(Constants.LocalStorage.SessionObject);
    }

    public SessionStorageService(ILocalStorageService svc)
    {
        localStorageSvc = svc;
    }
}
