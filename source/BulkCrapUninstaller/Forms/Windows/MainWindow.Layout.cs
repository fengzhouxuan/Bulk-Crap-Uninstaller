// Main window layout for the modified BCUninstaller build (Apache 2.0).

using System.Drawing;
using System.Windows.Forms;
using Klocman.Forms.Tools;

namespace BulkCrapUninstaller.Forms
{
    internal sealed partial class MainWindow
    {
        private static readonly Color WorkspaceBackground = Color.FromArgb(241, 245, 249);
        private static readonly Color SidebarBackground = Color.FromArgb(248, 250, 252);
        private static readonly Color HeaderBackground = Color.FromArgb(221, 235, 247);
        private static readonly Color HeadingText = Color.FromArgb(23, 36, 53);
        private static readonly Color WorkspaceBorder = Color.FromArgb(215, 224, 233);
        private static readonly Color PrimaryAction = Color.FromArgb(7, 111, 103);
        private static readonly Color PrimaryActionHover = Color.FromArgb(6, 84, 79);
        private static readonly Color MutedText = Color.FromArgb(73, 89, 107);

        private TableLayoutPanel _workspaceHeader;
        private Panel _searchSurface;

        private Padding ScalePadding(int horizontal, int vertical)
        {
            return new Padding(LogicalToDeviceUnits(horizontal), LogicalToDeviceUnits(vertical),
                LogicalToDeviceUnits(horizontal), LogicalToDeviceUnits(vertical));
        }

        private void InitializeWorkspaceLayout()
        {
            SuspendLayout();
            settingsSidebarPanel.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            toolStrip.SuspendLayout();

            _workspaceHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(66),
                Padding = ScalePadding(19, 0),
                ColumnCount = 2,
                RowCount = 1,
                TabIndex = 0
            };
            _workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            _workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            _workspaceHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleFont = new Font(Font.FontFamily, 19, FontStyle.Bold);
            label1.Font = titleFont;
            label1.Disposed += (_, _) => titleFont.Dispose();
            label1.Dock = DockStyle.Fill;
            label1.AutoSize = false;
            label1.AutoEllipsis = true;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label1.Margin = Padding.Empty;
            _workspaceHeader.Controls.Add(label1, 0, 0);

            var searchField = filterEditor1.SearchField;
            filterEditor1.Height -= searchField.Height;
            groupBox1.Height -= searchField.Height;
            _searchSurface = new Panel
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = Padding.Empty,
                Height = LogicalToDeviceUnits(36),
                Padding = ScalePadding(10, 6)
            };
            searchField.Dock = DockStyle.Fill;
            var searchIcon = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = LogicalToDeviceUnits(25),
                Padding = ScalePadding(3, 3),
                Image = Properties.Resources.magnify,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            searchIcon.Click += searchToolStripMenuItem_Click;
            _searchSurface.Click += searchToolStripMenuItem_Click;
            _searchSurface.Controls.Add(searchField);
            _searchSurface.Controls.Add(searchIcon);
            _workspaceHeader.Controls.Add(_searchSurface, 1, 0);
            Controls.Add(_workspaceHeader);
            Controls.SetChildIndex(_workspaceHeader, Controls.GetChildIndex(menuStrip));
            settingsSidebarPanel.Padding = ScalePadding(12, 10);
            settingsSidebarPanel.AutoScroll = true;
            splitContainer1.Panel2.Padding = ScalePadding(12, 0);
            menuStrip.Padding = ScalePadding(12, 3);

