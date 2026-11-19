using UnityEngine;
using UnityEngine.EventSystems;

namespace CampusAR
{
    // Respuesta táctil suave: el botón se encoge un poco al tocarlo y vuelve con rebote leve.
    // También sirve para las entradas escalonadas (el script anima la escala hacia 1).
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float objetivo = 1f;
        public void OnPointerDown(PointerEventData e) { objetivo = 0.94f; }
        public void OnPointerUp(PointerEventData e) { objetivo = 1f; }
        public void OnPointerExit(PointerEventData e) { objetivo = 1f; }
        void OnEnable() { objetivo = 1f; }
        void Update()
        {
            float k = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * objetivo, k);
        }
    }
}
