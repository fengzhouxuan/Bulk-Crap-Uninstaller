/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using BulkCrapUninstaller.Properties;
using Klocman.Forms;
using Klocman.Tools;

namespace BulkCrapUninstaller.Functions.Tools
{
    internal static class UpdateGrabber
    {
        /// <summary>
        ///     Look for updates while displaying a progress bar. At the end display a message box with the result.
        /// </summary>
        public static void LookForUpdates()
        {
            bool? result = null;
            Version latestVersion = null;
            var error = LoadingDialog.ShowDialog(null, Localisable.LoadingDialogTitleSearchingForUpdates,
                _ => { result = IsUpdateAvailable(Assembly.GetExecutingAssembly().GetName().Version, out latestVersion); });

            if (error == null)
            {
                switch (result)
                {
                    case null:
                        MessageBoxes.UpdateFailed("Unknown error");
                        break;

                    case true:
                        AskAndBeginUpdate(latestVersion);
                        break;

                    case false:
                        MessageBoxes.UpdateUptodate();
                        break;
                }
            }
            else
            {
                MessageBoxes.UpdateFailed(error.Message);
            }
        }

        public static void AskAndBeginUpdate(Version latestVersion)
        {
            if (MessageBoxes.UpdateAskToDownload(latestVersion))
            {
                try
                {
                    // Prevent log cleaner from running in portable builds
                    //EntryPoint.IsRestarting = true;

                    Process.Start(new ProcessStartInfo(LatestReleaseUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    EntryPoint.IsRestarting = false;

                    Console.WriteLine(ex);
                    MessageBoxes.UpdateFailed(ex.Message);
                }
            }
        }

        /// <summary>
        ///     Setup update system and automatically search for them if auto update is enabled.
        ///     Doesn't block, use delegates to interface.
        /// </summary>
        public static void Setup()
        {
            //UpdateSystem.UpdateFeedUri = Program.EnableDebug ? DebugUpdateFeedUri : UpdateFeedUri;
            //UpdateSystem.CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        ///     Automatically search for updates if auto update is enabled.
        ///     Doesn't block, use delegates to interface.
        /// </summary>
        /// <param name="canDisplayMessage">updateFoundCallback will be called after this returns true</param>
        /// <param name="updateFoundCallback">Launched only if a new update was found. It's launched from a background thread.</param>
        public static void AutoUpdate(Func<bool> canDisplayMessage, Action<Version> updateFoundCallback)
        {
            if (Settings.Default.MiscCheckForUpdates && WindowsTools.IsNetworkAvailable())
            {
                new Thread(() =>
                {
                    if (IsUpdateAvailable(Assembly.GetExecutingAssembly().GetName().Version, out var updateVersion) == true)
                    {
                        while (!canDisplayMessage())
                            Thread.Sleep(100);

                        try
                        {
                            updateFoundCallback(updateVersion);
                        }
                        catch
                        {
                            // Ignore background error, not necessary
                        }
                    }
                })
                { Name = "UpdateCheck_Thread", IsBackground = true }.Start();
            }
        }

        private const string ReleasesUrl = "https://github.com/fengzhouxuan/Bulk-Crap-Uninstaller/releases";
        private const string LatestReleaseUrl = ReleasesUrl + "/latest";

        private static Version CheckLatestVersion()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var request = new HttpRequestMessage(HttpMethod.Head, LatestReleaseUrl);
            using var response = client.Send(request);
            response.EnsureSuccessStatusCode();

            var url = response.RequestMessage?.RequestUri?.GetLeftPart(UriPartial.Path);
            if (string.Equals(url, ReleasesUrl, StringComparison.OrdinalIgnoreCase))
                return null;

            const string tagPrefix = ReleasesUrl + "/tag/";
            if (url == null || !url.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase) ||
                !Version.TryParse(url[tagPrefix.Length..].TrimStart('v', 'V'), out var version))
                throw new WebException("The release page did not contain a valid version.");

            return version;
        }

        /// <summary>
        /// Returns null if failed to look for updates, else returns if there is a newer version available
        /// </summary>
        public static bool? IsUpdateAvailable(Version currentVersion, out Version updateVersion)
        {
            updateVersion = null;
            try
            {
                var latestVersion = CheckLatestVersion();
                if (latestVersion != null && latestVersion > currentVersion)
                {
                    updateVersion = latestVersion;
                    Console.WriteLine("A new version is available: " + latestVersion);
                    return true;
                }
                else
                {
                    Console.WriteLine("The current version is the latest");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to check for new versions: " + e.Message);
                return null;
            }
        }

    }
}
