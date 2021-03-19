# Testing fixes prior to release

To help us test changes prior to their release to nuget.org, you can of course build from source.
An easier approach may be to consume the built nuget packages from our CI/PR feed.

Add this feed to your nuget.config file:

```xml
<add key="MessagePack-CI" value="https://pkgs.dev.azure.com/ils0086/MessagePack-CSharp/_packaging/MessagePack-CI/nuget/v3/index.json" />
```

Then you can add or update your package reference to some version recently built in our CI or PR build.
PR builds always include a `-gCOMMITID` version suffix.
CI builds lack this, but may include a standard pre-release identifier such as `-alpha`.

If the change you seek is already merged, look for the latest version without the `-gCOMMITID` suffix.
If the change you seek is in an open PR, navigate to the PR build to find the version of the built package (it will be the build number).
