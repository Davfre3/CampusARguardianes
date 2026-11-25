using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Pantalla final: las 4 insignias aparecen una tras otra con rebote, con confeti y fanfarria.
    public class InsigniaFinal : MonoBehaviour
    {
        public Image[] iconos;
        public StationCatalog catalogo;

        void OnEnable()
        {
            if (catalogo == null) catalogo = Resources.Load<StationCatalog>("CatalogoEstaciones");
            for (int i = 0; i < iconos.Length; i++)
            {
                if (iconos[i] == null) continue;
                var e = catalogo != null && i < catalogo.estaciones.Length ? catalogo.estaciones[i] : null;
                iconos[i].sprite = e != null ? e.insigniaIcono : null;
                iconos[i].enabled = iconos[i].sprite != null;
                var pop = iconos[i].GetComponent<PopIn>();
                if (pop != null) { pop.retardo = 0.25f + 0.3f * i; pop.enabled = false; pop.enabled = true; }
            }
            AudioManager.Insignia();
            var cv = GetComponentInParent<Canvas>();
            if (cv != null) ConfetiUI.Lanzar(cv.transform, new Color(0.2f, 0.5f, 1f), 90);
        }
    }
}
