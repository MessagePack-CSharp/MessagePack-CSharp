# Calculate the NPM package version, assuming the version change is in a new commit.
git commit --allow-empty -m "Dummy commit" -q
$NpmPackageVersion = (nbgv get-version -f json | ConvertFrom-Json).NpmPackageVersion
git reset --mixed HEAD~ -q

# Stamp the version into the package.json file and commit.
pushd $PSScriptRoot/src/MessagePack.UnityClient/Assets/Scripts/MessagePack
npm version $NpmPackageVersion --no-git-tag-version --allow-same-version
git add package.json
popd
git commit -m "Stamp unity package version as $NpmPackageVersion"

# Tag the release
nbgv tag
