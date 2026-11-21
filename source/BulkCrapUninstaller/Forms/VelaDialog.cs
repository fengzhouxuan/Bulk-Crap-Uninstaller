using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BrightIdeasSoftware;
using BulkCrapUninstaller.Properties;
using Klocman.Forms;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    public class VelaDialog : Form
    {
        private Panel _caption;
        private FlowLayoutPanel _captionButtons;
        private Label _captionText;
        private CaptionButton _maximize;
        private bool _resizable;

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
            bool canMinimize = MinimizeBox;
            bool canMaximize = MaximizeBox;
            bool hasControlBox = ControlBox;
            bool autoSize = AutoSize;
            var bodyPadding = Padding;
            _resizable = FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow;

            SuspendLayout();
            FormBorderStyle = FormBorderStyle.None;
            Padding = Padding.Empty;
            BackColor = WorkspaceBackground;
            ForeColor = HeadingText;
            var inset = LogicalToDeviceUnits(7);
            int captionHeight = LogicalToDeviceUnits(42);
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

            _caption = new Panel
            {
                Dock = DockStyle.Top, Height = captionHeight,
                Padding = new Padding(LogicalToDeviceUnits(14), 0, LogicalToDeviceUnits(3), 0)
            };
            var picture = new PictureBox
            {
                Image = Resources._bcu_logo, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Left,
                Width = LogicalToDeviceUnits(28), Padding = new Padding(0, LogicalToDeviceUnits(9), LogicalToDeviceUnits(6), LogicalToDeviceUnits(9))
            };
            var titleFont = new Font(Font.FontFamily, 10, FontStyle.Bold);
            _captionText = new Label
            {
                Text = Text, Dock = DockStyle.Fill, Font = titleFont, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false
            };
            _captionText.Disposed += (_, _) => titleFont.Dispose();
            _captionButtons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false };
            if (hasControlBox)
            {
                if (canMinimize)
                    AddCaptionButton(CaptionCommand.Minimize, "CaptionMinimize", () => WindowState = FormWindowState.Minimized);
                if (canMaximize)
                    _maximize = AddCaptionButton(CaptionCommand.Maximize, "CaptionMaximize", ToggleMaximized);
                AddCaptionButton(CaptionCommand.Close, "CaptionClose", Close);
            }
            _caption.Controls.AddRange(new Control[] { _captionText, _captionButtons, picture });
            Controls.AddRange(new Control[] { surface, _caption });

            StyleControls(body);
            SetPrimaryAction(primary, true);
            foreach (var heading in headings)
            {
                var font = new Font(heading.Font.FontFamily, Math.Max(heading.Font.SizeInPoints, 12), FontStyle.Bold);
                heading.Font = font;
                heading.ForeColor = HeadingText;
                heading.Disposed += (_, _) => font.Dispose();
            }
            var addedSpace = new Size(inset * 2, inset + captionHeight);
            ClientSize = client + addedSpace;
            if (!minimum.IsEmpty) MinimumSize = minimum + addedSpace;
            else if (_resizable) MinimumSize = new Size(Width * 4 / 5, Height * 2 / 3);
            if (!maximum.IsEmpty) MaximumSize = maximum + addedSpace;
            body.ResumeLayout();
            ResumeLayout(true);
        }

        private CaptionButton AddCaptionButton(CaptionCommand command, string text, Action action)
        {
            var button = new CaptionButton(command, CaptionText(text))
            {
                Size = new Size(LogicalToDeviceUnits(44), _caption.Height),
                BackColor = WorkspaceBackground, ForeColor = HeadingText, TabStop = false
            };
            button.FlatAppearance.MouseOverBackColor = command == CaptionCommand.Close
                ? Color.FromArgb(252, 226, 232) : HeaderBackground;
            button.FlatAppearance.MouseDownBackColor = WorkspaceBorder;
            button.Click += (_, _) => action();
            _captionButtons.Controls.Add(button);
            return button;
        }

        private static string CaptionText(string key) => new ComponentResourceManager(typeof(MainWindow)).GetString(key);

        protected void SetPrimaryAction(Button button, bool primary)
        {
            if (_caption != null && button != null)
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

        private void ToggleMaximized() => WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal : FormWindowState.Maximized;

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_captionText != null) _captionText.Text = Text;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            bool maximized = WindowState == FormWindowState.Maximized;
            if (_maximize == null || _maximize.ShowRestore == maximized)
                return;
            _maximize.ShowRestore = maximized;
            _maximize.AccessibleName = CaptionText(maximized ? "CaptionRestore" : "CaptionMaximize");
            _maximize.Invalidate();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                if (_caption == null)
                    return parameters;

                const int thickFrame = 0x00040000;
                const int systemMenu = 0x00080000;
                const int minimizeBox = 0x00020000;
                const int maximizeBox = 0x00010000;
                parameters.Style |= systemMenu;
                if (_resizable) parameters.Style |= thickFrame;
                if (MinimizeBox) parameters.Style |= minimizeBox;
                if (MaximizeBox) parameters.Style |= maximizeBox;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_caption != null)
            {
                switch (message.Msg)
                {
                    case 0x0083 when message.LParam != IntPtr.Zero: // WM_NCCALCSIZE
                        if (IsZoomed(message.HWnd))
                        {
                            var bounds = Marshal.PtrToStructure<NativeRectangle>(message.LParam);
                            var work = Screen.FromRectangle(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)).WorkingArea;
                            Marshal.StructureToPtr(new NativeRectangle
                            {
                                Left = work.Left, Top = work.Top, Right = work.Right, Bottom = work.Bottom
                            }, message.LParam, false);
                        }
                        message.Result = IntPtr.Zero;
                        return;
                    case 0x0084: // WM_NCHITTEST
                        long location = message.LParam.ToInt64();
                        var point = PointToClient(new Point(unchecked((short)location), unchecked((short)(location >> 16))));
                        message.Result = (IntPtr)HitTest(point);
                        return;
                    case 0x00A3 when message.WParam == (IntPtr)2 && _maximize != null: // WM_NCLBUTTONDBLCLK
                        ToggleMaximized();
                        message.Result = IntPtr.Zero;
                        return;
                }
            }
            base.WndProc(ref message);
        }

        private int HitTest(Point point)
        {
            if (_resizable && WindowState == FormWindowState.Normal)
            {
                int edge = LogicalToDeviceUnits(6);
                bool left = point.X < edge;
                bool right = point.X >= ClientSize.Width - edge;
                if (point.Y < edge) return left ? 13 : right ? 14 : 12;
                if (point.Y >= ClientSize.Height - edge) return left ? 16 : right ? 17 : 15;
                if (left) return 10;
                if (right) return 11;
            }
            var captionPoint = new Point(point.X - _caption.Left, point.Y - _caption.Top);
            return _caption.Bounds.Contains(point) && !_captionButtons.Bounds.Contains(captionPoint) ? 2 : 1;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr window);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
