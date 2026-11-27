using System;
using UnityEngine;

namespace CampusAR
{
    [Serializable]
    public class CareerLink
    {
        public string nombre;
        public string url;                 // página oficial de la carrera
        public string urlMalla;            // PDF o página con la malla curricular
        public string urlCertificaciones;  // vacío = el botón no se muestra
        public string urlEspecializacion;  // vacío = el botón no se muestra
    }

    [Serializable]
    public class QuizData
    {
        [TextArea] public string pregunta;
        public string[] opciones = new string[3];
        public int indiceCorrecto;
        [TextArea] public string explicacion;
        [TextArea] public string pista;
    }

    // Ficha de una estación (informe, sección 9). Un asset por estación en Assets/CampusAR/Datos.
    [CreateAssetMenu(menuName = "Campus AR/Estación", fileName = "Estacion_")]
    public class StationDefinition : ScriptableObject
    {
        public const string Pendiente = "[PENDIENTE de aprobación]";

        [Header("Identidad (estable, igual que en Vuforia)")]
        public string stationId;
        public string targetName;
        public string displayName;
        public string mascotaNombre;
        public Color colorFacultad = Color.white;
        public string grupoNombre;          // p. ej. "Administración y negocios"
        public string insigniaNombre;       // p. ej. "Estratega"
        public Sprite insigniaIcono;
        Sprite _iconoCache;
        // Devuelve el dibujo de la insignia; si no quedó asignado en el asset, lo carga de Resources/Insignias.
        public Sprite Icono()
        {
            if (insigniaIcono != null) return insigniaIcono;
            if (_iconoCache != null) return _iconoCache;
            string ruta = "Insignias/Insignia_" + stationId;
            try
            {
                _iconoCache = Resources.Load<Sprite>(ruta);
                if (_iconoCache == null)
                {
                    var t = Resources.Load<Texture2D>(ruta);
                    if (t != null) _iconoCache = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            catch (System.Exception ex) { Debug.LogWarning("[CampusAR] Icono de insignia no disponible (" + stationId + "): " + ex.Message); }
            return _iconoCache;
        }        // dibujo de la insignia (Assets/CampusAR/Insignias)
        [TextArea(2, 5)] public string infoCorta;   // mensaje breve de la mascota (burbuja y voz)
        public string mensajePersonalidad;  // frase corta de la mascota

        [Header("AR")]
        public GameObject mascotPrefab;
        public Texture2D markerImage;                 // zona de 14 x 10 cm, Read/Write activado
        public float markerWidthMeters = 0.14f;       // ancho físico registrado (se fuerza al crear el target)
        [Tooltip("XML de la base de Vuforia, relativo a StreamingAssets")]
        public string databasePath = "Vuforia/Trabajo_final.xml";
        [Tooltip("Nombre del target en la base. Si está vacío o falla, se usa markerImage.")]
        public string databaseTargetName;
        [Tooltip("Model Target (opcional): XML exportado por el Model Target Generator, relativo a StreamingAssets")]
        public string modelDatabasePath;
        [Tooltip("Nombre del Model Target dentro del XML. Vacío = sin reconocimiento por modelo.")]
        public string modelTargetName;

        [Header("Contenido")]
        [TextArea(2, 6)] public string summary;
        public CareerLink[] careers;
        public Sprite mapImage;
        [TextArea(2, 6)] public string directions = Pendiente;
        [TextArea(2, 6)] public string contact = Pendiente;
        [TextArea(2, 6)] public string serviceHours = Pendiente;
        public string webUrl;
        public Sprite ilustracion;   // arte de la placa (menús y pantalla de inicio)
        public string mapUrl;
        public QuizData quiz;
        public string contentVersion = "0.1-borrador";

        public static bool EsPendiente(string s)
        {
            return string.IsNullOrWhiteSpace(s) || s.Contains("PENDIENTE");
        }
    }
}
