using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using UnityEngine;
using static Heightmap;

namespace ExpertExplorer
{
    /// <summary>
    /// Custom player data to store the exploration data.
    /// Save/Load technique derived from RandyKnapp's EquipmentAndQuickSlots mod.
    /// https://github.com/RandyKnapp/ValheimMods/blob/main/EquipmentAndQuickSlots/EquipmentAndQuickSlots.cs
    /// </summary>
    public class PlayerExplorationData : MonoBehaviour
    {
        public Dictionary<Vector2s, int> DiscoveredLocations = new Dictionary<Vector2s, int>();
        public List<int> DiscoveredBiomes = new List<int>();
        public Dictionary<Vector2s, string> PinnedLocations = new Dictionary<Vector2s, string>();

        private static Dictionary<string, Heightmap.Biome> biomesByName = new Dictionary<string, Biome>()
        {
            { "$biome_meadows", Heightmap.Biome.Meadows },
            { "$biome_blackforest", Heightmap.Biome.BlackForest },
            { "$biome_swamp", Heightmap.Biome.Swamp },
            { "$biome_mountain", Heightmap.Biome.Mountain },
            { "$biome_plains", Heightmap.Biome.Plains },
            { "$biome_mistlands", Heightmap.Biome.Mistlands },
            { "$biome_ashlands", Heightmap.Biome.AshLands },
            { "$biome_deepnorth", Heightmap.Biome.DeepNorth },
            { "$biome_ocean", Heightmap.Biome.Ocean },
        };

        public bool IsZoneLocationAlreadyDiscovered(Vector2s zone)
        {
            return DiscoveredLocations.ContainsKey(zone);
        }

        public void FlagAsDiscovered(ZoneData zoneData)
        {
            if (zoneData == null)
                return;

            if (string.IsNullOrEmpty(zoneData.LocationPrefab))
                return;

            DiscoveredLocations[zoneData.ZoneId.ToVector2s()] = zoneData.LocationHash;

            Jotunn.Logger.LogInfo($"Discovered location {zoneData.LocalizedLocationName}");
        }

        public void FlagAsDiscovered(BiomeSector biome)
        {
            FlagAsDiscovered(biome.Biome);
        }

        public void FlagAsDiscovered(Heightmap.Biome biome)
        {
            try
            {
                int biomeIndex = (int)BiomeHelpers.ToBiomeIndex(biome);

                if (DiscoveredBiomes.Contains(biomeIndex))
                    return;

                DiscoveredBiomes.Add(biomeIndex);

                Jotunn.Logger.LogInfo($"Discovered biome {biome}");
            }
            catch (System.Exception)
            {
                Jotunn.Logger.LogError("Discovered unknown biome.");
            }
        }

        public void FlagAsPinned(Vector2s zone, Minimap.PinData pinData)
        {
            PinnedLocations[zone] = pinData.m_name;
        }

        public void RemovePin(Vector2s zone)
        {
            PinnedLocations.Remove(zone);
        }

        public void Save(Player player)
        {
            if (player == null)
            {
                Jotunn.Logger.LogError("Tried to save an PlayerExplorationData without a player!");
                return;
            }

            SaveValue(player, "PlayerExplorationData", ExpertExplorer.PluginVersion);

            // Save the discovered location array as a zpackage, then to a Base64 string.
            var pkg = new ZPackage();
            pkg.Write(DiscoveredLocations.Count);
            foreach (var kvp in DiscoveredLocations)
            {
                pkg.Write(kvp.Key);
                pkg.Write(kvp.Value);
            }
            SaveValue(player, nameof(DiscoveredLocations), pkg.GetBase64());

            // Save the discovered biome array as a zpackage, then to a Base64 string.
            pkg = new ZPackage();
            pkg.Write(DiscoveredBiomes.Count);
            foreach (int biomeIndex in  DiscoveredBiomes)
                pkg.Write(biomeIndex);
            SaveValue(player, "ExpertExplorerDiscoveredBiomes", pkg.GetBase64());

            // Save associated pins
            pkg = new ZPackage();
            pkg.Write(PinnedLocations.Count);
            foreach (var kvp in PinnedLocations)
            {
                pkg.Write(kvp.Key);
                pkg.Write(kvp.Value);
            }
            SaveValue(player, nameof(PinnedLocations), pkg.GetBase64());
        }

        public void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                Jotunn.Logger.LogError("Tried to load an PlayerExplorationData with a null player!");
                return;
            }

