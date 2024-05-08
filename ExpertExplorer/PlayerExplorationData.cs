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

        public bool IsZoneLocationAlreadyDiscovered(Vector2i zone)
        {
            return DiscoveredLocations.ContainsKey(zone);
        }

        public void FlagAsDiscovered(ZoneData zoneData)
        {
            if (zoneData == null)
                return;

            if (zoneData.ZoneLocation == null)
                return;

            DiscoveredLocations[zoneData.ZoneId] = zoneData.ZoneLocation.Hash;
        }

        public void FlagAsDiscovered(Heightmap.Biome biome)
        {
            int biomeIndex = Heightmap.s_biomeToIndex[biome];

            if (DiscoveredBiomes.Contains(biomeIndex))
                return;

            DiscoveredBiomes.Add(biomeIndex);
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
            SaveValue(player, nameof(DiscoveredBiomes), pkg.GetBase64());
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

            if (LoadValue(fromPlayer, nameof(DiscoveredBiomes), out var discoveredBiomesData))
            {
                var pkg = new ZPackage(discoveredBiomesData);
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                    DiscoveredBiomes.Add(pkg.ReadInt());
            }

#if DEBUG
            Jotunn.Logger.LogDebug("Previously Discovered Locations:");
            foreach (var kvp in DiscoveredLocations)
            {
                var loc = ZoneSystem.instance.GetLocation(kvp.Value);
                Jotunn.Logger.LogDebug($"Location: {loc.m_prefabName}, Zone: {kvp.Key}");
            }
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
