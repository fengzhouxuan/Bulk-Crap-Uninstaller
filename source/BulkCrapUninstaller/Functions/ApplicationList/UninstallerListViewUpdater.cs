/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
    Modified to report scans in the Vela main window.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrightIdeasSoftware;
using BulkCrapUninstaller.Forms;
using BulkCrapUninstaller.Properties;
using Klocman.Binding.Settings;
using Klocman.Extensions;
using Klocman.Forms.Tools;
using Klocman.IO;
using Klocman.Tools;
using UninstallTools;
using UninstallTools.Controls;
using UninstallTools.Factory;
using UninstallTools.Startup;

namespace BulkCrapUninstaller.Functions.ApplicationList
{
    internal class UninstallerListViewUpdater : IDisposable
    {
        private UninstallerIconGetter _iconGetter;
        private readonly TypedObjectListView<ApplicationUninstallerEntry> _listView;
        private readonly MainWindow _reference;
        readonly SettingBinder<Settings> _settings = Settings.Default.SettingBinder;
        private IEnumerable<ApplicationUninstallerEntry> _allUninstallers;
        private bool _firstRefresh = true;
        private bool _listRefreshIsRunning;
        private CancellationTokenSource _refreshCancellation;

        internal UninstallerListViewUpdater(MainWindow reference)
        {
            _reference = reference;
            _listView = new TypedObjectListView<ApplicationUninstallerEntry>(reference.uninstallerObjectListView);

            _iconGetter = new UninstallerIconGetter();
            _reference.olvColumnDisplayName.ImageGetter = entry => _iconGetter.ColumnImageGetter(entry);

            // Refresh items marked as invalid after corresponding setting change
            _settings.Subscribe((x, y) =>
            {
                if (CheckIsAppDisposed()) return;

                if (!_firstRefresh)
                    _listView.ListView.RefreshObjects(AllUninstallers.Where(u => !u.IsValid).ToList());
            }, x => x.AdvancedTestInvalid, this);

            // Refresh items marked as orphans after corresponding setting change
            _settings.Subscribe((x, y) =>
            {
                if (CheckIsAppDisposed()) return;

                if (!_firstRefresh)
                    _listView.ListView.UpdateColumnFiltering();
            }, x => x.AdvancedDisplayOrphans, this);

        }

        public IEnumerable<ApplicationUninstallerEntry> AllUninstallers
        {
            get { return _allUninstallers ?? Enumerable.Empty<ApplicationUninstallerEntry>(); }
            private set { _allUninstallers = value; }
        }

        public IEnumerable<ApplicationUninstallerEntry> FilteredUninstallers
            =>
                CheckIsAppDisposed()
                    ? AllUninstallers
                    : _listView.ListView.FilteredObjects.Cast<ApplicationUninstallerEntry>();

        public bool FirstRefreshCompleted => !_firstRefresh;

        public bool ListRefreshIsRunning
        {
            get { return _listRefreshIsRunning; }
            private set
            {
                if (value != _listRefreshIsRunning)
                {
                    _listRefreshIsRunning = value;
                    ListRefreshIsRunningChanged?.Invoke(this, new ListRefreshEventArgs(value, !FirstRefreshCompleted));
                }
            }
        }

        /// <summary>
        /// Faster than SelectedUninstallers.Count()
        /// </summary>
        public int SelectedUninstallerCount => _listView.ListView.CheckBoxes
            ? _listView.CheckedObjects.Count
            : _listView.SelectedObjects.Count;

        public IEnumerable<ApplicationUninstallerEntry> SelectedUninstallers => _listView.ListView.CheckBoxes
            ? _listView.ListView.GetAllObjectsWithMappedCheckState(CheckState.Checked).Cast<ApplicationUninstallerEntry>().Where(e => e != null && AllUninstallers.Contains(e))
            : _listView.SelectedObjects.Where(e => e != null);

        public void Dispose()
        {
            _refreshCancellation?.Cancel();
            _iconGetter?.Dispose();
        }

