using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CampusAR
{
    // Palanca en pantalla (táctil o ratón). Valor: x = derecha, y = adelante, entre -1 y 1.
    public class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
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

        void Actualizar(PointerEventData e)
        {
            if (baseRt == null || mango == null) return;
            Vector2 lp;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, e.pressEventCamera, out lp)) return;
            float radio = baseRt.rect.width * 0.5f * 0.62f;
            Valor = Vector2.ClampMagnitude(lp / radio, 1f);
            mango.anchoredPosition = Valor * radio;
        }

        public void OnPointerDown(PointerEventData e) { Actualizar(e); }
        public void OnDrag(PointerEventData e) { Actualizar(e); }
        public void OnPointerUp(PointerEventData e) { Reiniciar(); }
        void OnDisable() { Reiniciar(); }
        void Reiniciar() { Valor = Vector2.zero; if (mango != null) mango.anchoredPosition = Vector2.zero; }
    }
}
