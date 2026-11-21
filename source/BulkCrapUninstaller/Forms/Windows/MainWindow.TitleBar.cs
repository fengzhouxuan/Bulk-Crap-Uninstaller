using System;

namespace BulkCrapUninstaller.Forms
{
    internal sealed partial class MainWindow
    {
        private void InitializeTitleBar() => InitializeVelaFrame(Program.ProductName, resizable: true);

        private void ApplyTitleBarTheme(bool useSystemColors) => ApplyVelaFrameTheme(useSystemColors);

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            VelaCaption = MainTitleBarText != null && Text.StartsWith(MainTitleBarText, StringComparison.Ordinal)
                ? Program.ProductName + Text[MainTitleBarText.Length..]
                : Program.ProductName;
        }
    }
}