            LoadValue(fromPlayer, "PlayerExplorationData", out var ver);

            bool isLegacySave = IsLegacySave(ver);

            if (isLegacySave)
                Jotunn.Logger.LogInfo($"Loading legacy PlayerExplorationData.");

            // Load the Discovered Locations list
            if (isLegacySave)
                LoadLocationsLegacy(fromPlayer);
            else
                LoadLocations(fromPlayer);

#if DEBUG
            Jotunn.Logger.LogInfo($"Discoverd Location Count - {DiscoveredLocations.Count}");
#endif

            // Remove old biome storage (which bugged the file size)
            fromPlayer.m_customData.Remove("DiscoveredBiomes");

            // Load the Discovered Biome's list
            DiscoveredBiomes.Clear();
            LoadBiomes(fromPlayer);

#if DEBUG
            Jotunn.Logger.LogInfo($"Discoverd Biome Count - {DiscoveredBiomes.Count}");
#endif
            
            // In case the user just loaded this mod, see if they've already discovered some biomes
            foreach (var biome in fromPlayer.m_knownBiome)
                FlagAsDiscovered(GetBiomeFromName(biome));
            
            // Load the Pinned Locations list
            PinnedLocations.Clear();
            if (isLegacySave)
                LoadPinnedLocationsLegacy(fromPlayer);
            else
                LoadPinnedLocations(fromPlayer);

#if DEBUG
            Jotunn.Logger.LogInfo($"Pinned Location Count - {PinnedLocations.Count}");
#endif
        }

        private bool IsLegacySave(string versionString)
        {
            if (!Regex.IsMatch(versionString, @"^\d+\.\d+\.\d+$"))
                return true;

            System.Version saveVersion = new System.Version(versionString);
            System.Version legacySaveFormatVersion = new System.Version(ExpertExplorer.LegacySaveFormat);
            return saveVersion <= legacySaveFormatVersion;
        }

        private void LoadLocations(Player fromPlayer)
        {
            if (LoadValue(fromPlayer, nameof(DiscoveredLocations), out var discoveredLocationData))
            {
                var pkg = new ZPackage(discoveredLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2s zone = pkg.ReadVector2s();
                    DiscoveredLocations[zone] = pkg.ReadInt();
                }
            }
        }

        private void LoadLocationsLegacy(Player fromPlayer)
        {
            if (LoadValue(fromPlayer, nameof(DiscoveredLocations), out var discoveredLocationData))
            {
                var pkg = new ZPackage(discoveredLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2i zone = pkg.ReadVector2i();
                    DiscoveredLocations[zone.ToVector2s()] = pkg.ReadInt();
                }
            }
        }

        private void LoadBiomes(Player fromPlayer)
        {
            if (LoadValue(fromPlayer, "ExpertExplorerDiscoveredBiomes", out var discoveredBiomesData))
            {
                var pkg = new ZPackage(discoveredBiomesData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    int biomeIndex = pkg.ReadInt();
                    if (!DiscoveredBiomes.Contains(biomeIndex))
                        DiscoveredBiomes.Add(biomeIndex);
                }
            }
        }

        private void LoadPinnedLocations(Player fromPlayer)
        {
            if (LoadValue(fromPlayer, nameof(PinnedLocations), out var pinnedLocationData))
            {
                var pkg = new ZPackage(pinnedLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2s zone = pkg.ReadVector2s();
                    PinnedLocations[zone] = pkg.ReadString();
                }
            }
        }

        private void LoadPinnedLocationsLegacy(Player fromPlayer)
        {
            if (LoadValue(fromPlayer, nameof(PinnedLocations), out var pinnedLocationData))
            {
                var pkg = new ZPackage(pinnedLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2i zone = pkg.ReadVector2i();
                    PinnedLocations[zone.ToVector2s()] = pkg.ReadString();
                }
            }
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_customData.ContainsKey(key))
                player.m_customData[key] = value;
            else
                player.m_customData.Add(key, value);
        }

        private static bool LoadValue(Player player, string key, out string value)
        {
            if (player.m_customData.TryGetValue(key, out value))
                return true;
            return false;
        }

        private Heightmap.Biome GetBiomeFromName(string biomeName)
        {
            Heightmap.Biome biome = Heightmap.Biome.None;
            if (!biomesByName.TryGetValue(biomeName, out biome))
                biome = Heightmap.Biome.None;
            return biome;
        }
    }
}
