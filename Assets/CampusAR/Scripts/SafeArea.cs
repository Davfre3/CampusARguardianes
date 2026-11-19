using UnityEngine;

namespace CampusAR
{
    // Ajusta un RectTransform al área segura del teléfono (muescas, barras del sistema).
    [ExecuteAlways]
    public class SafeArea : MonoBehaviour
    {
        Rect aplicada;
        Vector2Int tamano;

        void OnEnable() { Aplicar(); }
        void Update() { if (Screen.safeArea != aplicada || tamano.x != Screen.width || tamano.y != Screen.height) Aplicar(); }

        void Aplicar()
        {
            var rt = transform as RectTransform;
            if (rt == null || Screen.width == 0 || Screen.height == 0) return;
            aplicada = Screen.safeArea;
            tamano = new Vector2Int(Screen.width, Screen.height);
            var min = aplicada.position;
            var max = aplicada.position + aplicada.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
