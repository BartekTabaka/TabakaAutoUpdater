using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core.Updater;

////////////
///
/// TODO:
/// 1. Zamiana polskich komentarzy na angielskie                        ( -> 1.2.0 )
/// 2. Dodanie listy błędów i wypisywanie co poszło nie tak na końcu    ( -> 1.3.0 )
/// 3. Sprawdzanie czy wszystkie potrzebne aplikacje są zainstalowane   ( -> 2.0.0 )
/// 
////////////

namespace App.Updater
{
    public partial class UpdaterForm : Form
    {
        private readonly WingetService m_WingetService = new();
        private CancellationTokenSource? m_Cts;
        private int m_PendingCount;
        private bool m_UpdateLaunchedThisSession = false;

        // Variables for the currently updated application
        private string m_CurrentUpdatePackage = "";
        private static readonly Regex s_PackageFoundRegex = new(@"Found (.+?) \[", RegexOptions.Compiled);
        private static bool IsProgressLine(string line) => line.Contains('█') || line.Contains('░') || line.Contains('▒');

        public UpdaterForm()
        {
            InitializeComponent();
            Icon = new Icon("assets/icon.ico");      // Window icon
            //WindowState = FormWindowState.Maximized; // Fullscreen
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await CheckUpdatesAsync();
            // Auto checks for updates after opening the window
        }

        /// <summary>
        /// Checks for available updates and updates the UI accordingly.
        /// Uses the WingetService to get the list of upgradable applications.
        /// </summary>
        private async Task CheckUpdatesAsync()
        {
            SetState(AppState.Checking);

            // To separate the validation results after the update so they aren't "stuck together"
            if (m_UpdateLaunchedThisSession) AppendLine("\n\n");

            AppendLine("Sprawdzanie dostępnych aktualizacji...", Color.Gray);

            try
            {
                var (count, names) = await m_WingetService.CheckUpdatesAsync();
                m_PendingCount = count;
                lblCount.Text = $"Aplikacji do aktualizacji: {count}";

                if (count > 0)
                {
                    AppendLine($"Znaleziono {count} aplikacji do aktualizacji:", Color.White);
                    foreach (var name in names)
                        AppendLine($"  • {name}", Color.LightGray);
                    AppendLine("");
                }
                else
                {
                    AppendLine("Wszystkie aplikacje są aktualne.", Color.LightGreen);
                }
            }
            catch (Exception ex) // Error catching
            {
                AppendLine($"Błąd podczas sprawdzania: {ex.Message}", Color.OrangeRed);
            }
            finally
            {
                SetState(AppState.Ready);
            }
        }

        // Updating button
        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            m_UpdateLaunchedThisSession = true;

            m_Cts = new CancellationTokenSource();
            SetState(AppState.Updating);
            AppendLine("\n══ Rozpoczęcie aktualizacji ══\n", Color.Cyan);
            m_CurrentUpdatePackage = "";

            try
            {
                // Run upgrading process through WingetService
                await m_WingetService.RunUpgradeAsync(line =>
                {
                    if (IsHandleCreated)
                        Invoke(() => HandleUpgradeLine(line));
                }, m_Cts.Token);

                AppendLine("\n══ Aktualizacja zakończona ══", Color.DeepPink);
            }
            catch (OperationCanceledException) // Cancellation handling
            {
                AppendLine("\nAktualizacja anulowana przez użytkownika.", Color.Yellow);
            }
            catch (Exception ex) // General error handling
            {
                AppendLine($"\nBłąd: {ex.Message}", Color.OrangeRed);
            }
            finally // Cleanup
            {
                m_Cts.Dispose();
                m_Cts = null;
                m_CurrentUpdatePackage = "";
                SetState(AppState.Done);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e) => m_Cts?.Cancel();

        private void BtnCheck_Click(object sender, EventArgs e) => _ = CheckUpdatesAsync();

        // Diagnostics button
        private async void btnDebug_Click(object sender, EventArgs e)
        {
            SetState(AppState.Checking);
            rtbOutput.Clear();
            AppendLine("=== DIAGNOSTICS ===", Color.Red);

            // Run diagnostics and get the report
            var report = await m_WingetService.DiagnoseAsync();

            // Every line is classified for better readability (errors in red, found packages in green, etc.)
            foreach (var line in report.Split('\n'))
            {
                var color = line.StartsWith("!!!") ? Color.OrangeRed
                          : line.StartsWith("[") && line.Contains("][ERR]") ? Color.Yellow
                          : line.Contains("PAKIET:") ? Color.LightGreen
                          : line.Contains("SEPARATOR") ? Color.Cyan
                          : line.Contains("UWAGA") ? Color.OrangeRed
                          : line.Contains("Wszystkie pakiety są aktualne") ? Color.LightGreen
                          : line.Contains("Wynik:") ? Color.MediumPurple
                          : Color.LightGray;

                AppendLine(line, color);
            }

            SetState(AppState.Done);
        }

