using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    public enum Seccion { Facultad = 0, Carreras = 1, Mapa = 2, Atencion = 3 }

    // Muestra Facultad / Carreras / Mapa / Atención de la estación seleccionada (RF06).
    // El texto es desplazable; los enlaces externos se identifican como tales.
    public class InformationPanelController : MonoBehaviour
    {
        [HideInInspector] public AppFlowController flow;
        public TMP_Text titulo, subtitulo, cuerpo;
        public Image imagenMapa;
        public RectTransform contenedorEnlaces;
        public Button plantillaEnlace;
        public TMP_Text plantillaTitulo;
        public Button[] pestanas;
        public ScrollRect scroll;

        StationDefinition est;
        Seccion seccion;
        readonly List<GameObject> enlaces = new List<GameObject>();

        public void Mostrar(StationDefinition e, Seccion s)
        {
            est = e;
            CambiarSeccion((int)s);
        }

        public void CambiarSeccion(int indice)
        {
            if (est == null) return;
            seccion = (Seccion)indice;
            string[] nombres = { "Facultad", "Carreras", "Mapa", "Atención" };
            titulo.text = nombres[indice].ToUpperInvariant();
            if (subtitulo != null) subtitulo.text = est.displayName + "  ·  " + est.mascotaNombre;
            LimpiarEnlaces();
            imagenMapa.gameObject.SetActive(false);
            var sb = new StringBuilder();

            switch (seccion)
            {
                case Seccion.Facultad:
                    sb.AppendLine(Texto(est.summary));
                    sb.AppendLine();
                    sb.AppendLine("Mascota de la estación: " + est.mascotaNombre);
                    AgregarEnlace("Sitio web (requiere internet)", est.webUrl);
                    break;

                case Seccion.Carreras:
                    sb.AppendLine("Elige una carrera y abre sus documentos oficiales. Los botones abren el navegador y requieren internet.");
                    if (est.careers != null)
                        foreach (var c in est.careers)
                        {
                            AgregarTitulo(c.nombre);
                            bool mallaEsPagina = !string.IsNullOrEmpty(c.urlMalla) && c.urlMalla == c.url;
                            AgregarEnlace(mallaEsPagina ? "Malla curricular (página oficial)" : "Malla curricular", c.urlMalla, false);
                            AgregarEnlace("Certificaciones", c.urlCertificaciones, false);
                            AgregarEnlace("Áreas de especialización", c.urlEspecializacion, false);
                        }
                    sb.AppendLine();
                    sb.AppendLine("<size=70%>Información obtenida de fuentes oficiales de Universidad ESAN. Los planes y documentos pueden ser actualizados por la universidad.</size>");
                    break;

                case Seccion.Mapa:
                    sb.AppendLine("<b>Cómo llegar</b>");
                    sb.AppendLine(Texto(est.directions));
                    if (est.mapImage != null) { imagenMapa.sprite = est.mapImage; imagenMapa.gameObject.SetActive(true); }
                    else sb.AppendLine("\nPlano: " + StationDefinition.Pendiente);
                    AgregarEnlace("Abrir mapa (requiere internet)", est.mapUrl);
                    break;

                case Seccion.Atencion:
                    sb.AppendLine("Contacto: " + Texto(est.contact));
                    sb.AppendLine();
                    sb.AppendLine("Horario: " + Texto(est.serviceHours));
                    AgregarEnlace("Sitio web (requiere internet)", est.webUrl);
                    break;
            }

            cuerpo.text = sb.ToString();
            if (pestanas != null)
                for (int i = 0; i < pestanas.Length; i++)
                {
                    var t = pestanas[i].GetComponentInChildren<TMP_Text>();
                    string baseTxt = ((Seccion)i).ToString().Replace("Atencion", "Atención");
                    if (t != null) t.text = (i == indice) ? "[ " + baseTxt + " ]" : baseTxt;   // no depende solo del color
                }
            if (scroll != null) { Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1f; }
        }

        static string Texto(string s) { return StationDefinition.EsPendiente(s) ? (string.IsNullOrWhiteSpace(s) ? StationDefinition.Pendiente : s) : s; }

        void AgregarEnlace(string etiqueta, string url, bool mostrarSiPendiente = true)
        {
            bool valido = !string.IsNullOrWhiteSpace(url) && url.StartsWith("https://");
            if (!valido && !mostrarSiPendiente) return;      // sin recurso, sin botón vacío
            var b = Instantiate(plantillaEnlace, contenedorEnlaces);
            b.gameObject.SetActive(true);
            var t = b.GetComponentInChildren<TMP_Text>();
            t.text = valido ? etiqueta : etiqueta.Replace(" (requiere internet)", "") + " — enlace pendiente";
            b.interactable = valido;
            string u = url;
            b.onClick.AddListener(() => flow.AbrirEnlace(u));
            enlaces.Add(b.gameObject);
        }

        void AgregarTitulo(string texto)
        {
            if (plantillaTitulo == null) return;
            var t = Instantiate(plantillaTitulo, contenedorEnlaces);
            t.gameObject.SetActive(true);
            t.text = texto;
            enlaces.Add(t.gameObject);
        }

        void LimpiarEnlaces()
        {
            foreach (var g in enlaces) if (g != null) Destroy(g);
            enlaces.Clear();
        }
    }
}
