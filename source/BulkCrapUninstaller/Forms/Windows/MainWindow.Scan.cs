// In-window application scanning progress for Vela (Apache 2.0).

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using BulkCrapUninstaller.Functions.ApplicationList;

using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    internal sealed partial class MainWindow
    {
        private ScanProgressView _scanProgressView;

        private void InitializeScanProgress()
        {
            var strings = new ComponentResourceManager(typeof(MainWindow));
            _scanProgressView = new ScanProgressView(strings.GetString("WorkspaceScanTitle"))
            {
                Name = "scanProgressView",
                Dock = DockStyle.Fill
            };
            listViewPanel.Controls.Add(_scanProgressView);
            _scanProgressView.BringToFront();
            ShowScanProgress();
        }

        internal void ShowScanProgress()
        {
            uninstallerObjectListView.Visible = false;
            _scanProgressView.ResetProgress();
            _scanProgressView.Visible = true;
        }

        internal void UpdateScanProgress(UninstallerListViewUpdater.ScanProgress progress)
        {
            _scanProgressView.SetProgress(progress);
        }

        internal void HideScanProgress()
        {
            _scanProgressView.Visible = false;
            uninstallerObjectListView.Visible = true;
        }

        private sealed class ScanProgressView : Control
        {
            private readonly Font _headingFont;
            private readonly string _heading;
            private UninstallerListViewUpdater.ScanProgress _progress;
            private int _indeterminateTick;

            public ScanProgressView(string heading)
            {
                _heading = heading;
                _headingFont = new Font(Font.FontFamily, 16, FontStyle.Bold);
                AccessibleName = heading;
                AccessibleRole = AccessibleRole.ProgressBar;
                DoubleBuffered = true;
                ResizeRedraw = true;
                TabStop = false;
            }

            public bool UseNativeColors { get; set; }

            public void ResetProgress()
            {
                _progress = null;
                AccessibleDescription = null;
                _indeterminateTick = 0;
                Invalidate();
            }

            public void SetProgress(UninstallerListViewUpdater.ScanProgress progress)
            {
                if (progress != null)
                {
                    if (_progress?.Stage != progress.Stage)
                        AccessibleDescription = progress.Stage;
                    if (_progress?.Fraction is double previous && progress.Fraction < previous)
                        progress = progress with { Fraction = previous };
                    _progress = progress;
                }

                if (_progress?.Fraction == null)
                    _indeterminateTick = (_indeterminateTick + 1) % 48;
                if (progress != null || _progress?.Fraction == null)
                    Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var graphics = e.Graphics;
                var background = UseNativeColors ? SystemColors.Control : WorkspaceBackground;
                var text = UseNativeColors ? SystemColors.WindowText : HeadingText;
                var muted = UseNativeColors ? SystemColors.WindowText : MutedText;
                var border = UseNativeColors ? SystemColors.ControlDark : WorkspaceBorder;
                int width = Math.Min(LogicalToDeviceUnits(448), ClientSize.Width - LogicalToDeviceUnits(24));
                int height = Math.Min(LogicalToDeviceUnits(150), ClientSize.Height - LogicalToDeviceUnits(16));
                graphics.Clear(background);
                if (height < LogicalToDeviceUnits(75) || width < LogicalToDeviceUnits(100))
                    return;

                var card = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
                int inset = LogicalToDeviceUnits(23);
                int contentWidth = card.Width - inset * 2;
                int left = card.Left + inset;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var surface = new SolidBrush(UseNativeColors ? SystemColors.Window : Color.White);
                using var stroke = new Pen(border);
                using var shape = RoundedRectangle(card, LogicalToDeviceUnits(13));
                graphics.FillPath(surface, shape);
                graphics.DrawPath(stroke, shape);

                var title = new Rectangle(left, card.Top + LogicalToDeviceUnits(18), contentWidth, LogicalToDeviceUnits(31));
                var fraction = _progress?.Fraction;
                if (fraction.HasValue)
                {
                    var percentage = fraction.Value.ToString("P0", CultureInfo.CurrentCulture);
                    int percentageWidth = TextRenderer.MeasureText(percentage, Font).Width;
                    var percentageBounds = new Rectangle(title.Right - percentageWidth, title.Y, percentageWidth, title.Height);
                    DrawLine(graphics, percentage, Font, percentageBounds, muted, TextFormatFlags.Right);
                    title.Width = Math.Max(0, title.Width - percentageWidth - LogicalToDeviceUnits(8));
                }
                DrawLine(graphics, _heading, _headingFont, title, text);

                var track = new Rectangle(left, card.Top + LogicalToDeviceUnits(61), contentWidth, LogicalToDeviceUnits(6));
                using var trackBrush = new SolidBrush(UseNativeColors ? SystemColors.Control : HeaderBackground);
                using var trackShape = RoundedRectangle(track, track.Height / 2);
                graphics.FillPath(trackBrush, trackShape);
                int fillWidth = fraction.HasValue ? (int)Math.Round(track.Width * fraction.Value) : track.Width / 4;
                if (fillWidth > 0)
                {
                    int travel = Math.Min(_indeterminateTick, 48 - _indeterminateTick);
                    int offset = fraction.HasValue ? 0 : travel * (track.Width - fillWidth) / 24;
                    var fill = new Rectangle(track.Left + offset, track.Top, fillWidth, track.Height);
                    using var brush = new SolidBrush(UseNativeColors ? SystemColors.Highlight : PrimaryAction);
                    using var fillShape = RoundedRectangle(fill, track.Height / 2);
                    graphics.FillPath(brush, fillShape);
                }

                if (height >= LogicalToDeviceUnits(114))
                {
                    var stage = new Rectangle(left, card.Top + LogicalToDeviceUnits(81), contentWidth, LogicalToDeviceUnits(23));
                    DrawLine(graphics, _progress?.Stage, Font, stage, text);
                }
                if (height >= LogicalToDeviceUnits(139))
                {
                    var item = new Rectangle(left, card.Top + LogicalToDeviceUnits(108), contentWidth, LogicalToDeviceUnits(21));
                    DrawLine(graphics, _progress?.Item, Font, item, muted);
                }
            }

            private static void DrawLine(Graphics graphics, string text, Font font, Rectangle bounds, Color color,
                TextFormatFlags alignment = TextFormatFlags.Left)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    TextRenderer.DrawText(graphics, text, font, bounds, color, alignment | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _headingFont.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
