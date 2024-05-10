using UnityEngine;

namespace ExpertExplorer
{
    public class ZoneData
    {
        public Vector2i ZoneId {  get; set; }
        public string LocationPrefab { get; set; }
        public Vector3 LocationPosition { get; set; }
        public int LocationHash { get; set; }
        public float LocationRadiusMax { get; set; }
        public bool IsPlaced { get; set; }
        public string LocalizedLocationName { get; set; }
        public bool HasLocation { get; set; }

        public ZoneData()
        {
            ZoneId = new Vector2i();
            LocationPrefab = string.Empty;
            LocationPosition = new Vector3();
            LocationHash = 0;
            LocationRadiusMax = 0f;
            IsPlaced = false;
            LocalizedLocationName = string.Empty;
            HasLocation = false;
        }

        public bool IsValid()
        {
            return HasLocation && string.IsNullOrEmpty(LocationPrefab) == false && IsPlaced;
        }
    }
}
