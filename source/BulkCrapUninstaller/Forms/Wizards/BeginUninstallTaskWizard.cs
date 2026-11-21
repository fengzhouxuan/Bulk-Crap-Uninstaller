using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BulkCrapUninstaller.Functions;
using BulkCrapUninstaller.Functions.Tools;
using BulkCrapUninstaller.Properties;
using Klocman;
using Klocman.Extensions;
using Klocman.IO;
using Klocman.Localising;
using UninstallTools;
using UninstallTools.Factory;
using UninstallTools.Uninstaller;
using static Klocman.Forms.VelaColors;

namespace BulkCrapUninstaller.Forms
{
    public partial class BeginUninstallTaskWizard : VelaDialog
    {
        private readonly string _nextText;
        private bool _anyRelatedUninstallers;
        private List<ApplicationUninstallerEntry> _otherUninstallers;
        private int _previousPageNumber;
        private bool _quiet;
        private ICollection<ApplicationUninstallerEntry> _selectedUninstallers;

        public BeginUninstallTaskWizard()
        {
            InitializeComponent();
            ApplyVelaStyle(buttonNext, p1Heading, label1, label2, label5, label6);

            _nextText = buttonNext.Text;
            button2.Hide();
            AcceptButton = buttonNext;
            DialogResult = DialogResult.Cancel;

            tabControl1.TabIndex = 0;
            if (FormBorderStyle == FormBorderStyle.None)
                StyleReview();
        }

