using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using BulkCrapUninstaller.Properties;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    public class VelaDialog : VelaWindow
    {
        protected void ApplyVelaStyle(Button primary = null, params Label[] headings)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            Icon = Resources.Icon_Logo;
            if (SystemInformation.HighContrast || Settings.Default.WindowUseSystemTheme)
                return;

            var client = ClientSize;
            var minimum = MinimumSize;
            var maximum = MaximumSize;
            bool autoSize = AutoSize;
            var bodyPadding = Padding;
            bool resizable = FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow;

            SuspendLayout();
            Padding = Padding.Empty;
            BackColor = WorkspaceBackground;
            ForeColor = HeadingText;
            var inset = LogicalToDeviceUnits(7);
            var body = new Panel
            {
                Dock = DockStyle.Fill, Size = client, BackColor = WorkspaceBackground,
                Padding = bodyPadding, AutoSize = autoSize, AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            var surface = new Panel
            {
                Dock = DockStyle.Fill, AutoSize = autoSize, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(inset, 0, inset, inset)
            };
            body.SuspendLayout();
            body.Controls.AddRange(Controls.Cast<Control>().ToArray());
            surface.Controls.Add(body);

            Controls.Add(surface);
            InitializeVelaFrame(Text, resizable);

            StyleControls(body);
            SetPrimaryAction(primary, true);
            foreach (var heading in headings)
            {
                var font = new Font(heading.Font.FontFamily, Math.Max(heading.Font.SizeInPoints, 12), FontStyle.Bold);
                heading.Font = font;
                heading.ForeColor = HeadingText;
                heading.Disposed += (_, _) => font.Dispose();
            }
            var addedSpace = new Size(inset * 2 + Padding.Horizontal, inset + VelaTitleBarControl.Height + Padding.Vertical);
            ClientSize = client + addedSpace;
            if (!minimum.IsEmpty) MinimumSize = minimum + addedSpace;
            else if (resizable) MinimumSize = new Size(Width * 4 / 5, Height * 2 / 3);
            if (!maximum.IsEmpty) MaximumSize = maximum + addedSpace;
            body.ResumeLayout();
            ResumeLayout(true);
        }

        protected void SetPrimaryAction(Button button, bool primary)
        {
            if (VelaTitleBarControl != null && button != null)
                StyleButton(button, primary);
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? PrimaryAction : Color.White;
            button.ForeColor = primary ? Color.White : HeadingText;
            button.FlatAppearance.BorderColor = primary ? PrimaryAction : WorkspaceBorder;
            button.FlatAppearance.MouseOverBackColor = primary ? PrimaryActionHover : HeaderBackground;
            button.FlatAppearance.MouseDownBackColor = primary ? PrimaryActionHover : SelectedRow;
        }

        private static void StyleControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case Button button:
                        StyleButton(button, false);
                        break;
                    case ObjectListView list:
                        list.BorderStyle = BorderStyle.FixedSingle;
                        list.GridLines = false;
                        list.UseAlternatingBackColors = true;
                        list.AlternateRowBackColor = Color.FromArgb(249, 250, 253);
                        list.SelectedBackColor = SelectedRow;
                        list.SelectedForeColor = HeadingText;
                        list.UnfocusedSelectedBackColor = HeaderBackground;
                        list.UnfocusedSelectedForeColor = HeadingText;
                        list.HeaderUsesThemes = false;
                        var header = new HeaderFormatStyle();
                        header.SetBackColor(HeaderBackground);
                        header.SetForeColor(HeadingText);
                        list.HeaderFormatStyle = header;
                        break;
                    case DataGridView grid:
                        grid.BorderStyle = BorderStyle.FixedSingle;
                        grid.BackgroundColor = Color.White;
                        grid.GridColor = WorkspaceBorder;
                        grid.EnableHeadersVisualStyles = false;
                        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
                        grid.ColumnHeadersDefaultCellStyle.ForeColor = HeadingText;
                        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 253);
                        grid.DefaultCellStyle.SelectionBackColor = SelectedRow;
                        grid.DefaultCellStyle.SelectionForeColor = HeadingText;
                        break;
                    case ToolStrip strip:
                        strip.BackColor = WorkspaceBackground;
                        strip.Renderer = new ToolStripProfessionalRenderer(new DialogColorTable());
                        break;
                    case TabPage tab:
                        tab.UseVisualStyleBackColor = false;
                        tab.BackColor = Color.White;
                        break;
                    case Panel panel when panel.BackColor == SystemColors.Control:
                        panel.BackColor = WorkspaceBackground;
                        break;
                }
                if (control.HasChildren)
                    StyleControls(control);
            }
        }

        private sealed class DialogColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => WorkspaceBackground;
            public override Color ToolStripGradientMiddle => WorkspaceBackground;
            public override Color ToolStripGradientEnd => WorkspaceBackground;
            public override Color ButtonSelectedHighlight => HeaderBackground;
            public override Color ButtonSelectedGradientBegin => HeaderBackground;
            public override Color ButtonSelectedGradientEnd => SelectedRow;
            public override Color ButtonSelectedBorder => WorkspaceBorder;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            VelaCaption = Text;
        }
    }
}
