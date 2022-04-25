using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using JsonFlatFileDataStore;
using Newtonsoft.Json;
using VirtualD.Entities;
using VirtualD.Extensions;
using Monitor = VirtualD.Entities.Monitor;

namespace VirtualD.Services;

public class WorkspaceService
{
    private const string DataPath = "data.json";

    private List<Monitor> _monitors = new();
    private readonly HotkeyService _hotkeyService;
    
    public WorkspaceService(HotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;
        Init();
    }

    public void Init()
    {
        // PInvoke.AllocConsole();
        _hotkeyService[HotkeyName.NextWorkspace] += (_, _) => NextWorkspace();
        
        var monitorHandles = User32Extensions.GetMonitors().ToArray();
        LoadFromFile();

        for (var i = _monitors.Count; i < monitorHandles.Length; i++)
        {
            var workspaces = new List<Workspace>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Default"
                }
            };
            
            _monitors.Add(new Monitor
            {
                Id = _monitors.Count,
                Workspaces = workspaces,
                ActiveWorkspaceId = workspaces[0].Id
            });
        }
        

        for (var i = 0; i < _monitors.Count; i++)
        {
            _monitors[i].Handle = monitorHandles[i];
        }
        
        foreach (var monitor in _monitors)
        {
            foreach (var workspace in monitor.Workspaces)
            {
                workspace.Windows.RemoveAll(w => !PInvoke.IsWindow(w));
            }
        }
        
        Reset();
    }

    public IEnumerable<Monitor> GetAllMonitors()
    {
        return _monitors;
    }
    
    public Guid AddWorkspaceToMonitor(int monitorId, Workspace workspace)
    {
        var monitor = _monitors[monitorId];
        
        workspace.Id = Guid.NewGuid();

        if (workspace.Name.Length == 0)
        {
            workspace.Name = "Workspace " + (monitor.Workspaces.Count);
        }

        monitor.Workspaces.Add(workspace);
        SaveToFile();

        return workspace.Id;
    }

    public void AddWorkspaceToFocusedMonitor(Workspace workspace)
    {
        var monitor = GetFocusedMonitor()!;
        
        workspace.Id = Guid.NewGuid();

        monitor.Workspaces.Add(workspace);
        SaveToFile();
    }

    public void UpdateWorkspace(Workspace updatedWorkspace)
    {
        foreach (var monitor in _monitors)
        {
            var workspace = monitor.Workspaces.FirstOrDefault(w => w.Id == updatedWorkspace.Id);

            if (workspace is null)
            {
                continue;
            }

            workspace.Name = updatedWorkspace.Name;

            SaveToFile();
        }
    }
    
    public void RemoveWorkspace(Guid id)
    {
        foreach (var monitor in _monitors)
        {
            var workspace = monitor.Workspaces.FirstOrDefault(w => w.Id == id);

            if (workspace is null)
            {
                continue;
            }
            
            foreach (var window in workspace.Windows)
            {
                EnableWindow(window);
            }
            
            monitor.Workspaces.RemoveAll(w => w.Id == id);
            
            SaveToFile();
        }
    }

    public void Reset()
    {
        foreach (var monitor in _monitors)
        {
            foreach (var workspace in monitor.Workspaces)
            {
                foreach (var window in workspace.Windows)
                {
                    EnableWindow(window);
                }
                
                workspace.Windows.Clear();
            }
            
            SaveToFile();
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
        
        _monitors = JsonConvert.DeserializeObject<List<Monitor>>(json);
    }

    private void SaveToFile()
    {
        var json = JsonConvert.SerializeObject(_monitors);
        
        using var writer = new StreamWriter(DataPath);
        writer.Write(json);
    }

    public void NextWorkspace()
    {
        var cursorMonitor = User32Extensions.MonitorFromCursor(MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        var windowsEnumerable = GetMonitorWindows(cursorMonitor);
        var windows = windowsEnumerable as List<HWND> ?? windowsEnumerable.ToList();

        foreach (var window in windows)
        {
            Console.WriteLine(User32Extensions.GetWindowText(window));
        }
        
        var monitor = GetFocusedMonitor()!;

        var oldWorkspace = monitor.Workspaces
            .First(x => x.Id == monitor.ActiveWorkspaceId)!;
        var newWorkspace = monitor.Workspaces
            .SkipWhile(x => x.Id != monitor.ActiveWorkspaceId)
            .Skip(1)
            .DefaultIfEmpty( monitor.Workspaces[0] )
            .First();

        oldWorkspace.Windows = windows;

        foreach (var window in oldWorkspace.Windows)
        {
            DisableWindow(window);
        }

        monitor.ActiveWorkspaceId = newWorkspace.Id;

        foreach (var window in newWorkspace.Windows)
        {
            EnableWindow(window);
        }
        
        SaveToFile();
    }

    private Monitor? GetFocusedMonitor()
    {
        var monitorHandle = User32Extensions.MonitorFromCursor(MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        return _monitors.FirstOrDefault(m => m.Handle == monitorHandle);
    }

    private void EnableWindow(HWND window)
    {
        PInvoke.ShowWindow(window, SHOW_WINDOW_CMD.SW_SHOW);
        PInvoke.EnableWindow(window, true);
    }

    private void DisableWindow(HWND window)
    {
        PInvoke.ShowWindow(window, SHOW_WINDOW_CMD.SW_HIDE);
        PInvoke.EnableWindow(window, false);
    }
    
    private static IEnumerable<HWND> GetMonitorWindows(HMONITOR monitor)
    {
        return User32Extensions.FindWindows((HWND wnd, LPARAM param) =>
        {
            if (!PInvoke.IsWindow(wnd))
            {
                return false;
            }

            if (PInvoke.GetWindowTextLength(wnd) == 0)
            {
                return false;
            }

            if (!PInvoke.IsWindowVisible(wnd))
            {
                return false;
            }

            if (wnd == PInvoke.GetShellWindow())
            {
                return false;
            }

            unsafe
            {
                PInvoke.GetCursorPos(out var cursorPoint);
                var titleBarInfo = new TITLEBARINFO();
                titleBarInfo.cbSize = (uint)sizeof(TITLEBARINFO);
                PInvoke.GetTitleBarInfo(wnd, ref titleBarInfo);
            
                if (Control.MouseButtons == MouseButtons.Left && PInvoke.PtInRect(titleBarInfo.rcTitleBar, cursorPoint))
                {
                    Console.WriteLine(User32Extensions.GetWindowText(wnd));
                    return false;
                }
            }

            var windowMonitor = PInvoke.MonitorFromWindow(wnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

            if (windowMonitor != monitor)
            {
                return false;
            }

            return true;
        });
    }
}