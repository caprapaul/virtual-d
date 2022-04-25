using NHotkey;
using NHotkey.WindowsForms;
using VirtualD.Entities;
using VirtualD.Properties;
using VirtualD.Services;

namespace VirtualD;

public class AppContext : ApplicationContext
{
    private readonly Form1 _form;
    private readonly NotifyIcon _trayIcon;
    private readonly WorkspaceService _workspaceService;
    private readonly HotkeyService _hotkeyService;

    public AppContext(WorkspaceService workspaceService, Form1 form, HotkeyService hotkeyService)
    {
        _workspaceService = workspaceService;
        _form = form;
        _hotkeyService = hotkeyService;

        _trayIcon = new NotifyIcon
        {
            Icon = Resources.die,
            ContextMenuStrip = new ContextMenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Configuration", null, ShowConfig),
                    new ToolStripMenuItem("Exit", null, Exit)
                }
            },
            Visible = true,
        };
        
        _trayIcon.DoubleClick += ShowConfig;
        
        Init();
    }
    
    public void Init()
    {
        // PInvoke.AllocConsole();
        
        // HotkeyManager.Current.AddOrReplace("New", Keys.Alt | Keys.Z, OnNewWorkspace);
        // HotkeyManager.Current.AddOrReplace("EnableWindows", Keys.Control | Keys.Alt | Keys.Q, OnEnableWindows);
        // HotkeyManager.Current.AddOrReplace("Reset", Keys.Control | Keys.Alt | Keys.Q, OnReset);
    }

    private void ShowConfig(object? sender, EventArgs e)
    {
        _hotkeyService.UnregisterHotkeys();
        _form.Init();
        
        if (_form.Visible)
        {
            _form.Activate();
        }
        else
        {
            _form.ShowDialog();
        }
        
        _hotkeyService.RegisterHotkeys();
    }

    private void OnReset(object? sender, HotkeyEventArgs e)
    {
        _workspaceService.Reset();
        
        e.Handled = true;
    }

    private void OnNewWorkspace(object? sender, HotkeyEventArgs e)
    {
        _workspaceService.AddWorkspaceToFocusedMonitor(new Workspace());

        e.Handled = true;
    }

    private void Exit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }
}