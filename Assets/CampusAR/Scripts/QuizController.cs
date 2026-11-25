using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusAR
{
    // Un reto por estación: pregunta, 3 alternativas, confirmación única por intento (RF08, RF09).
    public class QuizController : MonoBehaviour
    {
        [HideInInspector] public AppFlowController flow;
        public TMP_Text titulo, pregunta, retroalimentacion, estadoSello;
        public Button[] opciones;          // 3
        public Button confirmar, reintentar, verRecorrido;
        public Image insigniaImg;          // dibujo de la insignia (gris = pendiente, color = obtenida)

        StationDefinition est;
        int elegida = -1;
        int[] orden = { 0, 1, 2 };   // las opciones se mezclan en cada intento
        bool confirmada;

        public void Mostrar(StationDefinition e)
        {
            est = e;
            titulo.text = "Mini reto — " + e.displayName;
            pregunta.text = e.quiz.pregunta;
            estadoSello.text = ProgressService.EstaCompletada(e.stationId)
                ? "Insignia «" + e.insigniaNombre + "»: OBTENIDA. Puedes repetir el reto sin sellos extra."
                : "Supera este reto corto para obtener la insignia «" + e.insigniaNombre + "» y el sello de esta estación.";
            MostrarInsignia(ProgressService.EstaCompletada(e.stationId), false);
            Reiniciar();
        }

        void MostrarInsignia(bool obtenida, bool animar)
        {
            if (insigniaImg == null) return;
            bool hay = est != null && est.Icono() != null;
            insigniaImg.gameObject.SetActive(hay);
            if (!hay) return;
            insigniaImg.sprite = est.Icono();
            insigniaImg.color = obtenida ? Color.white : new Color(0.22f, 0.22f, 0.26f, 0.9f);
            var pop = insigniaImg.GetComponent<PopIn>();
            if (pop != null) { pop.enabled = false; if (animar) pop.enabled = true; else insigniaImg.transform.localScale = Vector3.one; }
        }

        void Reiniciar()
        {
            elegida = -1; confirmada = false;
            for (int i = orden.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int t = orden[i]; orden[i] = orden[j]; orden[j] = t; }
            retroalimentacion.text = "";
            for (int i = 0; i < opciones.Length; i++)
            {
                opciones[i].gameObject.SetActive(true);
                opciones[i].interactable = true;
                Etiquetar(i, false);
            }
            confirmar.gameObject.SetActive(true);
            confirmar.interactable = false;
            reintentar.gameObject.SetActive(false);
            verRecorrido.gameObject.SetActive(false);
        }

        void Etiquetar(int i, bool marcada)
        {
            var t = opciones[i].GetComponentInChildren<TMP_Text>();
            string letra = ((char)('A' + i)).ToString();
            t.text = (marcada ? "(•) " : "(  ) ") + letra + ". " + est.quiz.opciones[orden[i]];
        }

        public void Elegir(int indice)
        {
            if (confirmada) return;
            elegida = indice;
            for (int i = 0; i < opciones.Length; i++) Etiquetar(i, i == indice);
            confirmar.interactable = true;
        }

        public void Confirmar()
        {
            if (confirmada || elegida < 0) return;   // una sola confirmación por intento
            confirmada = true;
            foreach (var o in opciones) o.interactable = false;
            confirmar.gameObject.SetActive(false);

            if (orden[elegida] == est.quiz.indiceCorrecto)
            {
                bool nuevo = ProgressService.Completar(est.stationId);
                AudioManager.Acierto(); if (nuevo) AudioManager.Insignia();
                var cv = GetComponentInParent<Canvas>();
                if (cv != null) { if (nuevo) StartCoroutine(PopupTras(cv.transform)); else ConfetiUI.Lanzar(cv.transform, est.colorFacultad); }
                MostrarInsignia(true, true);
                retroalimentacion.text = "<b>¡Correcto!</b>\n" + est.quiz.explicacion + "\n\n" +
                    (nuevo ? "Obtuviste la insignia «" + est.insigniaNombre + "» y el sello de " + est.displayName + "."
                           : "Ya tenías esta insignia; el total no cambia.");
                estadoSello.text = "Insignia «" + est.insigniaNombre + "»: OBTENIDA.";
                verRecorrido.gameObject.SetActive(true);
                reintentar.gameObject.SetActive(true);
                reintentar.GetComponentInChildren<TMP_Text>().text = "Repetir el reto";
            }
            else
            {
                AudioManager.Error();
                StartCoroutine(Sacudir(retroalimentacion.transform));
                retroalimentacion.text = "<b>Casi.</b> Revisa la pista y vuelve a intentarlo.\n\n<b>Pista:</b> " + (string.IsNullOrWhiteSpace(est.quiz.pista) ? est.quiz.explicacion : est.quiz.pista) + "\n\nNo se concede sello hasta responder bien; puedes reintentar sin límite.";
                reintentar.gameObject.SetActive(true);
                reintentar.GetComponentInChildren<TMP_Text>().text = "Intentar de nuevo";
            }
        }

        System.Collections.IEnumerator PopupTras(Transform lienzo)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            var cat = flow != null ? flow.catalogo : Resources.Load<StationCatalog>("CatalogoEstaciones");
            InsigniaPopup.Mostrar(lienzo, est, cat != null && ProgressService.InsigniaDesbloqueada(cat));
        }

        System.Collections.IEnumerator Sacudir(Transform t)
        {
            float t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < 0.45f && t != null)
            {
                float u = (Time.unscaledTime - t0) / 0.45f;
                t.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(u * 28f) * 2.5f * (1f - u));
                yield return null;
            }
            if (t != null) t.localRotation = Quaternion.identity;
        }

        public void Reintentar() { if (est != null) Reiniciar(); }
    }
}
