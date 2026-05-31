using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Updater
{
    public sealed class WingetService
    {
        private sealed record CapturedLine(string Source, string Raw);

        // ═══════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════

        // Agreements flags to ensure non-interactive mode and avoid unexpected prompts during both check and upgrade operations.
        private const string m_SourceAgreements = "--accept-source-agreements";                          // For checking updates
        private const string m_AllAgreements = "--accept-package-agreements --accept-source-agreements"; // For running upgrades (accepting all agreements to prevent prompts during installation)

        // Checks for updates (waits for completion) and returns the count and list of package names that have available updates.
        public async Task<(int Count, List<string> Names)> CheckUpdatesAsync(CancellationToken ct = default)
        {
            var lines = new List<string>();
            int exitCode = await RunWingetAsync(
                $"upgrade {m_SourceAgreements}",  (_, raw) => lines.Add(raw), ct
            );

            if (exitCode != 0)
            {
                return (0, new List<string>()); // In case of error, return empty list (could also throw an exception or return a Result type)
            }
                
            return ParseUpgradeList(lines);
        }

        // Runs the upgrade process and invokes the provided callback for each line of output.
        public async Task RunUpgradeAsync(Action<string> onLine, CancellationToken ct = default)
        {
            await RunWingetAsync(
                $"upgrade --all --scope machine --silent --uninstall-previous {m_AllAgreements}",
                (src, raw) => onLine((src == "ERR" ? "[!] " : "") + StripAnsi(raw)), ct
            );
        }

        /// <summary>
        /// Uruchamia winget i zwraca pełny raport diagnostyczny jako string.
        /// Raport jest też zapisywany do katalogu "logs" w katalogu bazowym aplikacji.
        /// </summary>
        public async Task<string> DiagnoseAsync(CancellationToken ct = default)
        {
            const string command = $"upgrade {m_SourceAgreements}";
            var captured = new List<CapturedLine>();

            int exitCode = await RunWingetAsync(
                command,
                (src, raw) => captured.Add(new CapturedLine(src, raw)),
                ct);

            var sb = new StringBuilder();
            WriteHeader(sb, command, exitCode, captured.Count);
            WriteRawOutput(sb, captured);
            WriteParserTrace(sb, captured);

            var report = sb.ToString();

            // Save report to file
            try
            {
                // Ensure logs directory exists ("logs" folder where .exe is located)
                var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDir);

                var fileName = $"winget_diag_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var path = Path.Combine(logsDir, fileName); // Final path with filename
                await File.WriteAllTextAsync(path, report, Encoding.UTF8, ct);
                report += $"\n[Raport zapisany: {path}]";
            }
            catch { /* non-critical */ }

            return report;
        }

        // ═══════════════════════════════════════
        //  PARSING
        // ═══════════════════════════════════════

        /// <summary>
        /// Parses the output of the winget upgrade command and returns the count 
        /// and list of package names that have available updates.
        /// </summary>
        private static (int Count, List<string> Names) ParseUpgradeList(List<string> lines)
        {
            var names = new List<string>();
            bool pastSeparator = false;

            foreach (var raw in lines)
            {
                var line = StripAnsi(raw);

                if (!pastSeparator)
                {
                    if (line.TrimStart().StartsWith("---"))
                        pastSeparator = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (Regex.IsMatch(line.TrimStart(), @"^\d+ (upgrade|package)"))
                    break;

                var name = ExtractFirstColumn(line);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            return (names.Count, names);
        }

        private static string ExtractFirstColumn(string line)
        {
            var elipsisIndex = line.IndexOf('…');
            if (elipsisIndex >= 0)
                return line[..(elipsisIndex + 1)].Trim();

            var match = Regex.Match(line, @"^(.+?)\s{2,}");
            return match.Success ? match.Groups[1].Value.Trim() : line.Trim();
        }

        // ═══════════════════════════════════════
        //  PROCESS
        // ═══════════════════════════════════════

        private static async Task<int> RunWingetAsync(
            string args,
            Action<string, string> onLine, // (source: OUT/ERR, raw line)
            CancellationToken ct)
        {
            // Process Info
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // New process
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // Callbacks
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) onLine("OUT", e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) onLine("ERR", e.Data);
            };

            // Process start
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                // Wait for exit with cancellation support
                await process.WaitForExitAsync(ct);
                process.WaitForExit(); // flush remaining OutputDataReceived events (race condition fix)
            }
            catch (OperationCanceledException) // Cancellation requested, kill the process
            {
                process.Kill(entireProcessTree: true);
                throw;
            }

            return process.ExitCode;
        }

        // ═══════════════════════════════════════
        //  DIAGNOSTIC HELPERS
        // ═══════════════════════════════════════

        private static void WriteHeader(StringBuilder sb, string cmd, int exitCode, int lineCount)
        {
            sb.AppendLine("=== WINGET DIAGNOSTICS ===");
            sb.AppendLine($"Time      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Command   : winget {cmd}");
            sb.AppendLine($"Exit code : {exitCode}");
            sb.AppendLine($"Lines     : {lineCount} total (OUT + ERR)");
            sb.AppendLine();
        }

        private static void WriteRawOutput(StringBuilder sb, List<CapturedLine> captured)
        {
            sb.AppendLine("--- RAW OUTPUT (znaki kontrolne widoczne: \\r=<CR>, \\x1B=<ESC>) ---");

            for (int i = 0; i < captured.Count; i++)
            {
                var (src, raw) = captured[i];
                sb.AppendLine($"[{i:D3}][{src}] {MakeVisible(raw)}");
            }

            sb.AppendLine();
        }

        private static void WriteParserTrace(StringBuilder sb, List<CapturedLine> captured)
        {
            sb.AppendLine("--- PARSER TRACE ---");

            var names = new List<string>();
            bool pastSeparator = false;

            for (int i = 0; i < captured.Count; i++)
            {
                var stripped = StripAnsi(captured[i].Raw);
                var trimmed = stripped.TrimStart();

                var afterCrSplit = stripped
                    .Split('\r')
                    .Select(s => s.Trim())
                    .LastOrDefault(s => s.Length > 0) ?? stripped;

                bool crHidesContent = afterCrSplit != trimmed && afterCrSplit.Length > 0;

                if (!pastSeparator)
                {
                    if (trimmed.StartsWith("---"))
                    {
                        sb.AppendLine($"[{i:D3}] SEPARATOR znaleziony → zaczynam zbierać pakiety");
                        pastSeparator = true;
                    }
                    else
                    {
                        sb.Append($"[{i:D3}] skip (przed separatorem): \"{Truncate(MakeVisible(trimmed), 60)}\"");

                        if (crHidesContent)
                            sb.Append($"  !! po split('\\r'): \"{Truncate(afterCrSplit, 50)}\"");

                        sb.AppendLine();
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stripped))
                {
                    sb.AppendLine($"[{i:D3}] skip (pusta linia)");
                    continue;
                }

                if (Regex.IsMatch(trimmed, @"^\d+ (upgrade|package)"))
                    break;

                var name = ExtractFirstColumn(stripped);
                if (!string.IsNullOrEmpty(name))
                {
                    sb.AppendLine($"[{i:D3}] PAKIET: \"{name}\"");
                    names.Add(name);
                }
                else
                {
                    sb.AppendLine($"[{i:D3}] skip (nie udało się wyciągnąć nazwy): \"{Truncate(MakeVisible(trimmed), 60)}\"");
                }
            }

            if (!pastSeparator)
                sb.AppendLine("!!! UWAGA: linia separatora '---' NIE ZOSTAŁA ZNALEZIONA — to prawdopodobna przyczyna buga !!!");

            sb.AppendLine();
            sb.AppendLine($"Wynik: {names.Count} pakiet(y)");
            foreach (var n in names)
                sb.AppendLine($"  - {n}");
        }

        /// <summary> Zamienia znaki kontrolne na czytelne znaczniki, np. \r → <CR> </summary>
        private static string MakeVisible(string input)
        {
            var sb = new StringBuilder(input.Length * 2);
            foreach (var c in input)
            {
                sb.Append(c switch
                {
                    '\r' => "<CR>",
                    '\n' => "<LF>",
                    '\t' => "<TAB>",
                    '\x1B' => "<ESC>",
                    < ' ' => $"<{(int)c:X2}>",
                    _ => c.ToString()
                });
            }
            return sb.ToString();
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";

        private static readonly Regex s_AnsiRegex = new(@"\x1B\[[0-9;]*[mK]", RegexOptions.Compiled);
        private static string StripAnsi(string input) => s_AnsiRegex.Replace(input, string.Empty);
    }
}
