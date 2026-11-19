using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Un solo objeto vacío "GameManager" en la escena con este script.
// Controla: UI, contenido por facultad, reto, sellos (PlayerPrefs), barra 0/4 e insignia final.
public class ARGameManager : MonoBehaviour
{
    public static ARGameManager Instance { get; private set; }

    [Header("Datos (orden: Administración, Ingeniería, Economía, Ciencias)")]
    public FacultyInfo[] facultades = new FacultyInfo[4];

    [Header("Textos principales")]
    public TMP_Text tituloText;      // nombre de la facultad / mascota
    public TMP_Text detalleText;     // descripción, carreras o eventos
    public TMP_Text hintText;        // "Apunta a una placa"

    [Header("Paneles")]
    public GameObject panelMenu;     // botones: Conoce, Carreras, Eventos, Reto
    public GameObject panelDetalle;
    public GameObject panelReto;
    public GameObject panelInsignia;

    [Header("Reto")]
    public TMP_Text preguntaText;
    public Button[] botonesRespuesta = new Button[3];
    public TMP_Text feedbackText;

    [Header("Pasaporte")]
    public Image[] sellos = new Image[4];       // mismo orden que facultades
    public Color[] coloresSello = new Color[4]; // azul, verde, rojo, amarillo
    public Color colorSelloVacio = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Slider barraProgreso;                // min 0, max 4, whole numbers
    public TMP_Text progresoText;               // "0/4"

    private FacultyInfo actual;
    private int indiceActual = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        MostrarSinTarget();
        ActualizarPasaporte();
    }

    // ---------- Detección ----------
    public void OnFacultyDetected(string id)
    {
        int idx = System.Array.FindIndex(facultades, f => f.id == id);
        if (idx < 0) { Debug.LogWarning("Facultad no encontrada: " + id); return; }

        indiceActual = idx;
        actual = facultades[idx];

        if (hintText) hintText.gameObject.SetActive(false);
        if (tituloText) tituloText.text = actual.nombreFacultad + " - " + actual.mascota;
        panelMenu.SetActive(true);
        panelDetalle.SetActive(false);
        panelReto.SetActive(false);
        if (detalleText) detalleText.text = "";
    }

    public void OnFacultyLost(string id)
    {
        // Si el usuario está respondiendo el reto, no se cierra nada.
        if (actual == null || actual.id != id) return;
        if (panelReto.activeSelf) return;
        MostrarSinTarget();
    }

    private void MostrarSinTarget()
    {
        actual = null;
        indiceActual = -1;
        if (hintText) { hintText.gameObject.SetActive(true); hintText.text = "Apunta a una placa"; }
        if (tituloText) tituloText.text = "Campus AR Guardians";
        panelMenu.SetActive(false);
        panelDetalle.SetActive(false);
        panelReto.SetActive(false);
    }

    // ---------- Botones del menú (conéctalos en el OnClick) ----------
    public void BtnConoce()   { MostrarDetalle(actual != null ? actual.descripcion : ""); }
    public void BtnCarreras() { MostrarDetalle(actual != null ? actual.carreras : ""); }
    public void BtnEventos()  { MostrarDetalle(actual != null ? actual.eventos : ""); }

    private void MostrarDetalle(string texto)
    {
        if (actual == null) return;
        panelReto.SetActive(false);
        panelDetalle.SetActive(true);
        detalleText.text = texto;
    }

    public void BtnCerrarDetalle() { panelDetalle.SetActive(false); }

    // ---------- Reto ----------
    public void BtnReto()
    {
        if (actual == null) return;

        panelDetalle.SetActive(false);
        panelReto.SetActive(true);
        feedbackText.text = "";
        preguntaText.text = actual.pregunta;

        bool yaTieneSello = TieneSello(indiceActual);

        for (int i = 0; i < botonesRespuesta.Length; i++)
        {
            bool hayOpcion = i < actual.opciones.Length && !string.IsNullOrEmpty(actual.opciones[i]);
            botonesRespuesta[i].gameObject.SetActive(hayOpcion);
            if (!hayOpcion) continue;

            botonesRespuesta[i].GetComponentInChildren<TMP_Text>().text = actual.opciones[i];
            botonesRespuesta[i].interactable = !yaTieneSello;
        }

        if (yaTieneSello) feedbackText.text = "Ya ganaste este sello";
    }

    // Conecta cada botón de respuesta con BtnRespuesta(0), (1), (2)
    public void BtnRespuesta(int opcion)
    {
        if (actual == null) return;

        if (opcion == actual.respuestaCorrecta)
        {
            feedbackText.text = "¡Correcto! Ganaste el sello";
            GanarSello(indiceActual);
            foreach (var b in botonesRespuesta) b.interactable = false;
        }
        else
        {
            feedbackText.text = "Incorrecto, revisa la información e inténtalo de nuevo";
        }
    }

    public void BtnCerrarReto() { panelReto.SetActive(false); }

    // ---------- Pasaporte (PlayerPrefs) ----------
    private string Clave(int idx) { return "sello_" + facultades[idx].id; }

    private bool TieneSello(int idx) { return PlayerPrefs.GetInt(Clave(idx), 0) == 1; }

    private void GanarSello(int idx)
    {
        PlayerPrefs.SetInt(Clave(idx), 1);
        PlayerPrefs.Save();
        ActualizarPasaporte();
    }

    private int ContarSellos()
    {
        int total = 0;
        for (int i = 0; i < facultades.Length; i++) if (TieneSello(i)) total++;
        return total;
    }

    private void ActualizarPasaporte()
    {
        int total = ContarSellos();

        for (int i = 0; i < sellos.Length && i < facultades.Length; i++)
        {
            if (sellos[i] == null) continue;
            Color c = (i < coloresSello.Length) ? coloresSello[i] : Color.white;
            sellos[i].color = TieneSello(i) ? c : colorSelloVacio;
        }

        if (barraProgreso) barraProgreso.value = total;
        if (progresoText) progresoText.text = total + "/" + facultades.Length;

        if (panelInsignia) panelInsignia.SetActive(total >= facultades.Length);
    }

    public void BtnCerrarInsignia() { if (panelInsignia) panelInsignia.SetActive(false); }

    // Botón oculto/de pruebas: borra el progreso
    public void BtnReiniciarProgreso()
    {
        for (int i = 0; i < facultades.Length; i++) PlayerPrefs.DeleteKey(Clave(i));
        PlayerPrefs.Save();
        ActualizarPasaporte();
    }
}