        public event EventHandler<ListRefreshEventArgs> ListRefreshIsRunningChanged;

        private void ChangeSelection(IEnumerable<ApplicationUninstallerEntry> newSelection)
        {
            _listView.ListView.BeginUpdate();

            var items = newSelection.ToList();
            if (_listView.ListView.CheckBoxes)
                _listView.ListView.CheckedObjects = items;
            _listView.ListView.SelectedObjects = items;

            _listView.ListView.EndUpdate();
            _listView.ListView.Refresh();
            _listView.ListView.Focus();
        }

        public bool CheckIsAppDisposed()
        {
            return _listView.ListView.IsDisposed || _listView.ListView.Disposing
                   || _reference.IsDisposed || _reference.Disposing;
        }

        public void DeselectAllItems(object sender, EventArgs e)
        {
            var selected = _listView.ListView.CheckBoxes ? _listView.CheckedObjects : _listView.SelectedObjects;
            var subtracted = selected.Except(FilteredUninstallers);
            ChangeSelection(subtracted);
        }

        /// <summary>
        /// Get total size of all visible uninstallers.
        /// </summary>
        public FileSize GetFilteredSize()
        {
            return FilteredUninstallers.Select(x => x.EstimatedSize).DefaultIfEmpty(FileSize.Empty)
                .Aggregate((size1, size2) => size1 + size2);
        }

        /// <summary>
        /// Get total size of selected uninstallers
        /// </summary>
        /// <returns></returns>
        public FileSize GetSelectedSize()
        {
            return SelectedUninstallers.Select(x => x.EstimatedSize).DefaultIfEmpty(FileSize.Empty)
                .Aggregate((size1, size2) => size1 + size2);
        }

        public void InitiateListRefresh()
        {
            _reference.SafeInvoke(StartListRefresh);
        }

        private async void StartListRefresh()
        {
            if (ListRefreshIsRunning || CheckIsAppDisposed())
                return;

            using var cancellation = new CancellationTokenSource();
            _refreshCancellation = cancellation;
            ScanProgress pendingProgress = null;
            using var progressTimer = new System.Windows.Forms.Timer { Interval = 65 };
            progressTimer.Tick += (_, _) =>
            {
                if (!CheckIsAppDisposed())
                    _reference.UpdateScanProgress(Interlocked.Exchange(ref pendingProgress, null));
            };
            bool completed = false;
            Exception error = null;
            ScanResult result = null;
            try
            {
                ListRefreshIsRunning = true;
                _reference.ShowScanProgress();
                _reference.LockApplication(true);
                progressTimer.Start();
                var token = cancellation.Token;
                result = await Task.Run(() => ScanApplications(
                    progress => Interlocked.Exchange(ref pendingProgress, progress), token), token);
                if (CheckIsAppDisposed() || token.IsCancellationRequested)
                    return;

                var previousIcons = _iconGetter;
                _iconGetter = result.Icons;
                AllUninstallers = result.Entries;
                error = result.IconError;
                result = null;
                var list = _listView.ListView;
                list.BeginUpdate();
                try
                {
                    list.SmallImageList = _iconGetter.IconList;
                    list.SetObjects(AllUninstallers);
                }
                finally
                {
                    list.EndUpdate();
                    previousIcons.Dispose();
                }
                completed = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                progressTimer.Stop();
                _refreshCancellation = null;
                result?.Icons.Dispose();
                if (!CheckIsAppDisposed())
                {
                    _reference.HideScanProgress();
                    _reference.LockApplication(false);
                }
            }

            if (CheckIsAppDisposed())
                return;

            if (completed)
            {
                _listView.ListView.Focus();
                ListRefreshIsRunning = false;
                _firstRefresh = false;
            }
            else
            {
                // A failed scan leaves the first-run steps pending until the next successful scan.
                _listRefreshIsRunning = false;
            }

            if (error != null && !CheckIsAppDisposed())
                PremadeDialogs.GenericError(error);
        }

