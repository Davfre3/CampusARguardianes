using UnityEngine;

namespace CampusAR
{
    [CreateAssetMenu(menuName = "Campus AR/Catálogo de estaciones", fileName = "CatalogoEstaciones")]
    public class StationCatalog : ScriptableObject
    {
        public StationDefinition[] estaciones;

        public StationDefinition PorId(string id)
        {
            foreach (var e in estaciones) if (e != null && e.stationId == id) return e;
            return null;
        }

        public StationDefinition PorTarget(string targetName)
        {
            foreach (var e in estaciones) if (e != null && e.targetName == targetName) return e;
            return null;
        }
    }
}
