using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static Klocman.Forms.VelaColors;

namespace Klocman.Forms
{
    public sealed class VelaProgressBar : ProgressBar
    {
        private readonly Timer _animation = new() { Interval = 35 };

        public VelaProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _animation.Tick += (_, _) => Invalidate();
        }

        public bool UseSystemColors { get; set; }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            // The native progress messages also handle updates through a ProgressBar reference.
            if (message.Msg is >= 0x0401 and <= 0x040A)
            {
                UpdateAnimation();
                Invalidate();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (_animation != null)
                _animation.Enabled = !Disposing && Visible && IsHandleCreated && Style == ProgressBarStyle.Marquee;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int height = Math.Min(Height, LogicalToDeviceUnits(8));
            if (height < 2 || Width < 2)
                return;

            bool native = UseSystemColors || SystemInformation.HighContrast;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new RectangleF(0, (Height - height) / 2f, Width, height);
            using var shape = RoundedTrack(track);
            using var background = new SolidBrush(native ? SystemColors.ControlDark : WorkspaceBorder);
            using var fill = new SolidBrush(native ? SystemColors.Highlight : PrimaryAction);
            e.Graphics.FillPath(background, shape);
            e.Graphics.SetClip(shape);

            if (Style == ProgressBarStyle.Marquee)
            {
                float length = track.Width * 0.28f;
                float position = (Environment.TickCount64 % 1500) / 1500f * (track.Width + length) - length;
                e.Graphics.FillRectangle(fill, position, track.Y, length, height);
            }
            else if (Maximum > Minimum)
            {
                float width = track.Width * ((float)(Value - Minimum) / (Maximum - Minimum));
                if (width > 0)
                    e.Graphics.FillRectangle(fill, 0, track.Y, width, height);
            }
        }

        private static GraphicsPath RoundedTrack(RectangleF track)
        {
            float diameter = Math.Min(track.Height, track.Width);
            var shape = new GraphicsPath();
            shape.AddArc(track.X, track.Y, diameter, track.Height, 90, 180);
            shape.AddArc(track.Right - diameter, track.Y, diameter, track.Height, 270, 180);
            shape.CloseFigure();
            return shape;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _animation.Dispose();
            base.Dispose(disposing);
        }
    }
}