        public void InvertSelectedItems(object sender, EventArgs e)
        {
            var selected = _listView.ListView.CheckBoxes ? _listView.CheckedObjects : _listView.SelectedObjects;
            var inverted = FilteredUninstallers.Except(selected);
            ChangeSelection(inverted);
        }

        private static ScanResult ScanApplications(Action<ScanProgress> report, CancellationToken cancellation)
        {
            var entries = ApplicationUninstallerFactory.GetUninstallerEntries(progress =>
            {
                cancellation.ThrowIfCancellationRequested();
                var item = progress.Inner;
                double itemFraction = item?.TotalCount > 0 ? Math.Clamp((double)item.CurrentCount / item.TotalCount, 0, 1) : 0;
                double? fraction = progress.TotalCount > 0
                    ? Math.Clamp((progress.CurrentCount + itemFraction) / (progress.TotalCount + 1), 0, 0.97)
                    : null;
                report(new ScanProgress(fraction, progress.Message, item?.Message));
            });

            cancellation.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(Program.InstalledRegistryKeyName))
                entries.RemoveAll(
                    x => PathTools.PathsEqual(x.RegistryKeyName, Program.InstalledRegistryKeyName));

            report(new ScanProgress(0.98, Localisable.Progress_Finishing_Icons, null));
            var icons = new UninstallerIconGetter();
            Exception iconError = null;
            try
            {
                icons.UpdateIconList(entries);
            }
            catch (Exception ex)
            {
                iconError = ex;
            }

            if (cancellation.IsCancellationRequested)
            {
                icons.Dispose();
                cancellation.ThrowIfCancellationRequested();
            }
            return new ScanResult(entries, icons, iconError);
        }

        internal sealed record ScanProgress(double? Fraction, string Stage, string Item);

        private sealed record ScanResult(IList<ApplicationUninstallerEntry> Entries, UninstallerIconGetter Icons, Exception IconError);

        internal void ReassignStartupEntries(bool refreshListView, IEnumerable<StartupEntryBase> items)
        {
            ApplicationUninstallerFactory.AttachStartupEntries(AllUninstallers, items);

            if (refreshListView)
                RefreshList();
        }

        public void RefreshList()
        {
            if (CheckIsAppDisposed())
                return;

            _listView.ListView.UpdateColumnFiltering();
            //_listView.ListView.BuildList(true); No need, UpdateColumnFiltering already does this
        }

        public void SelectAllItems(object sender, EventArgs e)
        {
            var selected = _listView.ListView.CheckBoxes ? _listView.CheckedObjects : _listView.SelectedObjects;
            var added = selected.Union(FilteredUninstallers);
            ChangeSelection(added);
        }

        /// <summary>
        /// Select first item starting with the keycode.
        /// If keycode leads to a valid selection true is returned. Otherwise, if there is nothing relevant to select false is
        /// returned.
        /// </summary>
        public bool SelectItemFromKeystroke(Keys keyCode)
        {
            var keyName = keyCode.ToLetterOrNumberString();

            if (keyName != null)
            {
                var selectedObj =
                    FilteredUninstallers.FirstOrDefault(
                        x => x.DisplayName.StartsWith(keyName, StringComparison.InvariantCultureIgnoreCase));

                _listView.ListView.DeselectAll();

                if (selectedObj != null)
                {
                    _listView.ListView.SelectObject(selectedObj, true);
                    _listView.ListView.EnsureModelVisible(selectedObj);

                    return true;
                }
            }
            return false;
        }

        public sealed class ListRefreshEventArgs : EventArgs
        {
            public ListRefreshEventArgs(bool refreshIsRunning, bool firstRefresh)
            {
                RefreshIsRunning = refreshIsRunning;
                FirstRefresh = firstRefresh;
            }

            public bool FirstRefresh { get; }

            public bool RefreshIsRunning { get; }
        }
    }
}
