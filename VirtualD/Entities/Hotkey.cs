namespace VirtualD.Entities;

public class Hotkey
{
    public HotkeyName Name { get; set; }
    public Keys KeyCode { get; set; }

    public override int GetHashCode()
    {
        return (int)Name;
    }
}