            // Keep the existing commands and put the common operations first.
            toolStrip.Items.Clear();
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                toolStripButtonUninstall, toolStripButton2, toolStripButtonProperties, toolStripSeparator4,
                toolStripButton1, toolStripSeparator22, toolStripButtonSelAll, toolStripButtonSelNone,
                toolStripButtonSelInv, toolStripSeparator23, toolStripButtonTarget, toolStripButtonModify,
                toolStripSeparator21, toolStripButton7, toolStripButton8
            });
            toolStripSeparator24.Dispose();
            toolStrip.Padding = ScalePadding(0, 8);
            toolStrip.ImageScalingSize = new Size(LogicalToDeviceUnits(17), LogicalToDeviceUnits(17));
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton)
                {
                    item.Margin = ScalePadding(2, 0);
                    item.Padding = ScalePadding(6, 5);
                }
            }

            toolStripButtonUninstall.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonUninstall.Padding = ScalePadding(14, 5);
            toolStripButtonUninstall.Overflow = ToolStripItemOverflow.Never;
            toolStripButtonUninstall.Enabled = false;
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton2.Enabled = false;
            toolStripButtonProperties.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonProperties.Enabled = false;
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;

            listViewPanel.BorderStyle = BorderStyle.None;
            listViewPanel.Padding = ScalePadding(1, 1);
            uninstallerObjectListView.GridLines = false;
            uninstallerObjectListView.RowHeight = LogicalToDeviceUnits(27);

            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = ScalePadding(0, 5);
            foreach (ToolStripStatusLabel item in statusStrip1.Items)
            {
                item.BorderSides = ToolStripStatusLabelBorderSides.None;
                item.Padding = ScalePadding(5, 0);
            }

            toolStrip.ResumeLayout();
            splitContainer1.Panel2.ResumeLayout();
            settingsSidebarPanel.ResumeLayout();
            ResumeLayout();
        }

        private void ApplyWorkspaceTheme(bool useSystemStyle)
        {
            bool useNativeColors = useSystemStyle || SystemInformation.HighContrast;
            BackColor = useNativeColors ? SystemColors.Control : WorkspaceBackground;
            settingsSidebarPanel.BackColor = useNativeColors ? SystemColors.Control : SidebarBackground;
            _workspaceHeader.BackColor = useNativeColors ? SystemColors.Control : HeaderBackground;
            label1.ForeColor = useNativeColors ? SystemColors.ControlText : HeadingText;
            _searchSurface.BackColor = useNativeColors ? SystemColors.Window : Color.White;
            var searchField = filterEditor1.SearchField;
            searchField.BackColor = _searchSurface.BackColor;
            searchField.InactiveSearchColor = useNativeColors ? SystemColors.GrayText : MutedText;
            searchField.SearchBoxBorderStyle = useNativeColors ? BorderStyle.FixedSingle : BorderStyle.None;
            listViewPanel.BackColor = useNativeColors ? SystemColors.ControlDark : WorkspaceBorder;
            uninstallerObjectListView.UseAlternatingBackColors = !useNativeColors;
            uninstallerObjectListView.AlternateRowBackColor = SidebarBackground;

            if (useNativeColors)
            {
                toolStrip.RenderMode = ToolStripRenderMode.System;
                menuStrip.RenderMode = ToolStripRenderMode.System;
                statusStrip1.RenderMode = ToolStripRenderMode.System;
            }
            else
            {
                var renderer = new WorkspaceStripRenderer(toolStripButtonUninstall);
                toolStrip.Renderer = renderer;
                menuStrip.Renderer = renderer;
                statusStrip1.Renderer = renderer;
            }
        }

        protected override void OnSystemColorsChanged(System.EventArgs e)
        {
            base.OnSystemColorsChanged(e);
            if (_styleController != null)
                ApplyWorkspaceTheme(_setMan.Selected.Settings.WindowUseSystemTheme);
        }

        private sealed class WorkspaceStripRenderer : ToolStripProfessionalRenderer
        {
            private readonly ToolStripButton _uninstall;

            public WorkspaceStripRenderer(ToolStripButton uninstall) : base(new WorkspaceColorTable())
            {
                _uninstall = uninstall;
                RoundedEdges = false;
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item != _uninstall)
                {
                    base.OnRenderButtonBackground(e);
                    return;
                }

                var fillColor = !e.Item.Enabled ? WorkspaceBorder
                    : e.Item.Selected || e.Item.Pressed ? PrimaryActionHover : PrimaryAction;
                using var fill = new SolidBrush(fillColor);
                e.Graphics.FillRectangle(fill, new Rectangle(Point.Empty, e.Item.Size));
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (e.Item == _uninstall)
                    e.TextColor = e.Item.Enabled ? Color.White : MutedText;
                base.OnRenderItemText(e);
            }
        }

        private sealed class WorkspaceColorTable : StandardSystemColorTable
        {
            public override Color MenuStripGradientBegin => SidebarBackground;
            public override Color MenuStripGradientEnd => SidebarBackground;
            public override Color ToolStripGradientBegin => WorkspaceBackground;
            public override Color ToolStripGradientMiddle => WorkspaceBackground;
            public override Color ToolStripGradientEnd => WorkspaceBackground;
            public override Color ToolStripBorder => WorkspaceBackground;
            public override Color StatusStripGradientBegin => WorkspaceBackground;
            public override Color StatusStripGradientEnd => WorkspaceBackground;
            public override Color ButtonSelectedGradientBegin => WorkspaceBorder;
            public override Color ButtonSelectedGradientMiddle => WorkspaceBorder;
            public override Color ButtonSelectedGradientEnd => WorkspaceBorder;
            public override Color ButtonSelectedBorder => WorkspaceBorder;
            public override Color SeparatorDark => WorkspaceBorder;
            public override Color SeparatorLight => WorkspaceBackground;
        }
    }
}
