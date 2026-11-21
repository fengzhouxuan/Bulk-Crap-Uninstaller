// Custom title bar for the Vela fork (Apache 2.0).

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BulkCrapUninstaller.Properties;
using Klocman.Forms.Tools;

namespace BulkCrapUninstaller.Forms
{
    internal sealed partial class MainWindow
    {
        private const int ThickFrame = 0x00040000;
        private const int SystemMenu = 0x00080000;
        private const int MinimizeBoxStyle = 0x00020000;
        private const int MaximizeBoxStyle = 0x00010000;
        private const int NonClientCalculateSize = 0x0083;
        private const int NonClientHitTest = 0x0084;
        private const int NonClientDoubleClick = 0x00A3;
        private const int ClientHit = 1;
        private const int CaptionHit = 2;
        private const int LeftHit = 10;
        private const int RightHit = 11;
        private const int TopHit = 12;
        private const int TopLeftHit = 13;
        private const int TopRightHit = 14;
        private const int BottomHit = 15;
        private const int BottomLeftHit = 16;
        private const int BottomRightHit = 17;

        private Panel _workspaceTitleBar;
        private FlowLayoutPanel _captionButtons;
        private PictureBox _captionIcon;
        private Label _captionTitle;
        private CaptionButton _maximizeCaptionButton;
        private string _maximizeCaptionText;
        private string _restoreCaptionText;

        private void InitializeTitleBar()
        {
            var strings = new ComponentResourceManager(typeof(MainWindow));
            _maximizeCaptionText = strings.GetString("CaptionMaximize");
            _restoreCaptionText = strings.GetString("CaptionRestore");

            _workspaceTitleBar = new Panel { Dock = DockStyle.Top };
            _captionIcon = new PictureBox
            {
                Dock = DockStyle.Left,
                Image = Resources._bcu_logo,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            var titleFont = new Font(Font.FontFamily, 11, FontStyle.Bold);
            _captionTitle = new Label
            {
                Dock = DockStyle.Fill,
                Font = titleFont,
                Text = Program.ProductName,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _captionTitle.Disposed += (_, _) => titleFont.Dispose();

            var minimize = new CaptionButton(CaptionCommand.Minimize, strings.GetString("CaptionMinimize"));
            _maximizeCaptionButton = new CaptionButton(CaptionCommand.Maximize, _maximizeCaptionText);
            var close = new CaptionButton(CaptionCommand.Close, strings.GetString("CaptionClose"));
            minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
            _maximizeCaptionButton.Click += (_, _) => ToggleWindowMaximized();
            close.Click += (_, _) => Close();

            _captionButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            _captionButtons.Controls.AddRange(new Control[] { minimize, _maximizeCaptionButton, close });
            _workspaceTitleBar.Controls.AddRange(new Control[] { _captionTitle, _captionButtons, _captionIcon });
            Controls.Add(_workspaceTitleBar);
            Controls.SetChildIndex(_workspaceTitleBar, Controls.Count - 1);
            SizeTitleBar();

            FormBorderStyle = FormBorderStyle.None;
        }

        private void SizeTitleBar()
        {
            var height = LogicalToDeviceUnits(42);
            var buttonWidth = LogicalToDeviceUnits(46);
            _workspaceTitleBar.Height = height;
            _workspaceTitleBar.Padding = new Padding(LogicalToDeviceUnits(18), 0, LogicalToDeviceUnits(4), 0);
            _captionIcon.Width = LogicalToDeviceUnits(34);
            _captionIcon.Padding = ScalePadding(6, 10);
            _captionButtons.Width = buttonWidth * _captionButtons.Controls.Count;
            foreach (Control button in _captionButtons.Controls)
                button.Size = new Size(buttonWidth, height);
        }

        private void ApplyTitleBarTheme(bool useNativeColors)
        {
            _workspaceTitleBar.BackColor = useNativeColors ? SystemColors.Control : WorkspaceBackground;
            _captionTitle.ForeColor = useNativeColors ? SystemColors.ControlText : HeadingText;
            foreach (CaptionButton button in _captionButtons.Controls)
            {
                button.BackColor = _workspaceTitleBar.BackColor;
                button.ForeColor = _captionTitle.ForeColor;
                button.FlatAppearance.MouseOverBackColor = useNativeColors ? SystemColors.ControlLight
                    : button.Action == CaptionCommand.Close ? Color.FromArgb(252, 226, 232) : HeaderBackground;
                button.FlatAppearance.MouseDownBackColor = useNativeColors ? SystemColors.ControlDark : WorkspaceBorder;
            }
        }

        private void ToggleWindowMaximized()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_maximizeCaptionButton == null)
                return;

            _maximizeCaptionButton.ShowRestore = WindowState == FormWindowState.Maximized;
            _maximizeCaptionButton.AccessibleName = _maximizeCaptionButton.ShowRestore ? _restoreCaptionText : _maximizeCaptionText;
            _maximizeCaptionButton.Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_captionTitle != null)
            {
                _captionTitle.Text = MainTitleBarText != null && Text.StartsWith(MainTitleBarText, StringComparison.Ordinal)
                    ? Program.ProductName + Text[MainTitleBarText.Length..]
                    : Program.ProductName;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                // Keep the native window styles used for sizing, system commands and keyboard snapping.
                parameters.Style |= ThickFrame | SystemMenu | MinimizeBoxStyle | MaximizeBoxStyle;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_workspaceTitleBar != null)
            {
                switch (message.Msg)
                {
                    case NonClientCalculateSize when message.LParam != IntPtr.Zero:
                        // The first rectangle is the proposed window rectangle in both message formats.
                        if (IsZoomed(message.HWnd))
                        {
                            var proposed = Marshal.PtrToStructure<NativeRectangle>(message.LParam);
                            var workArea = Screen.FromRectangle(Rectangle.FromLTRB(
                                proposed.Left, proposed.Top, proposed.Right, proposed.Bottom)).WorkingArea;
                            Marshal.StructureToPtr(new NativeRectangle
                            {
                                Left = workArea.Left, Top = workArea.Top, Right = workArea.Right, Bottom = workArea.Bottom
                            }, message.LParam, false);
                        }
                        message.Result = IntPtr.Zero;
                        return;
                    case NonClientHitTest:
                        long location = message.LParam.ToInt64();
                        var screenPoint = new Point(unchecked((short)location), unchecked((short)(location >> 16)));
                        message.Result = (IntPtr)HitTestWindowFrame(screenPoint);
                        return;
                    case NonClientDoubleClick when message.WParam == (IntPtr)CaptionHit:
                        ToggleWindowMaximized();
                        message.Result = IntPtr.Zero;
                        return;
                }
            }

