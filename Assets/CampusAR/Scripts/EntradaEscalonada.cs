using UnityEngine;

namespace CampusAR
{
    // Los hijos aparecen uno tras otro (fundido + crecimiento) al activarse el contenedor.
    public class EntradaEscalonada : MonoBehaviour
    {
        public float retardoEntreHijos = 0.07f;
        public float duracion = 0.28f;

        void OnEnable() { StartCoroutine(Correr()); }

        System.Collections.IEnumerator Correr()
        {
            int n = transform.childCount;
            var grupos = new CanvasGroup[n];
            for (int i = 0; i < n; i++)
            {
                var h = transform.GetChild(i).gameObject;
                var cg = h.GetComponent<CanvasGroup>(); if (cg == null) cg = h.AddComponent<CanvasGroup>();
                cg.alpha = 0f; h.transform.localScale = Vector3.one * 0.85f;   // PressScale la lleva a 1
                grupos[i] = cg;
            }
            for (int i = 0; i < n; i++)
            {
                float espera = i == 0 ? 0f : retardoEntreHijos;
                float te = 0f; while (te < espera) { te += Time.unscaledDeltaTime; yield return null; }
                StartCoroutine(Aparecer(grupos[i]));
            }
        }

        System.Collections.IEnumerator Aparecer(CanvasGroup cg)
        {
            float t = 0f;
            while (t < duracion && cg != null) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(t / duracion); yield return null; }
            if (cg != null) cg.alpha = 1f;
        }
    }
}
