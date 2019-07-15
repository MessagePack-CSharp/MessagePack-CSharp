# Contributing

## How to Build

Open `MessagePack.sln` on Visual Studio 2017.

Unity project uses symbolic links. See [making symlinks work in Git for Windows](https://stackoverflow.com/a/42137273/46926),
or simply run this command before cloning:

    git config --global core.symlinks true

Unity Project is using symbolic link. At first, run `make_unity_symlink.bat` under `src\MessagePack.UnityClient`.
Then open that directory in the Unity Editor.
