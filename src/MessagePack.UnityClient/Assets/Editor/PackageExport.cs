using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PackageExport
{
    [MenuItem("Tools/Export Unitypackage")]
    public static void Export()
    {
        var root = "Scripts/MessagePack";
        var exportPath = "../../bin/MessagePack.unitypackage";

        var path = Path.Combine(Application.dataPath, root);
        var assets = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetExtension(x) == ".cs")
            .Select(x => "Assets" + x.Replace(Application.dataPath, "").Replace(@"\", "/"))
            .ToArray();

        UnityEngine.Debug.Log("Export below files" + Environment.NewLine + string.Join(Environment.NewLine, assets));

        AssetDatabase.ExportPackage(
            assets,
            exportPath,
            ExportPackageOptions.Default);

        UnityEngine.Debug.Log("Export complete: " + Path.GetFullPath(exportPath));
    }
}
