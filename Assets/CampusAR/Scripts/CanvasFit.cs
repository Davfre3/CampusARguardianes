using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Mantiene visible todo el diseño de referencia (1080 x 1920) en cualquier pantalla de teléfono.
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasFit : MonoBehaviour
    {
        int ancho, alto;
        void OnEnable() { Aplicar(); }
        void Update() { if (Screen.width != ancho || Screen.height != alto) Aplicar(); }

        void Aplicar()
        {
            ancho = Screen.width; alto = Screen.height;
            var sc = GetComponent<CanvasScaler>();
            if (sc == null || alto == 0) return;
            float pantalla = (float)ancho / alto;
            float referencia = sc.referenceResolution.x / sc.referenceResolution.y;
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = pantalla > referencia ? 1f : 0f;
        }
    }
}
