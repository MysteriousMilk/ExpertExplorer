# Expert Explorer

A Valheim mod that adds a new Exploration Skill to the game. Exploring and finding new points of interest will increase the skill. As the skill increases, so will the player's sight range (on the minimap.)

## Jotunn Valheim Lib Development Environment Setup

How to setup the development enviroment for this project.

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: [BepInEx pack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
3. Unpack and copy the contents of `BepInExPack_Valheim` into your Valheim root folder. You should now see a new folder called `<ValheimDir>\unstripped_corlib` and more additional stuff.
4. Fork and clone this repository using git. That should create a new folder `JotunnModStub`. You can also [use the template function of github](https://github.com/Valheim-Modding/JotunnModStub/generate) to create a new, clean repo out of it and clone that.
5. Edit `DoPrebuild.props` in the project base path and change `ExecutePrebuild` to `true` if you want Jötunn to automatically generate publicized versions of the game dlls for you.
6. Open the Solution file `<JotunnModStub>\JotunnModStub.sln`. Right-click on the project or solution in the Solution Explorer and select `Manage NuGet packages...`. It should prompt you a message at the top that some NuGet-Packages are missing. Click "Restore" and restart Visual Studio when finished.
7. Rename the Solution/Project and everything related so that it resembles your own projects name. This includes the assembly information as well as the Unity project.

A new environment file `Environment.props` can be created in the projects base path `<JotunnModStub>`.
Make sure you are not in any subfolder.
Paste this snippet and change the paths accordingly.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Valheim install folder. This is normally found automatically, uncomment to overwrite it. Needs to be your path to the base Valheim folder. -->
    <!-- <VALHEIM_INSTALL>X:\PathToYourSteamLibary\steamapps\common\Valheim</VALHEIM_INSTALL>-->

    <!-- This is the folder where your build gets copied to when using the post-build automations -->
    <MOD_DEPLOYPATH>$(VALHEIM_INSTALL)\BepInEx\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```

### Post Build automations

Included in this repo is a PowerShell script `publish.ps1`. The script is referenced in the project file as a post-build event. Depending on the chosen configuration in Visual Studio the script executes the following actions.

### Building Debug

* The compiled dll file for this project is copied to `<ValheimDir>\BepInEx\plugins` (or whatever path you set as MOD_DEPLOYPATH).
* A .mdb file is generated for the compiled project dll and copied to `<ValheimDir>\BepInEx\plugins` (or whatever path you set as MOD_DEPLOYPATH).

### Building Release

* A compressed file with the binaries is created in `<JotunnModStub>\Packages`ready for upload to ThunderStore. Dont forget to include your information in the manifest.json and to change the project's readme file.