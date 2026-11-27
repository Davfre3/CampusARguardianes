using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusAR
{
    // Palanca en pantalla (táctil o ratón). Valor: x = derecha, y = adelante, entre -1 y 1.
    public class JoystickVirtual : MonoBehaviour
    {
        public RectTransform baseRt, mango;
        public Vector2 Valor;
        static Sprite circulo;

        public static Sprite Circulo()
        {
            if (circulo != null) return circulo;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    byte a = (byte)(255f * Mathf.Clamp01((1f - d) * (n / 2f) / 1.5f));
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            circulo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circulo;
        }

        // Lectura directa del toque: sigue SOLO el dedo que empezó dentro de la zona, aunque haya otros dedos en pantalla.
        RectTransform zona;
        int idActivo = -1;          // -1 = ninguno, -2 = ratón
        const float Margen = 170f;  // zona de toque más grande que el dibujo
        Vector3 posInicial; bool tienePos; Vector2 origenPantalla; float radioPx;

        void Awake()
        {
            if (baseRt == null) baseRt = (RectTransform)transform;
            // capa invisible más grande: facilita agarrar la palanca y evita que el toque gire la mascota
            var go = new GameObject("Zona", typeof(RectTransform));
            zona = (RectTransform)go.transform;
            zona.SetParent(baseRt, false);
            zona.SetAsFirstSibling();
            zona.anchorMin = Vector2.zero; zona.anchorMax = Vector2.one;
            zona.offsetMin = new Vector2(-Margen, 0f); zona.offsetMax = new Vector2(Margen, Margen);   // sin invadir la barra de botones de abajo
            var im = go.AddComponent<Image>(); im.color = new Color(1, 1, 1, 0.003f); im.raycastTarget = true;
        }

        Camera Cam()
        {
            var c = GetComponentInParent<Canvas>();
            return c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        }

        void Update()
        {
            var cam = Cam();
            Vector2 pos; bool pulsado = false; bool inicio = false;
            var ts = Touchscreen.current;
            if (ts != null)
            {
                pos = Vector2.zero;
                if (idActivo >= 0)
                {
                    bool visto = false;
                    foreach (var tc in ts.touches)
                        if (tc.press.isPressed && tc.touchId.ReadValue() == idActivo) { pos = tc.position.ReadValue(); visto = true; break; }
                    if (!visto) { idActivo = -1; Reiniciar(); return; }
                    pulsado = true;
                }
                else
                {
                    foreach (var tc in ts.touches)
                        if (tc.press.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(zona, tc.position.ReadValue(), cam))
                        { idActivo = tc.touchId.ReadValue(); pos = tc.position.ReadValue(); pulsado = true; inicio = true; break; }
                }
            }
            else
            {
                var m = Mouse.current; if (m == null) return;
                pos = m.position.ReadValue();
                if (idActivo == -2) { pulsado = m.leftButton.isPressed; if (!pulsado) { idActivo = -1; Reiniciar(); return; } }
                else if (m.leftButton.wasPressedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(zona, pos, cam))
                { idActivo = -2; pulsado = true; inicio = true; }
            }
            if (!pulsado) return;
            if (inicio)
            {
                // palanca flotante: el centro es donde el dedo toca por primera vez (no importa dónde caiga)
                if (!tienePos) { posInicial = baseRt.position; tienePos = true; }
                Vector3 centro = baseRt.TransformPoint(baseRt.rect.center);
                baseRt.position += new Vector3(pos.x - centro.x, pos.y - centro.y, 0f);
                origenPantalla = pos;
                var cv = GetComponentInParent<Canvas>();
                float esc = cv != null ? cv.scaleFactor : 1f;
                radioPx = Mathf.Max(40f, 120f * esc);       // recorrido del dedo para llegar al máximo
            }
            Vector2 v = (pos - origenPantalla) / radioPx;
            float mag = v.magnitude;
            const float zonaMuerta = 0.08f;
            Valor = mag < zonaMuerta ? Vector2.zero : v.normalized * Mathf.Clamp01((mag - zonaMuerta) / (0.9f - zonaMuerta));
            float radioUi = baseRt.rect.width * 0.5f * 0.62f;
            mango.anchoredPosition = Vector2.ClampMagnitude(v, 1f) * radioUi;
        }

        void OnDisable() { idActivo = -1; Reiniciar(); }
        void Reiniciar()
        {
            Valor = Vector2.zero;
            if (mango != null) mango.anchoredPosition = Vector2.zero;
            if (tienePos && baseRt != null) baseRt.position = posInicial;     // vuelve a su sitio
        }
    }
}
