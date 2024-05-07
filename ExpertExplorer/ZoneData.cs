using UnityEngine;

namespace ExpertExplorer
{
    public class ZoneData
    {
        public Vector2i ZoneId {  get; set; }
        public ZoneSystem.ZoneLocation ZoneLocation { get; set; }
        public Vector3 LocationPosition { get; set; }
        public bool IsPlaced { get; set; }
        public string LocalizedLocationName { get; set; }

        public bool IsValid()
        {
            return ZoneLocation != null && IsPlaced;
        }
    }
}