            base.WndProc(ref message);
        }

        private int HitTestWindowFrame(Point screenPoint)
        {
            var point = PointToClient(screenPoint);
            if (WindowState == FormWindowState.Normal)
            {
                int edge = LogicalToDeviceUnits(6);
                bool left = point.X < edge;
                bool right = point.X >= ClientSize.Width - edge;
                if (point.Y < edge) return left ? TopLeftHit : right ? TopRightHit : TopHit;
                if (point.Y >= ClientSize.Height - edge) return left ? BottomLeftHit : right ? BottomRightHit : BottomHit;
                if (left) return LeftHit;
                if (right) return RightHit;
            }

            return _workspaceTitleBar.Bounds.Contains(point) &&
                !_captionButtons.Bounds.Contains(_workspaceTitleBar.PointToClient(screenPoint))
                ? CaptionHit : ClientHit;
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

        private enum CaptionCommand { Minimize, Maximize, Close }

        [WindowStyleController.ControlStyle(false)]
        private sealed class CaptionButton : Button
        {
            public CaptionCommand Action { get; }
            public bool ShowRestore { get; set; }

            public CaptionButton(CaptionCommand command, string accessibleName)
            {
                Action = command;
                AccessibleName = accessibleName;
                Margin = Padding.Empty;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                int size = LogicalToDeviceUnits(10);
                int x = (ClientSize.Width - size) / 2;
                int y = (ClientSize.Height - size) / 2;
                int offset = LogicalToDeviceUnits(3);
                using var pen = new Pen(ForeColor, Math.Max(1f, DeviceDpi / 96f));
                switch (Action)
                {
                    case CaptionCommand.Minimize:
                        e.Graphics.DrawLine(pen, x, y + size / 2, x + size, y + size / 2);
                        break;
                    case CaptionCommand.Maximize when ShowRestore:
                        e.Graphics.DrawLines(pen, new[] { new Point(x + offset, y + offset), new Point(x + offset, y),
                            new Point(x + size, y), new Point(x + size, y + size - offset), new Point(x + size - offset, y + size - offset) });
                        e.Graphics.DrawRectangle(pen, x, y + offset, size - offset, size - offset);
                        break;
                    case CaptionCommand.Maximize:
                        e.Graphics.DrawRectangle(pen, x, y, size, size);
                        break;
                    case CaptionCommand.Close:
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.DrawLine(pen, x, y, x + size, y + size);
                        e.Graphics.DrawLine(pen, x, y + size, x + size, y);
                        break;
                }
            }
        }
    }
}
