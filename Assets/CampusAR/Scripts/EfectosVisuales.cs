using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Chispas 3D temáticas alrededor de la mascota (monedas, engranajes, burbujas…) sin ParticleSystem:
    // usan primitivas con el material Resources/FxMat (incluido en la build).
    public class FxChispas : MonoBehaviour
    {
        class P { public Transform t; public Vector3 v; public float vida, max, tam; public Vector3 baseEsc; }
        readonly List<P> ps = new List<P>();
        static Material mat;
        static MaterialPropertyBlock mpb;

        public static void Rafaga(string estacionId, Vector3 centro, Color color, int n = 22, float radio = 0.05f)
        {
            if (mat == null)
            {
                mat = Resources.Load<Material>("FxMat");
                if (mat == null) { var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"); if (sh != null) mat = new Material(sh); }
            }
            if (mat == null) return;
            var go = new GameObject("Chispas");
            go.transform.position = centro;
            var fx = go.AddComponent<FxChispas>();
            fx.Emitir(estacionId, color, n, radio);
        }

        void Emitir(string id, Color color, int n, float radio)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color oro = new Color(1f, 0.82f, 0.2f);
            for (int i = 0; i < n; i++)
            {
                PrimitiveType tipo = PrimitiveType.Cube;
                Vector3 esc = Vector3.one;
                switch (id)
                {
                    case "admin": tipo = PrimitiveType.Cylinder; esc = new Vector3(1f, 0.12f, 1f); break;       // monedas
                    case "economia": tipo = PrimitiveType.Cylinder; esc = new Vector3(1f, 0.12f, 1f); break;
                    case "ciencias": tipo = PrimitiveType.Sphere; break;                                       // burbujas
                    default: tipo = PrimitiveType.Cube; esc = new Vector3(1f, 0.5f, 0.5f); break;              // chispas
                }
                var g = GameObject.CreatePrimitive(tipo);
                var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
                g.transform.SetParent(transform, false);
                var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                mpb.SetColor("_BaseColor", i % 3 == 0 ? oro : (i % 3 == 1 ? color : Color.Lerp(color, Color.white, 0.5f)));
                mpb.SetColor("_Color", i % 3 == 0 ? oro : color);
                r.SetPropertyBlock(mpb);
                float tam = Random.Range(0.010f, 0.020f);
                g.transform.localScale = Vector3.Scale(esc, Vector3.one * tam);
                g.transform.localRotation = Random.rotation;
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.8f + 0.3f;
                g.transform.localPosition = new Vector3(dir.x, 0, dir.z) * radio * 0.4f;
                float max = Random.Range(0.8f, 1.4f);
                ps.Add(new P { baseEsc = g.transform.localScale, t = g.transform, v = dir.normalized * Random.Range(0.06f, 0.16f), vida = 0f, max = max, tam = tam });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool alguna = false;
            foreach (var p in ps)
            {
                if (p.t == null) continue;
                p.vida += dt;
                if (p.vida >= p.max) { Destroy(p.t.gameObject); p.t = null; continue; }
                alguna = true;
                p.v += Vector3.down * 0.18f * dt;
                p.t.position += p.v * dt;
                p.t.Rotate(180f * dt, 240f * dt, 0f);
                float k = 1f - Mathf.Pow(p.vida / p.max, 3f);
                p.t.localScale = p.baseEsc * k;
            }
            if (!alguna) Destroy(gameObject);
        }
    }

    // Confeti de interfaz (acierto, insignia).
    public class ConfetiUI : MonoBehaviour
    {
        class C { public RectTransform r; public Vector2 v; public float giro, vida, max; }
        readonly List<C> cs = new List<C>();

        public static void Lanzar(Transform lienzo, Color color, int n = 70)
        {
            if (lienzo == null) return;
            var go = new GameObject("Confeti", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.transform.SetAsLastSibling();
            go.AddComponent<ConfetiUI>().Emitir(rt, color, n);
        }

        void Emitir(RectTransform rt, Color color, int n)
        {
            Color[] paleta = { color, new Color(1f, 0.82f, 0.2f), Color.white, Color.Lerp(color, Color.white, 0.5f), new Color(1f, 0.55f, 0.2f) };
            var rect = rt.rect;
            float w = Mathf.Max(rect.width, 400f), h = Mathf.Max(rect.height, 800f);
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("p", typeof(RectTransform), typeof(Image));
                g.transform.SetParent(rt, false);
                var im = g.GetComponent<Image>(); im.color = paleta[Random.Range(0, paleta.Length)]; im.raycastTarget = false;
                var r = (RectTransform)g.transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
                r.sizeDelta = new Vector2(Random.Range(14f, 28f), Random.Range(24f, 46f));
                r.anchoredPosition = new Vector2(Random.Range(-w * 0.5f, w * 0.5f), Random.Range(0f, 200f));
                r.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                cs.Add(new C { r = r, v = new Vector2(Random.Range(-120f, 120f), Random.Range(-1100f, -500f)), giro = Random.Range(-360f, 360f), max = Random.Range(1.6f, 2.8f) });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime; bool alguno = false;
            foreach (var c in cs)
            {
                if (c.r == null) continue;
                c.vida += dt;
                if (c.vida >= c.max) { Destroy(c.r.gameObject); c.r = null; continue; }
                alguno = true;
                c.v.x += Mathf.Sin(c.vida * 6f + c.giro) * 40f * dt;
                c.r.anchoredPosition += c.v * dt;
                c.r.Rotate(0, 0, c.giro * dt);
                var im = c.r.GetComponent<Image>(); var col = im.color; col.a = Mathf.Clamp01((c.max - c.vida) / 0.5f); im.color = col;
            }
            if (!alguno) Destroy(gameObject);
        }
    }
}
