using System;
using UnityEngine;

namespace ExpertExplorer
{
    public sealed class ZoneHelper
    {
        #region Singleton
        private static readonly ZoneHelper instance = new ZoneHelper();

        static ZoneHelper()
        {
        }

        private ZoneHelper()
        {
        }

        public static ZoneHelper Instance => instance;
        #endregion

        public Action<ZoneData> SetZoneDataAction;

        public void RegisterRPC(ZRoutedRpc routedRpc)
        {
#if DEBUG
            Jotunn.Logger.LogInfo("Registering ZoneHelper RPC methods.");
#endif

            // register remote procedure calls
            routedRpc.Register("RequestZoneData", new Action<long, int, int>(RPC_RequestZoneData));
            routedRpc.Register("SetZoneData", new Action<long, ZPackage>(RPC_SetZoneData));
            //transferZoneDataRpc = NetworkManager.Instance.AddRPC("TransferZoneDataRPC", TransferZoneDataRPCServerReceive, TransferZoneDataRPCClientReceive);
        }

        public ZoneData GetZoneData(Vector2i zone)
        {
            ZoneData data = new ZoneData();
            data.ZoneId = zone;

            if (ZoneSystem.instance.m_locationInstances.TryGetValue(zone, out var instance))
            {
                data.LocationPrefab = instance.m_location != null ? instance.m_location.m_prefabName : string.Empty;
                data.LocationRadiusMax = instance.m_location != null ? Mathf.Max(instance.m_location.m_exteriorRadius, instance.m_location.m_interiorRadius) : 0f;
                data.LocationPosition = instance.m_position;
                data.IsPlaced = instance.m_placed;
                data.LocalizedLocationName = instance.m_location != null ? GetLocationName(instance.m_location.m_prefabName) : string.Empty;
                data.HasLocation = instance.m_location != null;

#if DEBUG
                if (instance.m_location != null &&
                    instance.m_location.m_prefabName.StartsWith("MWL", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.LocalizedLocationName += " (MWL)";
                }
#endif

            }

            return data;
        }

        private string GetLocationName(string prefabName)
        {
            return ExpertExplorer.Localization.TryTranslate($"$location_{prefabName}");
        }

        public void Client_RequestZoneData(Vector2i zone)
        {
#if DEBUG
            Jotunn.Logger.LogInfo("Client - Requesting Zone Data.");
#endif

            ZRoutedRpc.instance.InvokeRoutedRPC("RequestZoneData", zone.x, zone.y);
        }

        private void RPC_RequestZoneData(long sender, int zoneX, int zoneY)
        {
#if DEBUG
            Jotunn.Logger.LogInfo("Server - Zone Data Request Received.");
#endif
            var zoneData = GetZoneData(new Vector2i(zoneX, zoneY));

            ZPackage packet = new ZPackage();
            packet.Write(zoneData.ZoneId);
            packet.Write(zoneData.LocationPrefab);
            packet.Write(zoneData.LocationPosition);
            packet.Write(zoneData.LocationHash);
            packet.Write(zoneData.LocationRadiusMax);
            packet.Write(zoneData.IsPlaced);
            packet.Write(zoneData.HasLocation);

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, "SetZoneData", packet);
        }

        private void RPC_SetZoneData(long sender, ZPackage zoneDataPkg)
        {
            if (zoneDataPkg == null)
            {
                Jotunn.Logger.LogWarning("Zone Data package was null.");
                return;
            }

            ZoneData zoneData = new ZoneData();
            zoneData.ZoneId = zoneDataPkg.ReadVector2i();
            zoneData.LocationPrefab = zoneDataPkg.ReadString();
            zoneData.LocationPosition = zoneDataPkg.ReadVector3();
            zoneData.LocationHash = zoneDataPkg.ReadInt();
            zoneData.LocationRadiusMax = zoneDataPkg.ReadSingle();
            zoneData.IsPlaced = zoneDataPkg.ReadBool();
            zoneData.HasLocation = zoneDataPkg.ReadBool();
            zoneData.LocalizedLocationName = string.IsNullOrEmpty(zoneData.LocationPrefab) == false ? ExpertExplorer.Localization.TryTranslate($"$location_{zoneData.LocationPrefab}") : string.Empty;

#if DEBUG
            Jotunn.Logger.LogInfo("Client - Received Zone Data. Invoking callback.");
#endif

            SetZoneDataAction?.Invoke(zoneData);
        }
    }
}
