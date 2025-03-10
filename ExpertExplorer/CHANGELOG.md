### Version 1.4.5
- Recompile to make compatible with Valheim patch 0.220.3.

### Version 1.4.4
------
- Added localization strings for the [More World Locations](https://thunderstore.io/c/valheim/p/warpalicious/) mods by [warpalicious](https://thunderstore.io/c/valheim/p/warpalicious/)

### Version 1.4.3
------
- Add code to search root mod directory for localization files if they are not found in the typical location

### Version 1.4.2
------
- Fix spawn issues introduced by the Bog Witch update
- Add Bog Witch location to list of available locations

### Version 1.4.1
------
- Fix issue where version number didn't have the patch digit (again)
- Updated to latest Jotunn framework
- Plugin correctly reads localization string from a file now

### Version 1.4
------
- Update to latest valheim version (0.218.21)
- Added several hotkeys for auto-pinning general pins to the minimap
  - Pin Home Icon (default RightCtrl + Numpad0)
  - Pin Point of Interest Icon (default RightCtrl + Numpad1)
  - Pin Ore Icon (default RightCtrl + Numpad2)
  - Pin Camp Icon (default RightCtrl + Numpad3)
  - Pin Dungeon Icon (default RightCtrl + Numpad4)
  - Pin Portal Icon (default RightCtrl + Numpad5)
- Additional Configuration Settings to toggle off mod related UI notifications


### Version 1.3.1
------
- Version number fix

### Version 1.3
------
- Added Auto-pinning option for dungeon locations
- Fixed issue where mod-tracked pins would not be removed from custom player data if the player removes the pin from the map
- Fixed issue where the key reported in the "Discovered Location" UI was static and not driven by the key code in the config

### Version 1.2
------
- Added names for Ashlands locations
- Location text above minimap now displays the correct location when in an interior dungeon
- Replaced in-world text with MessageHud messages when a location is discovered
- Added ability to pin locations to the map with a hotkey
- Server synced config variables

### Version 1.1.2
------
- Fix issue where saved biome data would duplicate everytime the player entered a biome, thus driving up the player file size. Affected player files will fix themselves upon the next save after this update.

### Version 1.1.1
------
- Rebuild for Ashlands update.

### Version 1.1.0
------
- Mod now works in multiplayer.
- Added RPC calls to communicate location data between the server and client.

### Version 1.0.1
------
- Internalized localization (at least for now) because r2modman condenses the directory structure to a single subdirectory.

### Version 1.0.0
------
- Initial mod release with base implementation.
- Provides a new "Exploration" skill that increases as the player discovers biomes and points of interest in the world.