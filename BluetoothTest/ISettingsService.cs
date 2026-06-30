namespace BluetoothTest;

public interface ISettingsService
{
    string? Get(string key);
    void Set(string key, string value);
    void Remove(string key);
}
