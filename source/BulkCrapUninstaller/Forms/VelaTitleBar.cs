using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BulkCrapUninstaller.Properties;
using static BulkCrapUninstaller.Forms.VelaWindow;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    internal sealed class VelaTitleBar : Panel
    {
        private readonly VelaWindow _window;
        private readonly FlowLayoutPanel _buttons;
        private readonly CaptionButton _maximize;
        private readonly Font _titleFont;
        private readonly string _maximizeText;
        private readonly string _restoreText;

        public VelaTitleBar(VelaWindow window, string caption, bool canClose, bool canMinimize, bool canMaximize)
        {
            _window = window;
            Name = "velaTitleBar";
            Text = caption;
            Dock = DockStyle.Top;
            TabStop = false;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            _titleFont = new Font(window.Font.FontFamily, 11, FontStyle.Bold);

            var strings = new ComponentResourceManager(typeof(MainWindow));
            _maximizeText = strings.GetString("CaptionMaximize");
            _restoreText = strings.GetString("CaptionRestore");
            _buttons = new FlowLayoutPanel { Dock = DockStyle.Right, WrapContents = false };
            if (canClose)
            {
                if (canMinimize)
                    AddButton(CaptionCommand.Minimize, strings.GetString("CaptionMinimize"), () => window.WindowState = FormWindowState.Minimized);
                if (canMaximize)
                    _maximize = AddButton(CaptionCommand.Maximize, _maximizeText, window.ToggleMaximized);
                AddButton(CaptionCommand.Close, strings.GetString("CaptionClose"), window.Close);
            }

            Controls.Add(_buttons);
            SetColors(false);
            SetMetrics(window.DeviceDpi);
        }

        private CaptionButton AddButton(CaptionCommand action, string label, Action click)
        {
            var button = new CaptionButton(action, label);
            button.Click += (_, _) => click();
            _buttons.Controls.Add(button);
            return button;
        }

        public void SetMaximized(bool maximized)
        {
            if (_maximize == null || _maximize.ShowRestore == maximized)
                return;
            _maximize.ShowRestore = maximized;
            _maximize.AccessibleName = maximized ? _restoreText : _maximizeText;
            _maximize.Invalidate();
        }

        public void SetColors(bool useSystemColors)
        {
            BackColor = useSystemColors ? SystemColors.Control : WorkspaceBackground;
            ForeColor = useSystemColors ? SystemColors.ControlText : HeadingText;
            foreach (CaptionButton button in _buttons.Controls)
                button.SetColors(BackColor, ForeColor, useSystemColors);
        }

        private void SetMetrics(int dpi)
        {
            Height = ScaleForDpi(38, dpi);
            int buttonWidth = ScaleForDpi(44, dpi);
            Padding = new Padding(0, 0, ScaleForDpi(3, dpi), 0);
            _buttons.Width = buttonWidth * _buttons.Controls.Count;
            foreach (Control button in _buttons.Controls)
                button.Size = new Size(buttonWidth, Height);
        }

        internal bool IsOverCaption(Point screenPoint)
        {
            var local = PointToClient(screenPoint);
            return ClientRectangle.Contains(local) && !_buttons.Bounds.Contains(local);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            SetMetrics(DeviceDpi);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int left = LogicalToDeviceUnits(13);
            int icon = LogicalToDeviceUnits(25);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Resources._bcu_logo, new Rectangle(left, (Height - icon) / 2, icon, icon));
            int textLeft = left + icon + LogicalToDeviceUnits(8);
            int textWidth = _buttons.Left - textLeft - LogicalToDeviceUnits(8);
            if (textWidth > 0)
            {
                TextRenderer.DrawText(e.Graphics, Text, _titleFont, new Rectangle(textLeft, 0, textWidth, Height),
                    ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ClassStyle |= 0x0008; // CS_DBLCLKS
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (_window != null && _buttons != null)
            {
                switch (message.Msg)
                {
                    case NcHitTest when IsOverCaption(ScreenPoint(message.LParam)):
                        message.Result = (IntPtr)CaptionHit;
                        return;
                    case NcLeftButtonDown:
                    case NcLeftButtonDoubleClick:
                    case NcRightButtonUp:
                        message.Result = SendWindowMessage(_window.Handle, message.Msg, message.WParam, message.LParam);
                        return;
                }
            }
            base.WndProc(ref message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _titleFont?.Dispose();
            base.Dispose(disposing);
        }
    }
}
