# Expert Explorer
A Valheim mod that adds a new Exploration Skill to the game. Exploring and finding new points of interest will increase the skill. As the skill increases, so will the player's sight range (on the minimap.)

<table>
  <tr>
    <td>
        <img src="https://github.com/MysteriousMilk/ExpertExplorer/blob/master/Screenshots/Screenshot1.png?raw=true" width="400" alt="Discover Points of Interest" />
    </td>
    <td>
        <img src="https://github.com/MysteriousMilk/ExpertExplorer/blob/master/Screenshots/Screenshot3.png?raw=true" width="400" alt="Discover Points of Interest" />
    </td>
  </tr>
</table>

## Manual Install Instructions
BepInEx is required for this mod. Assuming BepInEx is already installed, locate the BepInEx folder in the Valheim installation directory (By default: C:\ProgramFiles(x86)\Steam\steamapps\common\Valheim\BepInEx) and navigate to the plugins folder. Unzip the contents of this package to a folder called "ExpertExplorer" within the plugins folder. This should be all that is required, assuming all mod dependencies are installed.

## Features
- New Skill: Exploration
- Dynamic sight range based on Exploration Skill
- Common names for locations / points of interest
- Minimap indication of current location
#### Localization
Localization is implemented and English localization is provided. I would love to expand to other languages in the future.

## Contact and Issue Reporting
You can report issues with the mod at the github link below.\
<https://github.com/MysteriousMilk/ExpertExplorer>

Additionally, you can reach me in the [Valheim Modding Discord](https://discord.com/invite/GUEBuCuAMz) under the name Milk.

## Changelog
**v1.1.1 - Ashlands Update**\
Mod rebuilt to support the Ashlands update.

**v1.1.0 - Multiplayer Support**\
Mod now works in multiplayer. Added RPC calls to communicate location data between the server and client.

**v1.0.1 - Localization Fix**\
Internalized localization (at least for now) because r2modman condenses the directory structure to a single subdirectory.

**v1.0.0 - Initial Release**\
Implemented base mod functionality.