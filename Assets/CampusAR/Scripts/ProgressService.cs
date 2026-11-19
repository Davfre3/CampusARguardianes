using System;
using UnityEngine;

namespace CampusAR
{
    // Progreso local (RF09-RF13). Guarda solo identificadores de estaciones completadas y la
    // versión del formato. PlayerPrefs no está cifrado: no se guardan datos personales.
    public static class ProgressService
    {
        public const int FormatoActual = 1;
        const string ClaveFormato = "campusar.formato";
        const string PrefijoSello = "campusar.sello.";

        public static event Action Changed;

        static ProgressService()
        {
            if (!PlayerPrefs.HasKey(ClaveFormato)) PlayerPrefs.SetInt(ClaveFormato, FormatoActual);
        }

        public static bool EstaCompletada(string stationId)
        {
            return PlayerPrefs.GetInt(PrefijoSello + stationId, 0) == 1;
        }

        // Devuelve true solo la primera vez (un único sello por estación).
        public static bool Completar(string stationId)
        {
            if (string.IsNullOrEmpty(stationId) || EstaCompletada(stationId)) return false;
            PlayerPrefs.SetInt(PrefijoSello + stationId, 1);
            PlayerPrefs.SetInt(ClaveFormato, FormatoActual);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static int Contar(StationCatalog catalogo)
        {
            int n = 0;
            if (catalogo == null) return 0;
            foreach (var e in catalogo.estaciones) if (e != null && EstaCompletada(e.stationId)) n++;
            return n;
        }

        public static bool InsigniaDesbloqueada(StationCatalog catalogo)
        {
            return catalogo != null && catalogo.estaciones.Length > 0 && Contar(catalogo) == catalogo.estaciones.Length;
        }

        // Borra solo las claves del recorrido.
        public static void Reiniciar(StationCatalog catalogo)
        {
            if (catalogo != null)
                foreach (var e in catalogo.estaciones) if (e != null) PlayerPrefs.DeleteKey(PrefijoSello + e.stationId);
            PlayerPrefs.SetInt(ClaveFormato, FormatoActual);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
