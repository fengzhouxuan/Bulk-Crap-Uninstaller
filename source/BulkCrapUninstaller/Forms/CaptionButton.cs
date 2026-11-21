using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Klocman.Forms.Tools;

namespace BulkCrapUninstaller.Forms
{
    internal enum CaptionCommand { Minimize, Maximize, Close }

    [WindowStyleController.ControlStyle(false)]
    internal sealed class CaptionButton : Button
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
