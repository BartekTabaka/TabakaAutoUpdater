using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Updater
{
    public sealed class WingetService
    {
        public async Task<(int Count, List<string> Names)> CheckUpdatesAsync(CancellationToken ct = default)
        {
            var lines = new List<string>();
            await RunWingetAsync("upgrade --scope machine", line => lines.Add(line), ct);
            return ParseUpgradeList(lines);
        }

        public Task RunUpgradeAsync(Action<string> onLine, CancellationToken ct = default)
            => RunWingetAsync("upgrade --all --scope machine --silent --uninstall-previous", onLine, ct);

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

                // Linia podsumowania: "2 packages have updates available."
                if (char.IsDigit(line.TrimStart()[0]))
                    break;

                var name = ExtractFirstColumn(line);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            return (names.Count, names);
        }

        // Kolumny winget są oddzielone >=2 spacjami
        private static string ExtractFirstColumn(string line)
        {
            var match = Regex.Match(line, @"^(.+?)\s{2,}");
            return match.Success ? match.Groups[1].Value.Trim() : line.Trim();
        }

        private static async Task RunWingetAsync(string args, Action<string> onLine, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    onLine(StripAnsi(e.Data));
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    onLine("[!] " + StripAnsi(e.Data));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
        }

        private static readonly Regex s_AnsiRegex = new(@"\x1B\[[0-9;]*[mK]", RegexOptions.Compiled);
        private static string StripAnsi(string input) => s_AnsiRegex.Replace(input, string.Empty);
    }
}