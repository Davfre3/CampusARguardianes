using UnityEngine;

namespace CampusAR
{
    // Aparece con rebote (de 0 a 1 con sobreimpulso) y luego respira suavemente. Para insignias e iconos.
    public class PopIn : MonoBehaviour
    {
        public float retardo = 0f, duracion = 0.55f;
        public bool respirar = true;
        float t0;
        Vector3 escala = Vector3.one;
        bool guardada;

        void Awake() { if (!guardada) { escala = transform.localScale; guardada = true; } }
        void OnEnable() { t0 = Time.unscaledTime + retardo; transform.localScale = Vector3.zero; }

        void Update()
        {
            float t = Time.unscaledTime - t0;
            if (t < 0f) { transform.localScale = Vector3.zero; return; }
            float u = Mathf.Clamp01(t / duracion);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float k = 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
            float resp = (respirar && u >= 1f) ? 1f + 0.025f * Mathf.Sin((t - duracion) * 2.4f) : 1f;
            transform.localScale = escala * (k * resp);
        }
    }
}
