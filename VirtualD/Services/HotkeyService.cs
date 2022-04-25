using Newtonsoft.Json;
using NHotkey;
using NHotkey.WindowsForms;
using VirtualD.Entities;

namespace VirtualD.Services;

public class HotkeyService
{
    private const string DataPath = "hotkeys.json";

    private List<Hotkey> _hotkeys = new();
    private readonly Dictionary<HotkeyName, EventHandler<HotkeyEventArgs>> _hotkeyHandlers = new();
    
    public HotkeyService()
    {
        LoadFromFile();
        RegisterHotkeys();
    }

    public void AddHotkey(Hotkey hotkey)
    {
        if (_hotkeys.FirstOrDefault(h => h.Name == hotkey.Name) is { } existingHotkey)
        {
            existingHotkey.KeyCode = hotkey.KeyCode;
        }
        else
        {
            _hotkeys.Add(hotkey);
        }
    }

    public void Apply()
    {
        SaveToFile();
    }

    public Hotkey? GetHotkey(HotkeyName name)
    {
        return _hotkeys.FirstOrDefault(h => h.Name == name);
    }
    
    public EventHandler<HotkeyEventArgs> this[HotkeyName name]
    {
        get
        {
            if (!_hotkeyHandlers.TryGetValue(name, out var handler))
            {
                handler = delegate { };
                _hotkeyHandlers.Add(name, handler);
            }

            return handler;
        }
        set => _hotkeyHandlers[name] = value;
    }

    public void UnregisterHotkeys()
    {
        foreach (var hotkey in _hotkeys)
        {
            HotkeyManager.Current.AddOrReplace(hotkey.Name.ToString(), hotkey.KeyCode,  null);
        }
    }

    public void RegisterHotkeys()
    {
        foreach (var hotkey in _hotkeys)
        {
            if (_hotkeyHandlers.TryGetValue(hotkey.Name, out var handler))
            {
                HotkeyManager.Current.AddOrReplace(hotkey.Name.ToString(), hotkey.KeyCode, handler);
            }
            else
            { 
                handler = delegate {};
                HotkeyManager.Current.AddOrReplace(hotkey.Name.ToString(), hotkey.KeyCode, handler);
                _hotkeyHandlers.Add(hotkey.Name, handler);
            }
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(DataPath))
        {
            return;
        }
        
        using var reader = new StreamReader(DataPath);
        var json = reader.ReadToEnd();
        
        _hotkeys = JsonConvert.DeserializeObject<List<Hotkey>>(json);
    }

    private void SaveToFile()
    {
        var json = JsonConvert.SerializeObject(_hotkeys);
        
        using var writer = new StreamWriter(DataPath);
        writer.Write(json);
    }
}