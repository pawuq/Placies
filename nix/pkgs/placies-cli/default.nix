{
  lib,
  buildDotnetModule,
  dotnetCorePackages,
  nugetPackagesLockToNugetDeps,
  repoSrc,
}:
let
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
in
buildDotnetModule (finalAttrs: {
  pname = "Placies.Cli";
  version = lib.strings.fileContents "${repoSrc}/release-version.txt";
  inherit dotnet-sdk;
  inherit dotnet-runtime;
  src = repoSrc;
  projectFile = "src/Placies.Cli/Placies.Cli.fsproj";
  nugetDeps = nugetPackagesLockToNugetDeps {
    packagesLockJson = "${repoSrc}/src/Placies.Cli/packages.lock.json";
  };
})
