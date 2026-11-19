using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Roles de color que se recolorean según la facultad de la estación activa.
    public enum RolTema { Fondo, FondoOscuro, Acento, Tinta, Sombra }

    public struct Paleta
    {
        public Color pri, osc, tinta;
    }

    public static class TemaUI
    {
        public static Paleta Actual = Neutra;

        public static Paleta Neutra
        {
            get { return new Paleta { pri = new Color(0.16f, 0.36f, 0.82f), osc = new Color(0.06f, 0.09f, 0.16f), tinta = Color.white }; }
        }

        public static Paleta De(StationDefinition d)
        {
            if (d == null) return Neutra;
            Color c = d.colorFacultad; c.a = 1f;
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            return new Paleta
            {
                pri = c,
                osc = new Color(c.r * 0.42f, c.g * 0.42f, c.b * 0.42f, 1f),
                tinta = lum > 0.55f ? new Color(0.05f, 0.05f, 0.05f) : Color.white
            };
        }

        // Aplica la paleta de la facultad a todos los elementos marcados dentro de 'raiz'.
        public static void Aplicar(GameObject raiz, StationDefinition d)
        {
            Actual = De(d);
            if (raiz == null) return;
            foreach (var e in raiz.GetComponentsInChildren<TemaElemento>(true)) e.Aplicar(Actual);
        }
    }
}
