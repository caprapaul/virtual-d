using Windows.Win32.Graphics.Gdi;

namespace VirtualD.Entities;

[Serializable]
public class Monitor
{
    public int Id { get; set; }
    public HMONITOR Handle { get; set; }
    public Guid ActiveWorkspaceId { get; set; }
    public List<Workspace> Workspaces { get; set; }
}