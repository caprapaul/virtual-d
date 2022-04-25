using Windows.Win32.Foundation;

namespace VirtualD.Entities;

[Serializable]
public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<HWND> Windows { get; set; } = new();
}