        private void StyleReview()
        {
            tableLayoutPanel1.Dock = DockStyle.Top;
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            tableLayoutPanel1.BackColor = WorkspaceBackground;
            int lineHeight = LogicalToDeviceUnits(38);
            int appListHeight = LogicalToDeviceUnits(60);
            tableLayoutPanel1.Height = tableLayoutPanel1.Padding.Vertical + appListHeight +
                (tableLayoutPanel1.RowCount - 1) * lineHeight;
            for (int row = 0; row < tableLayoutPanel1.RowCount; row++)
            {
                var style = tableLayoutPanel1.RowStyles[row];
                style.SizeType = SizeType.Absolute;
                style.Height = row == 0 ? appListHeight : lineHeight;
            }
            foreach (Label label in tableLayoutPanel1.Controls)
            {
                label.Dock = DockStyle.Fill;
                label.Padding = new Padding(LogicalToDeviceUnits(12), 0, LogicalToDeviceUnits(12), 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.ForeColor = tableLayoutPanel1.GetColumn(label) == 0 ? MutedText : HeadingText;
            }
        }

        private int PageNumber
        {
            get { return tabControl1.SelectedIndex; }
            set
            {
                tabControl1.SelectedIndex = value;
                UpdateState();
            }
        }

        public IReadOnlyList<BulkUninstallEntry> Results { get; private set; }

        private void button2_Click(object sender, EventArgs e)
        {
            Results = uninstallConfirmation1.GetResults().ToArray();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (PageNumber == tabControl1.TabCount - 1)
                button2_Click(sender, e);
            else
                PageNumber++;
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            PageNumber = Math.Max(0, PageNumber - 1);
        }

        private List<BulkUninstallEntry> ConvertToTaskEntries(IEnumerable<ApplicationUninstallerEntry> targets)
        {
            var targetList = new List<BulkUninstallEntry>();

            foreach (var target in targets)
            {
                var tempStatus = UninstallStatus.Waiting;
                if (!target.IsValid)
                    tempStatus = UninstallStatus.Invalid;
                else if (!Settings.Default.AdvancedDisableProtection && target.IsProtected)
                    tempStatus = UninstallStatus.Protected;

                var silentPossible = _quiet && target.QuietUninstallPossible;

                targetList.Add(new BulkUninstallEntry(target, silentPossible, tempStatus));
            }

            return targetList;
        }

        private static IEnumerable<ApplicationUninstallerEntry> GetRelatedUninstallers(
            ApplicationUninstallerEntry thisUninstaller, IEnumerable<ApplicationUninstallerEntry> other)
        {
            return other.Where(y => ApplicationEntryTools.AreEntriesRelated(thisUninstaller, y, -3));
        }

        public void Initialize(ICollection<ApplicationUninstallerEntry> selectedUninstallers,
            ICollection<ApplicationUninstallerEntry> allUninstallers, bool quiet)
        {
            _quiet = quiet;
            _selectedUninstallers = selectedUninstallers;

            _otherUninstallers = allUninstallers
                .Except(_selectedUninstallers)
                .Where(x => !x.SystemComponent && !x.IsProtected)
                .ToList();

            var relatedUninstallers = _otherUninstallers.Select(
                x => new { Entry = x, Related = GetRelatedUninstallers(x, _selectedUninstallers).ToList() })
                .Where(x => x.Related.Any()).ToList();

            relatedUninstallerAdder1.SetRelatedApps(relatedUninstallers
                .Select(x => new RelatedUninstallerAdder.RelatedApplicationEntry(x.Entry, x.Related)));

            _anyRelatedUninstallers = relatedUninstallers.Any();
            if (!_anyRelatedUninstallers)
                PageNumber = 1;
        }

        private void processWaiterControl1_AllProcessesClosed(object sender, EventArgs e)
        {
            //if (PageNumber == 2)
            //    PageNumber = 3;
            //todo show greeen text all closed? or skip when clicking next? slow
        }

        private static List<BulkUninstallEntry> SortTaskEntryList(List<BulkUninstallEntry> taskEntries)
        {
            return Settings.Default.AdvancedIntelligentUninstallerSorting
                ? AppUninstaller.SortIntelligently(taskEntries).ToList()
                : taskEntries.OrderBy(x => x.UninstallerEntry.DisplayName).ToList();
        }

        private void UpdateState()
        {
            UseWaitCursor = true;
            Application.DoEvents();

            switch (PageNumber)
            {
                case 0:
                    break;

                case 1:
                    {
                        processWaiterControl1.StopUpdating();

                        var additionals = relatedUninstallerAdder1.GetResults();
                        var taskEntries = ConvertToTaskEntries(_selectedUninstallers.Concat(additionals));
                        taskEntries = SortTaskEntryList(taskEntries);
                        uninstallConfirmation1.SetRelatedApps(taskEntries);
                    }
                    break;

                case 2:
                    {
                        /*if (taskEntries == null || taskEntries.Count == 0) return;*/

                        var selectedTaskEntries = uninstallConfirmation1.GetResults().ToList();

                        if (!selectedTaskEntries.Any())
                        {
                            MessageBoxes.NoUninstallersSelectedInfo();

                            PageNumber = 1;
                            return;
                        }

                        var relatedPids = AppUninstaller.GetRelatedProcessIds(
                            selectedTaskEntries.Select(x => x.UninstallerEntry), !_quiet);

                        if (relatedPids.Length == 0)
                        {
                            PageNumber = _previousPageNumber < 2 ? 3 : 1;
                            return;
                        }

                        processWaiterControl1.Initialize(relatedPids, !_quiet);
                        processWaiterControl1.StartUpdating();
                    }
                    break;

                case 3: // Settings
                    processWaiterControl1.StopUpdating();
                    break;

                case 4: // Final
                    {
                        var taskEntries = uninstallConfirmation1.GetResults().ToList();

                        labelApps.Text = string.Join(", ",
                            taskEntries.Select(x => x.UninstallerEntry.DisplayName).OrderBy(x => x).ToArray());
                        labelTotalSize.Text = FileSize.SumFileSizes(taskEntries.Select(x => x.UninstallerEntry.EstimatedSize)).ToString();

                        labelConcurrentEnabled.Text = Settings.Default.UninstallConcurrency.ToYesNo();
                        labelFilesStillUsed.Text = processWaiterControl1.ProcessesStillRunning.ToYesNo();
                        labelRestorePointCreated.Text = (SysRestore.SysRestoreAvailable() ? Settings.Default.MessagesRestorePoints : YesNoAsk.No).GetLocalisedName();
                        labelWillBeSilent.Text = _quiet.ToYesNo();

                        labelOther.Text = Settings.Default.AdvancedSimulate ? "Simulating" : "-";
                    }
                    break;
            }

            labelProgress.Text = PageNumber + 1 + " / " + tabControl1.TabCount;
            buttonPrev.Enabled = PageNumber > 0 && (PageNumber != 1 || _anyRelatedUninstallers);
            buttonNext.Text = PageNumber == tabControl1.TabCount - 1 ? button2.Text : _nextText;

            UseWaitCursor = false;

            _previousPageNumber = PageNumber;
        }

        private void BeginUninstallTaskWizard_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (DialogResult != DialogResult.OK)
                SystemRestore.CancelSysRestore();
        }
    }
}
