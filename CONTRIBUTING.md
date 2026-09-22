# Contributing

## Dependencies

* [Visual Studio 2022](https://visualstudio.microsoft.com/)
* [Unity Editor](https://unity3d.com/unity/editor) (optional)
* .NET Core SDK and runtimes (run `init` to install)

This repo uses the .NET Core SDK and runtimes to build and test the projects.
You can install the right versions of the SDK and runtimes by running the `init.ps1` script at the root of the repo.

By default no elevation is required as these toolsets are installed in a per-user directory. Launching `devenv` from the same PowerShell window that you ran the script will lead VS to discover these per-user toolsets.
To get VS to find the toolsets when launched from the Start Menu, run `init -InstallLocality machine`, which requires elevation for each SDK or runtime installed.

## How to Build

Open `MessagePack.sln` on Visual Studio 2022.

Alternatively you may build from the command line using `msbuild.exe` or:

    dotnet build /p:platform=NoVSIX

## Unity

See the ReadMe for the target directory `src\MessagePack.UnityClient` for information on building and managing with Unity. Unity's CI is managed in `unity.yml` in GitHub Actions.

## How to Publish Package

Package publishing is triggered via GitHub Actions using workflow_dispatch. Follow these steps:

1. Select Actions -> "Run release build and publish to NuGet"
2. Enter a version tag (e.g., `v3.0.1`)
3. Click "Run workflow"

![image](https://github.com/user-attachments/assets/74886c88-f6d1-4108-8ce1-02d3d1b31f1f)

The workflow will:
- Update the version in [MessagePack.UnityClient/Assets/Scripts/MessagePack/package.json](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/src/MessagePack.UnityClient/Assets/Scripts/MessagePack/package.json)
- Commit and push the change
- Build the .NET library
- Publish to [NuGet/MessagePack](https://www.nuget.org/packages/MessagePack)
- Create a draft GitHub release

After CI completion, edit the release draft to add relevant release notes and announcements.

### Secret

The following secrets are managed at the organization level:

* `UNITY_EMAIL`
* `UNITY_LICENSE`
* `UNITY_PASSWORD`
* `NUGET_KEY`

The `UNITY_*` secrets are personal license keys required for Unity builds.

`NUGET_KEY` is a key required for releasing nupkg files, and since it has a 365-day expiration period, the key needs to be regenerated when it expires.