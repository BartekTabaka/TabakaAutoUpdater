using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core.Updater;

namespace App.Updater
{
    public partial class UpdaterForm : Form
    {
        private readonly WingetService m_WingetService = new();
        private CancellationTokenSource? m_Cts;
        private int m_PendingCount;

        public UpdaterForm()
        {
            InitializeComponent();
            Icon = new Icon("assets/icon.ico");      // Ikona okna
            WindowState = FormWindowState.Maximized; // Fullscreen
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await CheckUpdatesAsync();
        }

        private async Task CheckUpdatesAsync()
        {
            SetState(AppState.Checking);
            rtbOutput.Clear();
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
            catch (Exception ex)
            {
                AppendLine($"Błąd podczas sprawdzania: {ex.Message}", Color.OrangeRed);
            }
            finally
            {
                SetState(AppState.Ready);
            }

            //await CommandRunner.RunCommandRaw("winget upgrade --scope machine", line =>
            //{
            //    if (rtbOutput.InvokeRequired)
            //        rtbOutput.Invoke(() => AppendLine(line, ClassifyLine(line)));
            //    else
            //        AppendLine(line, Color.AliceBlue);
            //});
            //SetState(AppState.Ready);
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            m_Cts = new CancellationTokenSource();
            SetState(AppState.Updating);
            AppendLine("\n══ Rozpoczęcie aktualizacji ══\n", Color.Cyan);

            try
            {
                await m_WingetService.RunUpgradeAsync(line =>
                {
                    if (IsHandleCreated)
                        Invoke(() => AppendLine(line, ClassifyLine(line)));
                }, m_Cts.Token);

                AppendLine("\n══ Aktualizacja zakończona ══", Color.LightGreen);
            }
            catch (OperationCanceledException)
            {
                AppendLine("\nAktualizacja anulowana przez użytkownika.", Color.Yellow);
            }
            catch (Exception ex)
            {
                AppendLine($"\nBłąd: {ex.Message}", Color.OrangeRed);
            }
            finally
            {
                m_Cts.Dispose();
                m_Cts = null;
                SetState(AppState.Done);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e) => m_Cts?.Cancel();

        private void BtnCheck_Click(object sender, EventArgs e) => _ = CheckUpdatesAsync();

        private async void BtnRaw_Click(object sender, EventArgs e)
        {
            /*SetState(AppState.Checking);
            rtbOutput.Clear();
            AppendLine(" === DEBUG ===", Color.Red);

            // Funkcja wspomagająca (żeby nie pisać tego samego wielokrotnie)
            void AppendRaw(string line)
            {
                //AppendLine("AppendRaw", Color.Blue);
                if (rtbOutput.InvokeRequired)
                {
                    rtbOutput.Invoke(() => AppendLine(line, Color.LightGray));
                    //rtbOutput.Invoke(() => AppendLine("Invoke", Color.Blue));
                }
                else
                {
                    AppendLine("Bez Invoke", Color.Blue);
                    AppendLine(line, Color.LightGray);
                }
            }

            // ── Komendy ───────────────────────────────────────

            // Wersja winget
            AppendLine(" === WERSJA ===", Color.Red);
            await CommandRunner.RunCommandRaw("winget --version", line =>
            {
                AppendRaw(line);
            });

            // Lista aplikacji
            //AppendLine(" === LISTA ===", Color.Red);
            //await CommandRunner.RunCommand("winget list --source winget", line =>
            //{
            //    AppendRaw(line);
            //});

            // Aktualizacje
            AppendLine(" === AKTUALIZACJE ===", Color.Red);
            await CommandRunner.RunCommandRaw("winget upgrade --scope machine", line =>
            {
                AppendRaw(line);
            });

            SetState(AppState.Done);*/

            SetState(AppState.Checking);
            rtbOutput.Clear();
            AppendLine("=== DIAGNOSTICS ===", Color.Red);

            var report = await m_WingetService.DiagnoseAsync();

            // Każda linia raportu jako osobny wpis
            foreach (var line in report.Split('\n'))
            {
                var color = line.StartsWith("!!!") ? Color.OrangeRed
                          : line.StartsWith("[") && line.Contains("][ERR]") ? Color.Yellow
                          : line.Contains("PAKIET:") ? Color.LightGreen
                          : line.Contains("SEPARATOR") ? Color.Cyan
                          : line.Contains("UWAGA") ? Color.OrangeRed
                          : Color.LightGray;

                AppendLine(line, color);
            }

            SetState(AppState.Done);
        }

        private void PnlBottom_MouseMove(object sender, MouseEventArgs e)
        {
            var control = pnlBottom.GetChildAtPoint(e.Location);

            pnlBottom.Cursor = control is Button { Enabled: false } ? Cursors.No : Cursors.Default;
        }

        // ── Helpers ──────────────────────────────────────────────────

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
                    btnRaw.Enabled = false;
                    break;

                case AppState.Ready:
                    lblStatus.Text = "Gotowy";
                    btnCheck.Enabled = true;
                    btnUpdate.Enabled = m_PendingCount > 0;
                    btnCancel.Enabled = false;
                    btnRaw.Enabled = true;
                    break;

                case AppState.Updating:
                    lblStatus.Text = "Aktualizowanie...";
                    btnCheck.Enabled = false;
                    btnUpdate.Enabled = false;
                    btnCancel.Enabled = true;
                    btnRaw.Enabled = false;
                    break;

                case AppState.Done:
                    lblStatus.Text = "Zakończono";
                    btnCheck.Enabled = true;
                    btnUpdate.Enabled = false;
                    btnCancel.Enabled = false;
                    btnRaw.Enabled = true;
                    break;
            }
        }
    }
}