// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator
{
    internal static class ProcessUtil
    {
        public static async Task<int> ExecuteProcessAsync(string fileName, string args, Stream stdout, Stream stderr, TextReader stdin, CancellationToken ct = default(CancellationToken))
        {
            var psi = new ProcessStartInfo(fileName, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = stderr != null;
            psi.RedirectStandardOutput = stdout != null;
            psi.RedirectStandardInput = stdin != null;
            using (var proc = new Process())
            using (var cts = new CancellationTokenSource())
            using (var exitedct = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct))
            {
                proc.StartInfo = psi;
                proc.EnableRaisingEvents = true;
                proc.Exited += (sender, ev) =>
                {
                    cts.Cancel();
                };
                if (!proc.Start())
                {
                    throw new InvalidOperationException($"failed to start process(fileName = {fileName}, args = {args})");
                }

                int exitCode = 0;
                await Task.WhenAll(
                    Task.Run(() =>
                    {
                        exitCode = StdinTask(proc, stdin, exitedct, cts);
                        if (exitCode < 0)
                        {
                            proc.Dispose();
                        }
                    }),
                    Task.Run(async () =>
                    {
                        if (stdout != null)
                        {
                            await RedirectOutputTask(proc.StandardOutput.BaseStream, stdout, exitedct.Token, "stdout");
                        }
                    }),
                    Task.Run(async () =>
                    {
                        if (stderr != null)
                        {
                            await RedirectOutputTask(proc.StandardError.BaseStream, stderr, exitedct.Token, "stderr");
                        }
                    }));
                if (exitCode >= 0)
                {
                    return proc.ExitCode;
                }
                else
                {
                    return -1;
                }
            }
        }

        private static int StdinTask(Process proc, TextReader stdin, CancellationTokenSource exitedct, CancellationTokenSource cts)
        {
            if (stdin != null)
            {
                while (!exitedct.Token.IsCancellationRequested)
                {
                    var l = stdin.ReadLine();
                    if (l == null)
                    {
                        break;
                    }

                    proc.StandardInput.WriteLine(l);
                }

                proc.StandardInput.Dispose();
            }

            exitedct.Token.WaitHandle.WaitOne();
            if (cts.IsCancellationRequested)
            {
                proc.WaitForExit();
                var exitCode = proc.ExitCode;
                return exitCode;
            }
            else
            {
                proc.StandardOutput.Dispose();
                proc.StandardError.Dispose();
                proc.Kill();
                return -1;
            }
        }

        private static async Task RedirectOutputTask(Stream procStdout, Stream stdout, CancellationToken ct, string suffix)
        {
            if (stdout != null)
            {
                var buf = new byte[1024];
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var bytesread = await procStdout.ReadAsync(buf, 0, 1024, ct).ConfigureAwait(false);
                        if (bytesread <= 0)
                        {
                            break;
                        }

                        stdout.Write(buf, 0, bytesread);
                    }
                    catch (NullReferenceException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }
        }
    }
}
