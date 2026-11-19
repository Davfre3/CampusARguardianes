using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    public class TemaElemento : MonoBehaviour
    {
        public RolTema rol;

        void OnEnable() { Aplicar(TemaUI.Actual); }

        public void Aplicar(Paleta p)
        {
            Color c;
            switch (rol)
            {
                case RolTema.Fondo:
                case RolTema.Acento: c = p.pri; break;
                case RolTema.FondoOscuro: c = p.osc; break;
                case RolTema.Tinta: c = p.tinta; break;
                default: c = p.osc; break;
            }
            if (rol == RolTema.Sombra)
            {
                var s = GetComponent<Shadow>();
                if (s != null) s.effectColor = c;
                return;
            }
            var g = GetComponent<Graphic>();
            if (g != null) { c.a = g.color.a; g.color = c; }
        }
    }
}
