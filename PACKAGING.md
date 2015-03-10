# How to package (because I can never remember)

## Update files

Make sure to update the assembly version, the release notes and version number in `NtlmProxy/MikeRogers.NtlmProxy.nuspec`, and the version number in the README file's example NuGet XML.

Update the build version number in Appveyor.

## Do the NuGet dance

See [here](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package) for details.

You'll probably want to install the Chocolatey `NuGet.CommandLine` package.

## The command that pays

... after compiling in Release mode, is this:

    (cd NtlmProxy && NuGet Pack NtlmProxy.csproj -Prop Configuration=Release)

## Package it up

Make a ZIP file with:

* `LICENSE`
* `README.md`
* `MikeRogers.NtlmProxy.dll`

Name it `MikeRogers.NtlmProxy-${version}.zip`.

## Version it

Create a tag with the version number and push it to GitHub.

## Release it

Upload the ZIP file and the NuGet file to GitHub.

```bat
> nuget setApiKey ${myKey}

> nuget push ${packageName}
```

## Party hat

Put on a party hat.
