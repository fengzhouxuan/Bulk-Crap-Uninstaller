using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    public abstract class VelaWindow : Form
    {
        internal const int NcHitTest = 0x0084;
        internal const int NcLeftButtonDown = 0x00A1;
        internal const int NcLeftButtonDoubleClick = 0x00A3;
        internal const int NcRightButtonUp = 0x00A5;
        internal const int CaptionHit = 2;

        private VelaTitleBar _titleBar;
        private bool _resizable;
        private bool _useSystemColors;

        protected Control VelaTitleBarControl => _titleBar;

        protected string VelaCaption
        {
            set { if (_titleBar != null) _titleBar.Text = value; }
        }

        protected void InitializeVelaFrame(string caption, bool resizable)
        {
            _resizable = resizable;
            _titleBar = new VelaTitleBar(this, caption, ControlBox, MinimizeBox, MaximizeBox);
            FormBorderStyle = FormBorderStyle.None;
            Controls.Add(_titleBar);
            _titleBar.SendToBack();
            UpdateFrameLayout();
            if (IsHandleCreated)
                HideCompositorBorder();
        }

        protected void ApplyVelaFrameTheme(bool useSystemColors)
        {
            _useSystemColors = useSystemColors;
            _titleBar?.SetColors(useSystemColors);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_titleBar != null && _resizable && WindowState == FormWindowState.Normal)
            {
                using var border = new Pen(_useSystemColors ? SystemColors.ControlDark : WorkspaceBorder);
                e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }

        private void UpdateFrameLayout(int? dpi = null)
        {
            if (_titleBar == null)
                return;

            int band = _resizable && WindowState == FormWindowState.Normal
                ? ScaleForDpi(5, dpi ?? DeviceDpi) : 0;
            var padding = new Padding(band);
            if (Padding != padding)
                Padding = padding;
            _titleBar.SetMaximized(WindowState == FormWindowState.Maximized);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateFrameLayout();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            UpdateFrameLayout(e.DeviceDpiNew);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_titleBar != null)
                HideCompositorBorder();
        }

        private void HideCompositorBorder()
        {
            const int borderColorAttribute = 34;
            uint noBorder = 0xFFFFFFFE;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                DwmSetWindowAttribute(Handle, borderColorAttribute, in noBorder, sizeof(uint));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                if (_titleBar == null)
                    return parameters;

                const int thickFrame = 0x00040000;
                const int systemMenu = 0x00080000;
                const int minimizeBox = 0x00020000;
                const int maximizeBox = 0x00010000;
                const int caption = 0x00C00000;
                const int extendedEdges = 0x00020300;
                parameters.Style &= ~caption;
                parameters.ExStyle &= ~extendedEdges;
                if (_resizable) parameters.Style |= thickFrame;
                if (ControlBox) parameters.Style |= systemMenu;
                if (ControlBox && MinimizeBox) parameters.Style |= minimizeBox;
                if (ControlBox && MaximizeBox) parameters.Style |= maximizeBox;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_titleBar != null)
            {
                switch (message.Msg)
                {
                    case 0x0083 when message.LParam != IntPtr.Zero: // WM_NCCALCSIZE
                        if (IsZoomed(message.HWnd))
                        {
                            var bounds = Marshal.PtrToStructure<NativeRectangle>(message.LParam);
                            var workArea = Screen.FromRectangle(Rectangle.FromLTRB(
                                bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)).WorkingArea;
                            Marshal.StructureToPtr(new NativeRectangle
                            {
                                Left = workArea.Left, Top = workArea.Top,
                                Right = workArea.Right, Bottom = workArea.Bottom
                            }, message.LParam, false);
                        }
                        message.Result = IntPtr.Zero;
                        return;
                    case 0x0085: // WM_NCPAINT
                    case 0x00AE: // WM_NCUAHDRAWCAPTION
                    case 0x00AF: // WM_NCUAHDRAWFRAME
                        // Keep the sizing styles without letting Windows paint over the client title bar.
                        message.Result = IntPtr.Zero;
                        return;
                    case 0x0086: // WM_NCACTIVATE
                        message.Result = DefWindowProc(message.HWnd, message.Msg, message.WParam, new IntPtr(-1));
                        return;
                    case NcHitTest:
                        message.Result = (IntPtr)HitTestFrame(ScreenPoint(message.LParam));
                        return;
                    case NcLeftButtonDoubleClick when message.WParam == (IntPtr)CaptionHit && ControlBox && MaximizeBox:
                        ToggleMaximized();
                        message.Result = IntPtr.Zero;
                        return;
                }
            }
            base.WndProc(ref message);
        }

        internal void ToggleMaximized()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private int HitTestFrame(Point screenPoint)
        {
            var point = PointToClient(screenPoint);
            int band = Padding.Left;
            if (band > 0)
            {
                bool left = point.X < band;
                bool right = point.X >= ClientSize.Width - band;
                if (point.Y < band) return left ? 13 : right ? 14 : 12;
                if (point.Y >= ClientSize.Height - band) return left ? 16 : right ? 17 : 15;
                if (left) return 10;
                if (right) return 11;
            }
            return _titleBar.IsOverCaption(screenPoint) ? CaptionHit : 1;
        }

        internal static int ScaleForDpi(int size, int dpi) => (size * dpi + 48) / 96;

        internal static Point ScreenPoint(IntPtr packed)
        {
            long coordinates = packed.ToInt64();
            return new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        internal static extern IntPtr SendWindowMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        private static extern IntPtr DefWindowProc(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr window);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, in uint value, int size);

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
