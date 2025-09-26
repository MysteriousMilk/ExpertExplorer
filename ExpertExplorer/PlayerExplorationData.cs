using System.Collections.Generic;
using UnityEngine;

namespace ExpertExplorer
{
    /// <summary>
    /// Custom player data to store the exploration data.
    /// Save/Load technique derived from RandyKnapp's EquipmentAndQuickSlots mod.
    /// https://github.com/RandyKnapp/ValheimMods/blob/main/EquipmentAndQuickSlots/EquipmentAndQuickSlots.cs
    /// </summary>
    public class PlayerExplorationData : MonoBehaviour
    {
        public Dictionary<Vector2i, int> DiscoveredLocations = new Dictionary<Vector2i, int>();
        public List<int> DiscoveredBiomes = new List<int>();
        public Dictionary<Vector2i, string> PinnedLocations = new Dictionary<Vector2i, string>();

        public bool IsZoneLocationAlreadyDiscovered(Vector2i zone)
        {
            return DiscoveredLocations.ContainsKey(zone);
        }

        public void FlagAsDiscovered(ZoneData zoneData)
        {
            if (zoneData == null)
                return;

            if (string.IsNullOrEmpty(zoneData.LocationPrefab))
                return;

            DiscoveredLocations[zoneData.ZoneId] = zoneData.LocationHash;

            Jotunn.Logger.LogInfo($"Discovered location {zoneData.LocalizedLocationName}");
        }

        public void FlagAsDiscovered(Heightmap.Biome biome)
        {
            if (Heightmap.s_biomeToIndex.TryGetValue(biome, out int biomeIndex))
            {
                if (DiscoveredBiomes.Contains(biomeIndex))
                    return;

                DiscoveredBiomes.Add(biomeIndex);

                Jotunn.Logger.LogInfo($"Discovered biome {biome}");
            }
        }

        public void FlagAsPinned(Vector2i zone, Minimap.PinData pinData)
        {
            PinnedLocations[zone] = pinData.m_name;
        }

        public void RemovePin(Vector2i zone)
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

            SaveValue(player, "PlayerExplorationData", "This player is using PlayerExplorationData!");

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

            LoadValue(fromPlayer, "PlayerExplorationData", out var init);

            // Load the Discovered Locations list
            if (LoadValue(fromPlayer, nameof(DiscoveredLocations), out var discoveredLocationData))
            {
                var pkg = new ZPackage(discoveredLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2i zone = pkg.ReadVector2i();
                    DiscoveredLocations[zone] = pkg.ReadInt();
                }
            }

#if DEBUG
            Jotunn.Logger.LogInfo($"Discoverd Location Count - {DiscoveredLocations.Count}");
#endif

            // Remove old biome storage (which bugged the file size)
            fromPlayer.m_customData.Remove("DiscoveredBiomes");

            // Load the Discovered Biome's list
            DiscoveredBiomes.Clear();
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

#if DEBUG
            Jotunn.Logger.LogInfo($"Discoverd Biome Count - {DiscoveredBiomes.Count}");
#endif

            // In case the user just loaded this mod, see if they've already discovered some biomes
            foreach (var biome in fromPlayer.m_knownBiome)
                FlagAsDiscovered(biome);

            // Load the Pinned Locations list
            PinnedLocations.Clear();
            if (LoadValue(fromPlayer, nameof(PinnedLocations), out var pinnedLocationData))
            {
                var pkg = new ZPackage(pinnedLocationData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Vector2i zone = pkg.ReadVector2i();
                    PinnedLocations[zone] = pkg.ReadString();
                }
            }

#if DEBUG
            Jotunn.Logger.LogInfo($"Pinned Location Count - {PinnedLocations.Count}");
#endif
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
    }
}
