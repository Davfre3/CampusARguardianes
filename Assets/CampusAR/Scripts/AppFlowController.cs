using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CampusAR
{
    // Navegación entre pantallas (paneles) de una escena y entre las escenas Inicio y ExploracionAR.
    // Un solo gestor por escena; los botones lo invocan con UnityEvents persistentes.
    public class AppFlowController : MonoBehaviour
    {
        public const string PrefAyudaVista = "campusar.ayuda_vista";

        [Header("Escenas")]
        public string escenaInicio = "Inicio";
        public string escenaAR = "ExploracionAR";

        [Header("Pantallas (solo una activa a la vez)")]
        public GameObject panelRaiz;
        public GameObject panelInfoHub;
        public TMP_Text hubTitulo, hubSubtitulo;
        public Image hubIlustracion;
        public GameObject panelAyuda, panelFacultades, panelInformacion, panelReto, panelRecorrido, panelInsignia, panelConfirmar;
        public GameObject panelMenu;                 // menú a pantalla completa de la estación detectada
        public TMP_Text menuTitulo, menuSubtitulo;
        public Image menuAcento;
        public Image menuIlustracion;
        public float duracionTransicion = 0.30f;

        [Header("Controladores")]
        public InformationPanelController info;
        public QuizController quiz;
        public ProgressPanelController progreso;
        public TMP_Text aviso;
        public StationCatalog catalogo;

        public StationDefinition EstacionActual { get; set; }
        public event Action PanelCambiado;

        GameObject actual;
        readonly Stack<GameObject> historial = new Stack<GameObject>();
        List<GameObject> todos = new List<GameObject>();
        Coroutine avisoCo, animCo;

        public GameObject PanelActual { get { return actual; } }
        public bool EnRaiz { get { return actual == panelRaiz; } }
        // RF15: hay una pregunta o una lectura en curso.
        public bool EsOcupado { get { return actual == panelInformacion || actual == panelInfoHub || actual == panelReto || actual == panelMenu; } }

        void Awake()
        {
            if (catalogo == null) catalogo = Resources.Load<StationCatalog>("CatalogoEstaciones");
            todos.Clear();
            foreach (var p in new[] { panelRaiz, panelAyuda, panelFacultades, panelInfoHub, panelInformacion, panelReto, panelRecorrido, panelInsignia, panelConfirmar, panelMenu })
                if (p != null) todos.Add(p);
            if (info != null) info.flow = this;
            if (quiz != null) quiz.flow = this;
            if (progreso != null) progreso.flow = this;
        }

        void Start()
        {
            Activar(panelRaiz, true, false);
            if (aviso != null) aviso.gameObject.SetActive(false);
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) Atras();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) Atras();
#endif
        }

        // ---------- navegación básica ----------
        public void Mostrar(GameObject destino)
        {
            if (destino == null || destino == actual) return;
            if (actual != null) historial.Push(actual);
            Activar(destino, true, true);
        }

        public void Atras()
        {
            if (historial.Count > 0) { Activar(historial.Pop(), false, true); return; }
            // Sin historial: se regresa a la pantalla raíz (o al inicio de la app si ya se está en ella en AR).
            if (actual != panelRaiz && panelRaiz != null) { Activar(panelRaiz, false, true); return; }
            if (SceneManager.GetActiveScene().name == escenaAR) IrAInicio();
        }

        void Activar(GameObject destino, bool adelante = true, bool animar = true)
        {
            TemaUI.Aplicar(destino, EstacionActual);
            foreach (var p in todos) p.SetActive(p == destino);
            actual = destino;
            if (destino == panelRaiz) historial.Clear();
            PanelCambiado?.Invoke();
            if (animar && destino != null && duracionTransicion > 0f)
            {
                if (animCo != null) StopCoroutine(animCo);
                animCo = StartCoroutine(Entrada(destino, adelante));
            }
        }

        // Transición suave: aparece con fundido y un deslizamiento corto (hacia arriba al avanzar,
        // desde la izquierda al volver). Usa tiempo no escalado para no depender de Time.timeScale.
        IEnumerator Entrada(GameObject g, bool adelante)
        {
            var cg = g.GetComponent<CanvasGroup>();
            if (cg == null) cg = g.AddComponent<CanvasGroup>();
            var rt = (RectTransform)g.transform;
            Vector2 inicio = adelante ? new Vector2(0f, -120f) : new Vector2(-110f, 0f);
            float t = 0f;
            cg.alpha = 0f; rt.anchoredPosition = inicio;
            while (t < duracionTransicion)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duracionTransicion);
                k = 1f - Mathf.Pow(1f - k, 3f);               // ease-out cúbico
                cg.alpha = k;
                rt.anchoredPosition = Vector2.LerpUnclamped(inicio, Vector2.zero, k);
                yield return null;
            }
            cg.alpha = 1f; rt.anchoredPosition = Vector2.zero;
        }

        public void IrARaiz() { Activar(panelRaiz, false, true); }

        public void IrAInicio() { SceneManager.LoadScene(escenaInicio); }
        public void CargarAR() { SceneManager.LoadScene(escenaAR); }

        // Primera vez: se explica cómo usar la cámara antes de pedir el permiso (RF02).
        public void IrAExploracion()
        {
            if (PlayerPrefs.GetInt(PrefAyudaVista, 0) == 0) { Mostrar(panelAyuda); return; }
            CargarAR();
        }

        public void AyudaEntendidaYAbrirCamara()
        {
            PlayerPrefs.SetInt(PrefAyudaVista, 1);
            PlayerPrefs.Save();
            CargarAR();
        }

        public void AbrirAyuda() { Mostrar(panelAyuda); }
        public void AbrirFacultades() { Mostrar(panelFacultades); }

        // ---------- información ----------
        public void AbrirInfoDeIndice(int indice)
        {
            if (catalogo == null || indice < 0 || indice >= catalogo.estaciones.Length) return;
            EstacionActual = catalogo.estaciones[indice];
            AbrirInformacion();
        }

        // Pantalla con los 3 botones (Facultad, Carreras, Atención); cada uno abre su propia pantalla con la información en grande.
        public void AbrirInformacion()
        {
            if (EstacionActual == null || panelInfoHub == null) { MostrarAviso("Primero elige una estación."); return; }
            if (hubTitulo != null) hubTitulo.text = "INFORMACIÓN";
            if (hubSubtitulo != null) hubSubtitulo.text = EstacionActual.displayName + "  ·  " + EstacionActual.mascotaNombre;
            if (hubIlustracion != null) { hubIlustracion.sprite = EstacionActual.ilustracion; hubIlustracion.enabled = EstacionActual.ilustracion != null; }
            Mostrar(panelInfoHub);
        }

        public void AbrirFacultad() { AbrirInfo(Seccion.Facultad); }
        public void AbrirCarreras() { AbrirInfo(Seccion.Carreras); }
        public void AbrirMapa() { AbrirInfo(Seccion.Mapa); }
        public void AbrirAtencion() { AbrirInfo(Seccion.Atencion); }

        void AbrirInfo(Seccion s)
        {
            if (EstacionActual == null) { MostrarAviso("Primero elige una estación."); return; }
            info.Mostrar(EstacionActual, s);
            Mostrar(panelInformacion);
        }

        // ---------- menú de la estación (pantalla completa) ----------
        public void AbrirMenuEstacion()
        {
            if (EstacionActual == null || panelMenu == null) return;
            menuTitulo.text = EstacionActual.displayName;
            bool ok = ProgressService.EstaCompletada(EstacionActual.stationId);
            menuSubtitulo.text = "Mascota: " + EstacionActual.mascotaNombre + "   ·   Sello: " + (ok ? "obtenido" : "pendiente") +
                (string.IsNullOrWhiteSpace(EstacionActual.mensajePersonalidad) ? "" : "\n«" + EstacionActual.mensajePersonalidad + "»");
            if (menuAcento != null) menuAcento.color = EstacionActual.colorFacultad;
            if (menuIlustracion != null) { menuIlustracion.sprite = EstacionActual.ilustracion; menuIlustracion.enabled = EstacionActual.ilustracion != null; }
            Mostrar(panelMenu);
        }

        // ---------- reto, recorrido, insignia ----------
        public void AbrirReto()
        {
            if (EstacionActual == null) { MostrarAviso("Primero elige una estación."); return; }
            quiz.Mostrar(EstacionActual);
            Mostrar(panelReto);
        }

        public void AbrirRecorrido() { progreso.Refrescar(); Mostrar(panelRecorrido); }

        public void AbrirInsignia()
        {
            if (!ProgressService.InsigniaDesbloqueada(catalogo)) { MostrarAviso("La insignia se desbloquea con las 4 estaciones."); return; }
            Mostrar(panelInsignia);
        }

        // ---------- reinicio con confirmación (RF13) ----------
        public void PedirReinicio() { Mostrar(panelConfirmar); }
        public void CancelarReinicio() { Atras(); }
        public void ConfirmarReinicio()
        {
            ProgressService.Reiniciar(catalogo);
            // Se vuelve a «Mi recorrido» conservando el historial, para que «Volver» siga llevando al inicio.
            if (historial.Count > 0 && historial.Peek() == panelRecorrido) Activar(historial.Pop(), false, true);
            else Activar(panelRecorrido, false, true);
            progreso.Refrescar();
            MostrarAviso("Recorrido reiniciado.");
        }

        // ---------- enlaces externos (RF07) ----------
        public void AbrirEnlace(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://")) { MostrarAviso("Enlace pendiente de aprobación."); return; }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            { MostrarAviso("Sin conexión: este enlace necesita internet. Tu progreso está a salvo."); return; }
            Application.OpenURL(url);
        }

        public void MostrarAviso(string texto, float segundos = 3.5f)
        {
            if (aviso == null) return;
            if (avisoCo != null) StopCoroutine(avisoCo);
            avisoCo = StartCoroutine(AvisoCo(texto, segundos));
        }

        IEnumerator AvisoCo(string texto, float segundos)
        {
            aviso.text = texto;
            aviso.transform.parent.gameObject.SetActive(true);
            aviso.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(segundos);
            aviso.transform.parent.gameObject.SetActive(false);
        }
    }
}
