using Android.App;
using Android.Content;

namespace BluetoothTest.Android;

public class AndroidSettingsService : ISettingsService
{
    private readonly ISharedPreferences _prefs =
        Application.Context.GetSharedPreferences("BluetoothTestPrefs", FileCreationMode.Private)!;

    public string? Get(string key) => _prefs.GetString(key, null);

    public void Set(string key, string value) =>
        _prefs.Edit()!.PutString(key, value)!.Apply();

    public void Remove(string key) =>
        _prefs.Edit()!.Remove(key)!.Apply();
}
