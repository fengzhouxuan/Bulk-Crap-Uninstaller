// Main window layout for the modified BCUninstaller build (Apache 2.0).

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Klocman.Forms.Tools;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    internal sealed partial class MainWindow
    {
        private TableLayoutPanel _workspaceHeader;
        private SearchFieldPanel _searchSurface;
        private ToolStripMenuItem _filtersMenuItem;
        private HeaderFormatStyle _listHeaderStyle;

        private Padding ScalePadding(int horizontal, int vertical)
        {
            return new Padding(LogicalToDeviceUnits(horizontal), LogicalToDeviceUnits(vertical),
                LogicalToDeviceUnits(horizontal), LogicalToDeviceUnits(vertical));
        }

        private void InitializeWorkspaceLayout()
        {
            var resources = new ComponentResourceManager(typeof(MainWindow));
            SuspendLayout();
            settingsSidebarPanel.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            toolStrip.SuspendLayout();

            int headerLayer = Controls.GetChildIndex(menuStrip);
            _workspaceHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(80),
                Padding = ScalePadding(26, 0),
                ColumnCount = 3,
                RowCount = 1,
                TabIndex = 0
            };
            _workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
            _workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
            _workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _workspaceHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleFont = new Font(Font.FontFamily, 23, FontStyle.Bold);
            label1.Font = titleFont;
            label1.Disposed += (_, _) => titleFont.Dispose();
            label1.Text = resources.GetString("WorkspaceInstalledApps");
            label1.Dock = DockStyle.Fill;
            label1.AutoSize = false;
            label1.AutoEllipsis = true;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label1.Margin = Padding.Empty;
            _workspaceHeader.Controls.Add(label1, 0, 0);

            var searchField = filterEditor1.SearchField;
            filterEditor1.Height -= searchField.Height;
            groupBox1.Height -= searchField.Height;
            _searchSurface = new SearchFieldPanel
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, LogicalToDeviceUnits(12), 0),
                Height = LogicalToDeviceUnits(40),
                Padding = ScalePadding(12, 8)
            };
            searchField.Dock = DockStyle.Fill;
            var searchIcon = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = LogicalToDeviceUnits(25),
                Padding = ScalePadding(3, 3),
                BackColor = Color.Transparent,
                Image = Properties.Resources.magnify,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            searchIcon.Click += searchToolStripMenuItem_Click;
            _searchSurface.Click += searchToolStripMenuItem_Click;
            _searchSurface.Controls.Add(searchField);
            _searchSurface.Controls.Add(searchIcon);
            _workspaceHeader.Controls.Add(_searchSurface, 1, 0);

            _filtersMenuItem = new ToolStripMenuItem(resources.GetString("WorkspaceFilters"));
            _filtersMenuItem.Click += (_, _) => displaySettingsToolStripMenuItem.PerformClick();
            var commandsMenu = new ToolStripMenuItem(resources.GetString("WorkspaceMenu"));
            var originalMenus = menuStrip.Items.Cast<ToolStripItem>().ToArray();
            menuStrip.Items.Clear();
            commandsMenu.DropDownItems.AddRange(originalMenus);
            menuStrip.Items.AddRange(new ToolStripItem[] { _filtersMenuItem, commandsMenu });
            menuStrip.Dock = DockStyle.None;
            menuStrip.AutoSize = true;
            menuStrip.Anchor = AnchorStyles.Right;
            menuStrip.Padding = Padding.Empty;
            menuStrip.Margin = Padding.Empty;
            foreach (ToolStripItem item in menuStrip.Items)
                item.Padding = ScalePadding(9, 9);
            _workspaceHeader.Controls.Add(menuStrip, 2, 0);

            Controls.Add(_workspaceHeader);
            Controls.SetChildIndex(_workspaceHeader, headerLayer);
            MainMenuStrip = menuStrip;
            settingsSidebarPanel.Padding = ScalePadding(16, 12);
            settingsSidebarPanel.AutoScroll = true;
            splitContainer1.Panel2.Padding = new Padding(LogicalToDeviceUnits(26), 0, LogicalToDeviceUnits(26), LogicalToDeviceUnits(10));

            var selectionMenu = new ToolStripDropDownButton(resources.GetString("WorkspaceSelection"));
            toolStrip.Items.Clear();
            selectionMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                toolStripButtonSelAll, toolStripButtonSelNone, toolStripButtonSelInv
            });
            foreach (ToolStripItem item in selectionMenu.DropDownItems)
                item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                toolStripButton1, selectionMenu, toolStripButtonUninstall, toolStripButton2, toolStripButtonProperties,
                toolStripButtonTarget, toolStripButtonModify, toolStripButton7, toolStripButton8
            });
            toolStripSeparator4.Dispose();
            toolStripSeparator21.Dispose();
            toolStripSeparator22.Dispose();
            toolStripSeparator23.Dispose();
            toolStripSeparator24.Dispose();
            toolStrip.Padding = ScalePadding(0, 6);
            toolStrip.ImageScalingSize = new Size(LogicalToDeviceUnits(17), LogicalToDeviceUnits(17));
            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.Margin = ScalePadding(3, 0);
                item.Padding = ScalePadding(9, 7);
            }

            toolStripButtonUninstall.Alignment = ToolStripItemAlignment.Right;
            toolStripButtonUninstall.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonUninstall.Padding = ScalePadding(16, 7);
            toolStripButtonUninstall.Overflow = ToolStripItemOverflow.Never;
            toolStripButtonUninstall.Enabled = false;
            toolStripButton2.Alignment = ToolStripItemAlignment.Right;
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton2.Enabled = false;
            toolStripButtonProperties.Alignment = ToolStripItemAlignment.Right;
            toolStripButtonProperties.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonProperties.Enabled = false;
            toolStripButton1.Text = resources.GetString("WorkspaceRefresh");
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            foreach (var button in new[] { toolStripButtonTarget, toolStripButtonModify, toolStripButton7, toolStripButton8 })
            {
                button.Overflow = ToolStripItemOverflow.Always;
                button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }

            listViewPanel.BorderStyle = BorderStyle.None;
            listViewPanel.Padding = ScalePadding(1, 1);
            uninstallerObjectListView.GridLines = false;
            uninstallerObjectListView.RowHeight = LogicalToDeviceUnits(36);
            uninstallerObjectListView.HeaderMinimumHeight = LogicalToDeviceUnits(36);
            _listHeaderStyle = new HeaderFormatStyle();
            components.Add(_listHeaderStyle);
            _listHeaderStyle.SetForeColor(MutedText);
            _listHeaderStyle.Normal.BackColor = HeaderBackground;
            _listHeaderStyle.Hot.BackColor = WorkspaceBorder;
            _listHeaderStyle.Pressed.BackColor = WorkspaceBorder;
            ConfigureDefaultWorkspaceColumns();

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

        private void ConfigureDefaultWorkspaceColumns()
        {
            var defaultColumns = new[] { olvColumnDisplayName, olvColumnPublisher, olvColumnDisplayVersion,
                olvColumnInstallDate, olvColumnSize };
            foreach (var column in uninstallerObjectListView.AllColumns)
                column.IsVisible = defaultColumns.Contains(column);

            olvColumnDisplayName.Width = LogicalToDeviceUnits(320);
            if (string.IsNullOrEmpty(Properties.Settings.Default.UninstallerListViewState))
            {
                olvColumnDisplayName.MinimumWidth = LogicalToDeviceUnits(168);
                olvColumnDisplayName.FillsFreeSpace = true;
            }
            olvColumnPublisher.Width = LogicalToDeviceUnits(210);
            olvColumnPublisher.WordWrap = false;
            olvColumnDisplayVersion.Width = LogicalToDeviceUnits(120);
            olvColumnInstallDate.Width = LogicalToDeviceUnits(125);
            olvColumnSize.Width = LogicalToDeviceUnits(90);
            uninstallerObjectListView.RebuildColumns();
        }

        private void ApplyWorkspaceTheme(bool useSystemStyle)
        {
            bool useNativeColors = useSystemStyle || SystemInformation.HighContrast;
            Klocman.Forms.LoadingDialog.UseSystemTheme = useSystemStyle;
            ApplyTitleBarTheme(useNativeColors);
            _scanProgressView.UseNativeColors = useNativeColors;
            _scanProgressView.Invalidate();
            BackColor = useNativeColors ? SystemColors.Control : WorkspaceBackground;
            settingsSidebarPanel.BackColor = useNativeColors ? SystemColors.Control : Color.White;
            _workspaceHeader.BackColor = BackColor;
            label1.ForeColor = useNativeColors ? SystemColors.ControlText : HeadingText;
            _searchSurface.BackColor = useNativeColors ? SystemColors.Window : WorkspaceBackground;
            _searchSurface.UseNativeColors = useNativeColors;
            _searchSurface.Invalidate();
            var searchField = filterEditor1.SearchField;
            searchField.BackColor = useNativeColors ? SystemColors.Window : Color.White;
            searchField.InactiveSearchColor = useNativeColors ? SystemColors.GrayText : MutedText;
            searchField.SearchBoxBorderStyle = useNativeColors ? BorderStyle.FixedSingle : BorderStyle.None;
            listViewPanel.BackColor = useNativeColors ? SystemColors.ControlDark : WorkspaceBorder;
            uninstallerObjectListView.UseAlternatingBackColors = !useNativeColors;
            uninstallerObjectListView.AlternateRowBackColor = Color.FromArgb(249, 250, 253);
            uninstallerObjectListView.HeaderUsesThemes = useNativeColors;
            uninstallerObjectListView.HeaderFormatStyle = useNativeColors ? null : _listHeaderStyle;
            uninstallerObjectListView.SelectedBackColor = useNativeColors ? Color.Empty : SelectedRow;
            uninstallerObjectListView.SelectedForeColor = useNativeColors ? Color.Empty : HeadingText;
            uninstallerObjectListView.UnfocusedSelectedBackColor = useNativeColors ? Color.Empty : HeaderBackground;
            uninstallerObjectListView.UnfocusedSelectedForeColor = useNativeColors ? Color.Empty : HeadingText;

            if (useNativeColors)
            {
                toolStrip.RenderMode = ToolStripRenderMode.System;
                menuStrip.RenderMode = ToolStripRenderMode.System;
                statusStrip1.RenderMode = ToolStripRenderMode.System;
            }
            else
            {
                var renderer = new WorkspaceStripRenderer(toolStripButtonUninstall, LogicalToDeviceUnits(8));
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

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = System.Math.Min(radius * 2, System.Math.Min(bounds.Width, bounds.Height));
            var shape = new GraphicsPath();
            shape.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            shape.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            shape.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            shape.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            shape.CloseFigure();
            return shape;
        }

        private sealed class SearchFieldPanel : Panel
        {
            public bool UseNativeColors { get; set; }

            public SearchFieldPanel()
            {
                ResizeRedraw = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (UseNativeColors || ClientSize.Width < 2 || ClientSize.Height < 2)
                    return;

                var bounds = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
                using var shape = RoundedRectangle(bounds, LogicalToDeviceUnits(10));
                using var border = new Pen(WorkspaceBorder);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(Brushes.White, shape);
                e.Graphics.DrawPath(border, shape);
            }
        }

        private sealed class WorkspaceStripRenderer : ToolStripProfessionalRenderer
        {
            private readonly ToolStripButton _uninstall;
            private readonly int _cornerRadius;

            public WorkspaceStripRenderer(ToolStripButton uninstall, int cornerRadius) : base(new WorkspaceColorTable())
            {
                _uninstall = uninstall;
                _cornerRadius = cornerRadius;
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
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-1, -1);
                using var shape = RoundedRectangle(bounds, _cornerRadius);
                var previousSmoothing = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(fill, shape);
                e.Graphics.SmoothingMode = previousSmoothing;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.ToolStrip is not MenuStrip)
                {
                    base.OnRenderMenuItemBackground(e);
                    return;
                }

                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-3, -3);
                using var shape = RoundedRectangle(bounds, _cornerRadius);
                using var fill = new SolidBrush(e.Item.Selected || e.Item.Pressed ? HeaderBackground : Color.White);
                using var border = new Pen(WorkspaceBorder);
                var previousSmoothing = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(fill, shape);
                e.Graphics.DrawPath(border, shape);
                e.Graphics.SmoothingMode = previousSmoothing;
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
            public override Color MenuStripGradientBegin => WorkspaceBackground;
            public override Color MenuStripGradientEnd => WorkspaceBackground;
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
