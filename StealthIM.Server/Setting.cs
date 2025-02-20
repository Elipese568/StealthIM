using Newtonsoft.Json;

namespace StealthIM.Server;

internal class Setting
{
    private static Dictionary<string, string> _settingItems;

    public static void Initialize()
    {
        if (!File.Exists("Setting.json"))
        {
            File.Create("Setting.json");
            _settingItems = new Dictionary<string, string>();
        }
        else
            _settingItems = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Setting.json")) ?? new();

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            File.WriteAllText("Setting.json", JsonConvert.SerializeObject(_settingItems, Formatting.Indented));
        };
    }

    public static string Get(string key, string defaultValue = "")
    {
        try
        {
            return _settingItems[key];
        }
        catch
        {
            _settingItems.Add(key, defaultValue);
            return defaultValue;
        }
    }

    public static void Set(string key, string value)
    {
        if (!_settingItems.TryAdd(key, value))
            _settingItems[key] = value;
    }

    public static bool Contains(string key)
    {
        return _settingItems.ContainsKey(key);
    }
}
