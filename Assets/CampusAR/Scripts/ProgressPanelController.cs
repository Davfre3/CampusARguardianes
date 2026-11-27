using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Mi recorrido: cuatro estaciones con estado, contador 0-4 y acceso a la insignia (RF10, RF12).
    public class ProgressPanelController : MonoBehaviour
    {
        [HideInInspector] public AppFlowController flow;
        public TMP_Text[] filas;           // una por estación del catálogo
        public TMP_Text contador;
        public Image[] iconos;             // un dibujo por estación (gris = pendiente)
        public Button botonInsignia;

        void OnEnable() { ProgressService.Changed += Refrescar; Refrescar(); }
        void OnDisable() { ProgressService.Changed -= Refrescar; }

        public void Refrescar()
        {
            var cat = flow != null ? flow.catalogo : Resources.Load<StationCatalog>("CatalogoEstaciones");
            if (cat == null) return;
            int total = cat.estaciones.Length;
            for (int i = 0; i < filas.Length && i < total; i++)
            {
                var e = cat.estaciones[i];
                bool ok = ProgressService.EstaCompletada(e.stationId);
                filas[i].text = (ok ? "[X] " : "[  ] ") + e.mascotaNombre + " — " + e.displayName +
                                (ok ? "\n      COMPLETADO · insignia «" + e.insigniaNombre + "»" : "\n      Pendiente");
                if (iconos != null && i < iconos.Length && iconos[i] != null)
                {
                    Sprite sp = null;
                    try { sp = e.Icono(); } catch (System.Exception ex) { Debug.LogWarning("[CampusAR] " + ex.Message); }
                    iconos[i].sprite = sp; iconos[i].enabled = sp != null;
                    iconos[i].color = ok ? Color.white : new Color(0.45f, 0.45f, 0.5f, 0.9f);
                }
            }
            int n = ProgressService.Contar(cat);
            contador.text = n + " de " + total + " sellos";
            botonInsignia.interactable = n == total;
            var t = botonInsignia.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = n == total ? "Ver mi insignia" : "Insignia bloqueada (faltan " + (total - n) + ")";
        }
    }
}
