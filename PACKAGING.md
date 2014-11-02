# How to package (because I can never remember)

## Update files

Make sure to update the assembly version and release notes in `NtlmProxy/MikeRogers.NtlmProxy.nuspec`.

Update the build version number in Appveyor.

## Do the NuGet dance

See [here](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package).

## The command that pays

... is this:

    (cd NtlmProxy && NuGet Pack NtlmProxy.csproj -Prop Configuration=Release)

## Party hat

Put on a party hat.
