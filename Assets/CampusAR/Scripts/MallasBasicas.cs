using System.Collections.Generic;
using UnityEngine;

namespace CampusAR
{
    // Formas básicas sin colisionador (el módulo de física no va en la build, y CreatePrimitive falla con errores).
    public static class MallasBasicas
    {
        static Mesh cubo, esfera, cilindro;

        public static GameObject Crear(PrimitiveType tipo, string nombre = "Forma")
        {
            var g = new GameObject(nombre);
            g.AddComponent<MeshFilter>().sharedMesh = Malla(tipo);
            g.AddComponent<MeshRenderer>();
            return g;
        }

        public static Mesh Malla(PrimitiveType tipo)
        {
            switch (tipo)
            {
                case PrimitiveType.Cube: return cubo != null ? cubo : (cubo = Cubo());
                case PrimitiveType.Cylinder: return cilindro != null ? cilindro : (cilindro = Cilindro(16));
                default: return esfera != null ? esfera : (esfera = Esfera(14, 10));
            }
        }

        static Mesh Cubo()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3[] n = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var nn in n)
            {
                Vector3 a = Vector3.Cross(nn, Mathf.Abs(nn.y) > 0.9f ? Vector3.right : Vector3.up).normalized, b = Vector3.Cross(nn, a);
                int i0 = v.Count;
                v.Add((nn - a - b) * 0.5f); v.Add((nn + a - b) * 0.5f); v.Add((nn + a + b) * 0.5f); v.Add((nn - a + b) * 0.5f);
                t.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
            }
            var m = new Mesh { name = "CuboBasico" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Mesh Esfera(int lon, int lat)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int y = 0; y <= lat; y++)
                for (int x = 0; x <= lon; x++)
                {
                    float th = Mathf.PI * y / lat, ph = 2f * Mathf.PI * x / lon;
                    v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f);
                }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x, b = a + lon + 1;
                    t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            var m = new Mesh { name = "EsferaBasica" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Mesh Cilindro(int seg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = 2f * Mathf.PI * i / seg, a1 = 2f * Mathf.PI * (i + 1) / seg;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 0.5f, p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 0.5f;
                Vector3 up = Vector3.up * 0.5f;
                int k = v.Count;
                v.Add(p0 - up); v.Add(p1 - up); v.Add(p1 + up); v.Add(p0 + up);              // lado
                t.AddRange(new[] { k, k + 3, k + 2, k, k + 2, k + 1 });
                k = v.Count; v.Add(up); v.Add(p0 + up); v.Add(p1 + up);                         // tapa superior
                t.AddRange(new[] { k, k + 2, k + 1 });
                k = v.Count; v.Add(-up); v.Add(p0 - up); v.Add(p1 - up);                        // tapa inferior
                t.AddRange(new[] { k, k + 1, k + 2 });
            }
            var m = new Mesh { name = "CilindroBasico" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}