        /// <summary>
        /// Updates the cursor based on whether it's hovering over a disabled button in the bottom panel.
        /// </summary>
        private void PnlBottom_MouseMove(object sender, MouseEventArgs e)
        {
            var control = pnlBottom.GetChildAtPoint(e.Location);

            pnlBottom.Cursor = control is Button { Enabled: false } ? Cursors.No : Cursors.Default;
        }

        // ── Helpers ──────────────────────────────────────────────────

        // Skips appending lines that are just spinner characters
        private static readonly HashSet<string> s_SpinnerChars = ["|", "/", "-", "\\"];
        private void AppendLine(string text, Color color)
        {
            if (s_SpinnerChars.Contains(text.Trim())) return;
            if (string.IsNullOrWhiteSpace(text)) return;

            rtbOutput.SelectionStart = rtbOutput.TextLength;
            rtbOutput.SelectionLength = 0;
            rtbOutput.SelectionColor = color;
            rtbOutput.AppendText(text + "\n");
            rtbOutput.ScrollToCaret();
        }
        private void AppendLine(string text) => AppendLine(text, Color.White);

        // Line classification based on keywords for better readability in the output.
        private static Color ClassifyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return Color.White;
            var l = line.ToLowerInvariant();

            if (l.Contains("downloading") || l.Contains("installing"))
                return Color.DeepSkyBlue;
            if (l.Contains("successfully") || l.Contains("installed"))
                return Color.LightGreen;
            if (l.Contains("failed") || l.Contains("error") || l.StartsWith("[!]"))
                return Color.OrangeRed;
            if (l.Contains("found "))
                return Color.Cyan;

            return Color.White;
        }

        // Handles status and progress tracking for updates and downloads
        private void HandleUpgradeLine(string line)
        {
            // Search for the line indicating the next update
            // for example: (1/2) Found Google Chrome (EXE) [Google.Chrome.EXE] Version X.X.X.X
            var match = s_PackageFoundRegex.Match(line);
            if (match.Success)
            {
                m_CurrentUpdatePackage = match.Groups[1].Value.Trim();
                lblStatus.Text = $"Aktualizowanie {m_CurrentUpdatePackage}";
                AppendLine($"\n{line}", ClassifyLine(line));
                return;
            }

            // Adding a prefix to the download progress bar
            if (IsProgressLine(line))
            {
                var prefix = string.IsNullOrEmpty(m_CurrentUpdatePackage) ?
                    "" : $"Pobieranie {m_CurrentUpdatePackage} -  ";
                var display = prefix + line.TrimStart();

                AppendLine(display, Color.MediumOrchid);
                return;
            }

            // Normal formatting of the remaining lines
            AppendLine(line, ClassifyLine(line));
        }

        /// <summary>
        /// App state management to control the UI elements (buttons, status label) 
        /// based on the current operation (checking, ready, updating, done).
        /// </summary>
        private enum AppState { Checking, Ready, Updating, Done }
        private void SetState(AppState state)
        {
            switch (state)
            {
                case AppState.Checking:
                    lblStatus.Text = "Sprawdzanie...";
                    btnCheck.Enabled = false;
                    btnUpdate.Enabled = false;
                    btnCancel.Enabled = false;
                    btnDebug.Enabled = false;
                    break;

                case AppState.Ready:
                    lblStatus.Text = "Gotowy";
                    btnCheck.Enabled = true;
                    btnUpdate.Enabled = m_PendingCount > 0;
                    btnCancel.Enabled = false;
                    btnDebug.Enabled = true;
                    break;

                case AppState.Updating:
                    lblStatus.Text = "Aktualizowanie...";
                    btnCheck.Enabled = false;
                    btnUpdate.Enabled = false;
                    btnCancel.Enabled = true;
                    btnDebug.Enabled = false;
                    break;

                case AppState.Done:
                    lblStatus.Text = "Zakończono";
                    btnCheck.Enabled = true;
                    btnUpdate.Enabled = false;
                    btnCancel.Enabled = false;
                    btnDebug.Enabled = true;
                    break;
            }
        }
    }
}
