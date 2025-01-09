# NuGet Package Installer for Unity

A minimal script to install NuGet packages into a Unity project. 
It does not automatically download dependencies, allowing you to choose which packages and dependencies to install.

If you need a more feature-rich solution with a user interface, 
consider using [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), a full-fledged package manager for NuGet built into Unity.

## Motivation

This script was created to provide a simple and lightweight solution for installing NuGet packages in Unity projects, 
without the overhead of a full-featured package manager.
I wanted it to be no more than a single file, and that I could use it via C# rather than a user interface.

## How to use

Here's an example of how to install the [FFMpegCore](https://www.nuget.org/packages/FFMpegCore) package and its dependencies:

```C#
private static bool InstallFFMpegCore()
{
	var packages = new (string packageId, string version)[]
	{
		("System.Runtime.CompilerServices.Unsafe", "6.0.0"),
		("Microsoft.Bcl.AsyncInterfaces", "7.0.0"),
		("System.Text.Encodings.Web", "7.0.0"),
		("System.Text.Json", "7.0.2"),
		("Instances", "3.0.0"),
		("FFMpegCore", "5.1.0"),
	};
	
	try
	{
        foreach (var (packageId, version) in packages)
        {
            NuGetPackageInstaller.InstallNuGetEditorPackage(packageId, version, isEditorOnly: true, refreshAfterInstall: false);
        }
		AssetDatabase.Refresh();
		return true;
	}
	catch (Exception e)
	{
		Debug.LogError("Error while installing FFMpegCore: " + e);
		return false;
	}
}
```
