using UnityEngine;

namespace ExpertExplorer
{
    internal static class ZoneHelper
    {
        public static ZoneData GetZoneData(Vector2i zone)
        {
            if (ZoneSystem.instance.m_locationInstances.TryGetValue(zone, out var instance))
            {
                ZoneData data = new ZoneData();
                data.ZoneId = zone;
                data.ZoneLocation = instance.m_location;
                data.LocationPosition = instance.m_position;
                data.IsPlaced = instance.m_placed;
                data.LocalizedLocationName = instance.m_location != null ? GetLocationName(instance.m_location.m_prefabName) : string.Empty;
                return data;
            }

            return null;
        }

        private static string GetLocationName(string prefabName)
        {
            return ExpertExplorer.Localization.TryTranslate($"location_{prefabName}");
        }
    }
}
