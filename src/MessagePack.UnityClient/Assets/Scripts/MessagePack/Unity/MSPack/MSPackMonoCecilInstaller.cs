// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// ReSharper disable DelegateSubtraction

namespace MSPack.Processor.Editor.Install
{
    // ReSharper disable once InconsistentNaming
    [InitializeOnLoad]
    public static class MSPackMonoCecilInstaller
    {
        private static ListRequest ListRequest;
        private static AddRequest AddRequest;
        private static string PackagePath;
        private static string CommonPackagePath;

        static MSPackMonoCecilInstaller()
        {
            if (File.Exists("Assets/Plugins/MSPack.Processor/Core.meta"))
            {
                return;
            }

            ListRequest = Client.List(offlineMode: true);
            EditorApplication.update += ListCollectionDone;
            DefineName();
        }

        private static void DefineName([CallerFilePath]string callerFilePath = "")
        {
            var directory = Path.GetDirectoryName(callerFilePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException(callerFilePath);
            }

            const string prefix = "MSPack.UnityEditor.";
            // ReSharper disable once StringLiteralTypo
            const string suffix = ".unitypackage";

#if UNITY_2019_3_OR_NEWER
            PackagePath = Path.Combine(directory, prefix + "2019.3" + suffix);
#elif UNITY_2018_4
            PackagePath = Path.Combine(directory, prefix + "2018.4" + suffix);
#else
            throw new NotSupportedException("This is not supported unity version.");
#endif
            CommonPackagePath = Path.Combine(directory, prefix + "Common" + suffix);
        }

        private static void ImportPackage()
        {
            Debug.Log(PackagePath);
            Debug.Log(CommonPackagePath);
            AssetDatabase.ImportPackage(PackagePath, false);
            AssetDatabase.ImportPackage(CommonPackagePath, false);
        }

        private static void ListCollectionDone()
        {
            switch (ListRequest.Status)
            {
                case StatusCode.InProgress:
                    return;
                case StatusCode.Success:
                    ProcessSuccessInListCollection(ListRequest.Result);
                    ListRequest = default;
                    EditorApplication.update -= ListCollectionDone;
                    break;
                case StatusCode.Failure:
                    ProcessFailInListCollection(ListRequest.Error);
                    ListRequest = default;
                    EditorApplication.update -= ListCollectionDone;
                    break;
                default:
                    EditorApplication.update -= ListCollectionDone;
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ProcessSuccessInListCollection(PackageCollection listRequestResult)
        {
            const string cecil = "nuget.mono-cecil";
            if (listRequestResult.Any(info => info.name == cecil))
            {
                ImportPackage();
                return;
            }

            AddRequest = Client.Add(cecil);
            EditorApplication.update += AddRequestDone;
        }

        private static void ProcessFailInListCollection(Error error)
        {
            switch (error.errorCode)
            {
                case ErrorCode.Unknown:
                    Debug.LogWarning(error.message);
                    break;
                case ErrorCode.NotFound:
                    Debug.LogWarning(error.message);
                    break;
                case ErrorCode.Forbidden:
                    Debug.LogWarning(error.message);
                    break;
                case ErrorCode.InvalidParameter:
                    Debug.LogWarning(error.message);
                    break;
                case ErrorCode.Conflict:
                    Debug.LogWarning(error.message);
                    break;
                default:
                    Debug.LogWarning(error.message);
                    break;
            }
        }

        private static void AddRequestDone()
        {
            switch (AddRequest.Status)
            {
                case StatusCode.InProgress:
                    return;
                case StatusCode.Success:
                    EditorApplication.update -= AddRequestDone;
                    Debug.Log("Add Mono.Cecil to packages.");
                    ImportPackage();
                    break;
                case StatusCode.Failure:
                    EditorApplication.update -= AddRequestDone;
                    Debug.LogError(AddRequest.Error.message);
                    break;
                default:
                    EditorApplication.update -= AddRequestDone;
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
#endif
