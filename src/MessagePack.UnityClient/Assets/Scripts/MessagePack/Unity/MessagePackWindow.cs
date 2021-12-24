#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MessagePack.Unity.Editor
{
    internal class MessagePackWindow : EditorWindow
    {
        static MessagePackWindow window;

        bool processInitialized;

        bool isDotnetInstalled;
        string dotnetVersion;

        bool isInstalledMpc;
        bool installingMpc;
        bool invokingMpc;

        MpcArgument mpcArgument;

        [MenuItem("Window/MessagePack/CodeGenerator")]
        public static void OpenWindow()
        {
            if (window != null)
            {
                window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<MessagePackWindow>("MessagePack CodeGen").Show();
        }

        async void OnEnable()
        {
            window = this; // set singleton.
            try
            {
                var dotnet = await ProcessHelper.FindDotnetAsync();
                isDotnetInstalled = dotnet.found;
                dotnetVersion = dotnet.version;

                if (isDotnetInstalled)
                {
                    isInstalledMpc = await ProcessHelper.IsInstalledMpc();
                }
            }
            finally
            {
                mpcArgument = MpcArgument.Restore();
                processInitialized = true;
            }
        }

        async void OnGUI()
        {
            if (!processInitialized)
            {
                GUILayout.Label("Check .NET Core SDK/CodeGen install status.");
                return;
            }
            if (mpcArgument == null)
            {
                return;
            }

            if (!isDotnetInstalled)
            {
                GUILayout.Label(".NET Core SDK not found.");
                GUILayout.Label("MessagePack CodeGen requires .NET Core Runtime.");
                if (GUILayout.Button("Open .NET Core install page."))
                {
                    Application.OpenURL("https://dotnet.microsoft.com/download");
                }
                return;
            }

            if (!isInstalledMpc)
            {
                GUILayout.Label("MessagePack CodeGen does not instaled.");
                EditorGUI.BeginDisabledGroup(installingMpc);

                if (GUILayout.Button("Install MessagePack CodeGen."))
                {
                    installingMpc = true;
                    try
                    {
                        var log = await ProcessHelper.InstallMpc();
                        if (!string.IsNullOrWhiteSpace(log))
                        {
                            UnityEngine.Debug.Log(log);
                        }
                        if (log != null && log.Contains("error"))
                        {
                            isInstalledMpc = false;
                        }
                        else
                        {
                            isInstalledMpc = true;
                        }
                    }
                    finally
                    {
                        installingMpc = false;
                    }
                    return;
                }

                EditorGUI.EndDisabledGroup();
                return;
            }

            EditorGUILayout.LabelField("-i input path(csproj or directory):");
            TextField(mpcArgument, x => x.Input, (x, y) => x.Input = y);

            EditorGUILayout.LabelField("-o output filepath(.cs) or directory(multiple):");
            TextField(mpcArgument, x => x.Output, (x, y) => x.Output = y);

            EditorGUILayout.LabelField("-m(optional) use map mode:");
            var newToggle = EditorGUILayout.Toggle(mpcArgument.UseMapMode);
            if (mpcArgument.UseMapMode != newToggle)
            {
                mpcArgument.UseMapMode = newToggle;
                mpcArgument.Save();
            }

            EditorGUILayout.LabelField("-c(optional) conditional compiler symbols(split with ','):");
            TextField(mpcArgument, x => x.ConditionalSymbol, (x, y) => x.ConditionalSymbol = y);

            EditorGUILayout.LabelField("-r(optional) generated resolver name:");
            TextField(mpcArgument, x => x.ResolverName, (x, y) => x.ResolverName = y);

            EditorGUILayout.LabelField("-n(optional) namespace root name:");
            TextField(mpcArgument, x => x.Namespace, (x, y) => x.Namespace = y);

            EditorGUILayout.LabelField("-ms(optional) Generate #if-- files by symbols, split with ','");
            TextField(mpcArgument, x => x.MultipleIfDirectiveOutputSymbols, (x, y) => x.MultipleIfDirectiveOutputSymbols = y);

            EditorGUI.BeginDisabledGroup(invokingMpc);
            if (GUILayout.Button("Generate"))
            {
                var commnadLineArguments = mpcArgument.ToString();
                UnityEngine.Debug.Log("Generate MessagePack Files, command:" + commnadLineArguments);

                invokingMpc = true;
                try
                {
                    var log = await ProcessHelper.InvokeProcessStartAsync("mpc", commnadLineArguments);
                    UnityEngine.Debug.Log(log);
                }
                finally
                {
                    invokingMpc = false;
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        void TextField(MpcArgument args, Func<MpcArgument, string> getter, Action<MpcArgument, string> setter)
        {
            var current = getter(args);
            var newValue = EditorGUILayout.TextField(current);
            if (newValue != current)
            {
                setter(args, newValue);
                args.Save();
            }
        }
    }

    internal class MpcArgument
    {
        public string Input;
        public string Output;
        public string ConditionalSymbol;
        public string ResolverName;
        public string Namespace;
        public bool UseMapMode;
        public string MultipleIfDirectiveOutputSymbols;

        static string Key => "MessagePackCodeGen." + Application.productName;

        public static MpcArgument Restore()
        {
            if (EditorPrefs.HasKey(Key))
            {
                var json = EditorPrefs.GetString(Key);
                return JsonUtility.FromJson<MpcArgument>(json);
            }
            else
            {
                return new MpcArgument();
            }
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString(Key, json);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("-i "); sb.Append(Input);
            sb.Append(" -o "); sb.Append(Output);
            if (!string.IsNullOrWhiteSpace(ConditionalSymbol))
            {
                sb.Append(" -c "); sb.Append(ConditionalSymbol);
            }
            if (!string.IsNullOrWhiteSpace(ResolverName))
            {
                sb.Append(" -r "); sb.Append(ResolverName);
            }
            if (UseMapMode)
            {
                sb.Append(" -m");
            }
            if (!string.IsNullOrWhiteSpace(Namespace))
            {
                sb.Append(" -n "); sb.Append(Namespace);
            }
            if (!string.IsNullOrWhiteSpace(MultipleIfDirectiveOutputSymbols))
            {
                sb.Append(" -ms "); sb.Append(MultipleIfDirectiveOutputSymbols);
            }

            return sb.ToString();
        }
    }

    internal static class ProcessHelper
    {
        const string InstallName = "messagepack.generator";

        public static async Task<bool> IsInstalledMpc()
        {
            var list = await InvokeProcessStartAsync("dotnet", "tool list -g");
            if (list.Contains(InstallName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<string> InstallMpc()
        {
            return await InvokeProcessStartAsync("dotnet", "tool install --global " + InstallName);
        }

        public static async Task<(bool found, string version)> FindDotnetAsync()
        {
            try
            {
                var version = await InvokeProcessStartAsync("dotnet", "--version");
                return (true, version);
            }
            catch
            {
                return (false, null);
            }
        }

        public static Task<string> InvokeProcessStartAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = Application.dataPath
            };

            Process p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Exception ex)
            {
                return Task.FromException<string>(ex);
            }

            var tcs = new TaskCompletionSource<string>();
            p.EnableRaisingEvents = true;
            p.Exited += (object sender, System.EventArgs e) =>
            {
                var data = p.StandardOutput.ReadToEnd();
                p.Dispose();
                p = null;

                tcs.TrySetResult(data);
            };

            return tcs.Task;
        }
    }
}

#endif
