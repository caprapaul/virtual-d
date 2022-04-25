using NHotkey.WindowsForms;
using VirtualD.Entities;
using VirtualD.Extensions;
using VirtualD.Services;

namespace VirtualD
{
    public partial class Form1 : Form
    {
        private readonly WorkspaceService _workspaceService;
        private readonly HotkeyService _hotkeyService;

        public Form1(WorkspaceService workspaceService, HotkeyService hotkeyService)
        {
            _workspaceService = workspaceService;
            _hotkeyService = hotkeyService;

            InitializeComponent();
            Init();
        }

        public void Init()
        {
            LoadWorkspaces();
            LoadHotkeys();
        }

        private void LoadHotkeys()
        {
            var converter = new KeysConverter();
            var keyCode = _hotkeyService.GetHotkey(HotkeyName.NextWorkspace)?.KeyCode;
            
            nextWorkspaceInput.Text = keyCode is not null ? converter.ConvertToString(keyCode) : "Not Set";
            nextWorkspaceInput.KeyDown += NextWorkspaceInput_KeyDown;
            nextWorkspaceInput.PreviewKeyDown += NextWorkspaceInput_PreviewKeyDown;
        }

        private void NextWorkspaceInput_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData.HasFlag(Keys.Tab))
            {
                e.IsInputKey = true;
            }
        }

        private void NextWorkspaceInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                e.Handled = false;
                e.SuppressKeyPress = true;

                nextWorkspaceInput.Text = "";
                return;
            }

            var modifierKeys = e.Modifiers;
            var pressedKey = e.KeyData ^ modifierKeys; //remove modifier keys

            if (modifierKeys == Keys.None || pressedKey == Keys.None)
            {
                return;
            }
            
            var converter = new KeysConverter();
            nextWorkspaceInput.Text = converter.ConvertToString(e.KeyData);
            
            _hotkeyService.AddHotkey(new Hotkey
            {
                Name = HotkeyName.NextWorkspace,
                KeyCode = e.KeyData,
            });
            
            e.Handled = true;
        }

        private void LoadWorkspaces()
        {
            Cursor = Cursors.WaitCursor;
            
            monitorsTree.Nodes.Clear();
            monitorsTree.LabelEdit = true;
            
            monitorsTree.AfterLabelEdit += MonitorsTreeOnAfterLabelEdit;
            monitorsTree.NodeMouseDoubleClick += MonitorsTreeOnNodeMouseDoubleClick;
            
            // var node = monitorsTree.Nodes.Add("Monitors");
            var i = 0;
            
            foreach (var monitor in _workspaceService.GetAllMonitors())
            {
                var monitorNode = monitorsTree.Nodes.Add($"Monitor {i}");
                monitorNode.Tag = i;
                monitorNode.Expand();

                foreach (var workspace in monitor.Workspaces)
                {
                    var workspaceNode = monitorNode.Nodes.Add(workspace.Name);
                    workspaceNode.Tag = workspace.Id;

                    foreach (var window in workspace.Windows)
                    {
                        var windowName = User32Extensions.GetWindowText(window);
                        workspaceNode.Nodes.Add(windowName);
                    }
                }

                i++;
            }
            
            Cursor = Cursors.Default;
            Update();
        }

        private void MonitorsTreeOnNodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is not Guid)
            {
                return;
            }
            
            e.Node.BeginEdit();
        }

        private void MonitorsTreeOnAfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
        {
            var name = e.Label;

            if (name is null)
            {
                e.Node.EndEdit(true);
                return;
            }

            var selectedWorkspaceId = GetSelectedWorkspaceId()!;
            
            _workspaceService.UpdateWorkspace(new Workspace
            {
                Id = selectedWorkspaceId.Value,
                Name = name
            });
            
            e.Node.EndEdit(false);
            LoadWorkspaces();
        }

        private TreeNode GetSelectedMonitorNode()
        {
            var selectedNode = monitorsTree.SelectedNode;
            
            while (selectedNode.Parent != null)
            {
                selectedNode = selectedNode.Parent;
            }

            return selectedNode;
        }
        
        private Guid? GetSelectedWorkspaceId()
        {
            var selectedNode = monitorsTree.SelectedNode;
            var selectedId = selectedNode.Tag as Guid?;
            
            return selectedId;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var monitorNode = GetSelectedMonitorNode();
            var monitorId = (int) monitorNode.Tag;

            var workspaceNode = monitorNode.Nodes.Add("New Workspace");
            workspaceNode.Tag = monitorId;
            workspaceNode.BeginEdit();
            
            _workspaceService.AddWorkspaceToMonitor(monitorId, new Workspace());

            //LoadWorkspaces();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            _workspaceService.Reset();
            
            LoadWorkspaces();
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            var selectedWorkspaceId = GetSelectedWorkspaceId();

            if (selectedWorkspaceId is null)
            {
                return;
            }
            
            _workspaceService.RemoveWorkspace(selectedWorkspaceId.Value);
            LoadWorkspaces();
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            _hotkeyService.Apply();
        }
    }
}