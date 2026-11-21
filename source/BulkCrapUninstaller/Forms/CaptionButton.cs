using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Klocman.Forms.Tools;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    internal enum CaptionCommand { Minimize, Maximize, Close }

    [WindowStyleController.ControlStyle(false)]
    internal sealed class CaptionButton : Button
    {
        private bool _hovered;
        private bool _pressed;
        private bool _useSystemColors;

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
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void SetColors(Color background, Color foreground, bool useSystemColors)
        {
            BackColor = background;
            ForeColor = foreground;
            _useSystemColors = useSystemColors;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            base.OnMouseUp(e);
            Invalidate();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture && _pressed)
            {
                _pressed = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (_hovered || _pressed)
                PaintHover(e.Graphics);

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
        private void PaintHover(Graphics graphics)
        {
            int inset = LogicalToDeviceUnits(3);
            var bounds = Rectangle.Inflate(ClientRectangle, -inset, -inset);
            int diameter = Math.Min(LogicalToDeviceUnits(12), Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
                return;

            var color = _useSystemColors
                ? (_pressed ? SystemColors.ControlDark : SystemColors.ControlLight)
                : Action == CaptionCommand.Close
                    ? (_pressed ? Color.FromArgb(248, 204, 214) : Color.FromArgb(252, 226, 232))
                    : (_pressed ? WorkspaceBorder : SelectedRow);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var shape = new GraphicsPath();
            shape.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            shape.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            shape.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            shape.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            shape.CloseFigure();
            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, shape);
            graphics.SmoothingMode = SmoothingMode.Default;
        }
    }
}
