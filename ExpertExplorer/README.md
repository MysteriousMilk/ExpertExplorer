# Expert Explorer
A Valheim mod that adds a new Exploration Skill to the game. Exploring and finding new points of interest will increase the skill. As the skill increases, so will the player's sight range (on the minimap.) Discovered locations can be pinned to the map with a hotkey. Default hotkey is "P", but it can be changed in the config file.

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
- Pin discovered locations to the map with a hotkey
#### Localization
Localization is implemented and English localization is provided. I would love to expand to other languages in the future.

## Contact and Issue Reporting
You can report issues with the mod at the github link below.\
<https://github.com/MysteriousMilk/ExpertExplorer>

Additionally, you can reach me in the [Valheim Modding Discord](https://discord.com/invite/GUEBuCuAMz) under the name Milk.

## Changelog
**v1.4 - Auto-Pinning**
- Update to latest valheim version (0.218.21)
- Added several hotkeys for auto-pinning general pins to the minimap
  - Pin Home Icon (default RightCtrl + Numpad0)
  - Pin Point of Interest Icon (default RightCtrl + Numpad1)
  - Pin Ore Icon (default RightCtrl + Numpad2)
  - Pin Camp Icon (default RightCtrl + Numpad3)
  - Pin Dungeon Icon (default RightCtrl + Numpad4)
  - Pin Portal Icon (default RightCtrl + Numpad5)
- Additional Configuration Settings to toggle off mod related UI notifications

**v1.3.1 - Version Number Fix**\
Fix issue where version number did not have the "patch" digit, causing issues validating against the server version

**v1.3 - Dungeon Auto-Pinning Option**
- Added Auto-pinning option for dungeon locations
- Fixed issue where mod-tracked pins would not be removed from custom player data if the player removes the pin from the map
- Fixed issue where the key reported in the "Discovered Location" UI was static and not driven by the key code in the config

**v1.2 - UI Tweaks and Location Pinning**
- Added names for Ashlands locations
- Location text above minimap now displays the correct location when in an interior dungeon
- Replaced in-world text with MessageHud messages when a location is discovered
- Added ability to pin locations to the map with a hotkey
- Server synced config variables

**v1.1.2 - Player Save Fix**\
Fix issue where saved biome data would duplicate everytime the player entered a biome, thus driving up the player file size.

**v1.1.1 - Ashlands Update**\
Mod rebuilt to support the Ashlands update.

**v1.1.0 - Multiplayer Support**\
Mod now works in multiplayer. Added RPC calls to communicate location data between the server and client.

**v1.0.1 - Localization Fix**\
Internalized localization (at least for now) because r2modman condenses the directory structure to a single subdirectory.

**v1.0.0 - Initial Release**\
Implemented base mod functionality.