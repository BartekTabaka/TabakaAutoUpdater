using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Updater
{
    public sealed class CommandRunner
    {
        public static async Task RunCommandRaw(
            string arguments,
            Action<string> onOutput,
            Action<int>? onExit = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var tcs = new TaskCompletionSource<int>();
            var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    onOutput(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    onOutput("[ERR] " + e.Data);
            };
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                onExit?.Invoke(process.ExitCode);
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await tcs.Task;
        }
    }
}