using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Ventana que aparece al ganar el reto: la insignia entra con rebote, confeti y fanfarria.
    // Se construye por código, así funciona aunque la escena no se haya regenerado.
    public class InsigniaPopup : MonoBehaviour
    {
        public static void Mostrar(Transform lienzo, StationDefinition def, bool ultima)
        {
            if (lienzo == null || def == null) return;
            var go = new GameObject("PopupInsignia", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            go.transform.SetAsLastSibling();
            go.AddComponent<InsigniaPopup>().Construir(def, ultima);
        }

        void Construir(StationDefinition def, bool ultima)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var fondo = gameObject.AddComponent<Image>();
            Color f = def.colorFacultad * 0.35f; f.a = 0.92f;
            fondo.color = f;

            var tarjeta = Crear("Contenido", rt);
            tarjeta.anchorMin = new Vector2(0.5f, 0.5f); tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
            tarjeta.sizeDelta = new Vector2(900, 1200);

            var titulo = Texto(tarjeta, "¡Insignia obtenida!", 78, new Vector2(0, 470), new Vector2(860, 130), Color.white);
            titulo.fontStyle = FontStyles.Bold;

            var ic = Crear("Icono", tarjeta);
            ic.sizeDelta = new Vector2(640, 640); ic.anchoredPosition = new Vector2(0, 90);
            var img = ic.gameObject.AddComponent<Image>(); img.preserveAspect = true; img.raycastTarget = false;
            if (def.insigniaIcono != null) img.sprite = def.insigniaIcono;
            else { img.sprite = JoystickVirtual.Circulo(); img.color = def.colorFacultad; }
            ic.gameObject.AddComponent<PopIn>().retardo = 0.15f;

            Texto(tarjeta, "«" + def.insigniaNombre + "»", 74, new Vector2(0, -290), new Vector2(860, 110), new Color(1f, 0.85f, 0.3f)).fontStyle = FontStyles.Bold;
            Texto(tarjeta, ultima ? "¡Reuniste las 4! Mira tu insignia final en «Mi recorrido»."
                                  : "Sello de " + def.displayName + " conseguido.", 44, new Vector2(0, -390), new Vector2(820, 150), Color.white);

            var bt = Crear("Continuar", tarjeta);
            bt.sizeDelta = new Vector2(620, 140); bt.anchoredPosition = new Vector2(0, -540);
            var bi = bt.gameObject.AddComponent<Image>(); bi.color = new Color(1f, 0.85f, 0.3f);
            var b = bt.gameObject.AddComponent<Button>(); b.targetGraphic = bi;
            bt.gameObject.AddComponent<PressScale>();
            Texto(bt, "Continuar", 54, Vector2.zero, new Vector2(600, 120), new Color(0.1f, 0.1f, 0.15f)).fontStyle = FontStyles.Bold;
            b.onClick.AddListener(() => Destroy(gameObject));

            AudioManager.Insignia();
            ConfetiUI.Lanzar(rt, def.colorFacultad, 110);
            StartCoroutine(Reconfeti(rt, def.colorFacultad));
        }

        IEnumerator Reconfeti(Transform lienzo, Color c)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            if (lienzo != null) ConfetiUI.Lanzar(lienzo, c, 60);
        }

        static RectTransform Crear(string n, Transform padre)
        {
            var g = new GameObject(n, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            return (RectTransform)g.transform;
        }

        static TMP_Text Texto(Transform padre, string txt, float tam, Vector2 pos, Vector2 size, Color c)
        {
            var r = Crear("Texto", padre);
            r.anchoredPosition = pos; r.sizeDelta = size;
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = txt; t.fontSize = tam; t.color = c; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            return t;
        }
    }
}
