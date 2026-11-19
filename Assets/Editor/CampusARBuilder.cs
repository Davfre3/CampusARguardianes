#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú: Campus AR > Construir escena (UI + GameManager)
// Crea el Canvas, paneles, botones, pasaporte, EventSystem y el GameManager ya conectado.
// Los textos son de EJEMPLO: reemplázalos con la ficha de contenido de Stiven en el Inspector del GameManager.
public static class CampusARBuilder
{
    static readonly Color Fondo = new Color(0.05f, 0.07f, 0.12f, 0.88f);
    static readonly Color Boton = new Color(0.16f, 0.38f, 0.85f, 1f);
    static readonly Color Cerrar = new Color(0.45f, 0.45f, 0.5f, 1f);

    [MenuItem("Campus AR/Construir escena (UI + GameManager)")]
    public static void Construir()
    {
        if (TMP_Settings.instance == null)
        {
            Debug.LogError("Falta importar TMP Essentials: Window > TextMeshPro > Import TMP Essential Resources.");
            return;
        }
        if (Object.FindAnyObjectByType<ARGameManager>() != null)
        {
            Debug.LogWarning("La escena ya tiene un ARGameManager. No se construyó nada.");
            return;
        }

        // ---------- EventSystem ----------
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // ---------- Canvas ----------
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // ---------- GameManager ----------
        var mgrGo = new GameObject("GameManager");
        var mgr = mgrGo.AddComponent<ARGameManager>();

        // ---------- Textos superiores ----------
        mgr.hintText = Texto("HintText", root, "Apunta a una placa", 44, Color.white);
        Anclar(mgr.hintText.rectTransform, 0, 1, 1, 1, 40, -200, -40, -80);
        mgr.tituloText = Texto("TituloText", root, "Campus AR Guardians", 52, Color.white);
        Anclar(mgr.tituloText.rectTransform, 0, 1, 1, 1, 40, -340, -40, -210);
        mgr.tituloText.fontStyle = FontStyles.Bold;

        // ---------- Menú ----------
        var menu = Rect("PanelMenu", root);
        Anclar(menu, 0, 0, 1, 0, 40, 360, -40, 500);
        var hl = menu.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
        hl.childControlWidth = true; hl.childControlHeight = true;
        var bConoce = Boton_("BtnConoce", menu, "Conoce", Boton, 34);
        var bCarreras = Boton_("BtnCarreras", menu, "Carreras", Boton, 34);
        var bEventos = Boton_("BtnEventos", menu, "Eventos", Boton, 34);
        var bReto = Boton_("BtnReto", menu, "Reto", new Color(0.9f, 0.55f, 0.1f), 34);
        mgr.panelMenu = menu.gameObject;
        UnityEventTools.AddPersistentListener(bConoce.onClick, new UnityAction(mgr.BtnConoce));
        UnityEventTools.AddPersistentListener(bCarreras.onClick, new UnityAction(mgr.BtnCarreras));
        UnityEventTools.AddPersistentListener(bEventos.onClick, new UnityAction(mgr.BtnEventos));
        UnityEventTools.AddPersistentListener(bReto.onClick, new UnityAction(mgr.BtnReto));

        // ---------- Panel detalle ----------
        var det = Rect("PanelDetalle", root);
        Anclar(det, 0.05f, 0.30f, 0.95f, 0.72f, 0, 0, 0, 0);
        Imagen("Fondo", det, Fondo, true);
        mgr.detalleText = Texto("DetalleText", det, "", 38, Color.white);
        mgr.detalleText.alignment = TextAlignmentOptions.TopLeft;
        Anclar(mgr.detalleText.rectTransform, 0, 0, 1, 1, 40, 150, -40, -40);
        var bCerrarDet = Boton_("BtnCerrar", det, "Cerrar", Cerrar, 36);
        Anclar((RectTransform)bCerrarDet.transform, 0.5f, 0, 0.5f, 0, -150, 30, 150, 120);
        UnityEventTools.AddPersistentListener(bCerrarDet.onClick, new UnityAction(mgr.BtnCerrarDetalle));
        mgr.panelDetalle = det.gameObject;

        // ---------- Panel reto ----------
        var reto = Rect("PanelReto", root);
        Anclar(reto, 0.05f, 0.28f, 0.95f, 0.74f, 0, 0, 0, 0);
        Imagen("Fondo", reto, Fondo, true);
        mgr.preguntaText = Texto("PreguntaText", reto, "", 42, Color.white);
        mgr.preguntaText.fontStyle = FontStyles.Bold;
        Anclar(mgr.preguntaText.rectTransform, 0, 1, 1, 1, 40, -250, -40, -30);
        var respuestas = Rect("Respuestas", reto);
        Anclar(respuestas, 0, 0, 1, 1, 40, 260, -40, -270);
        var vl = respuestas.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 16; vl.childForceExpandWidth = true; vl.childForceExpandHeight = true;
        vl.childControlWidth = true; vl.childControlHeight = true;
        mgr.botonesRespuesta = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var b = Boton_("BtnRespuesta" + (i + 1), respuestas, "Opción " + (i + 1), Boton, 36);
            mgr.botonesRespuesta[i] = b;
            UnityEventTools.AddIntPersistentListener(b.onClick, new UnityAction<int>(mgr.BtnRespuesta), i);
        }
        mgr.feedbackText = Texto("FeedbackText", reto, "", 36, new Color(1f, 0.95f, 0.5f));
        Anclar(mgr.feedbackText.rectTransform, 0, 0, 1, 0, 40, 130, -40, 250);
        var bCerrarReto = Boton_("BtnCerrar", reto, "Cerrar", Cerrar, 36);
        Anclar((RectTransform)bCerrarReto.transform, 0.5f, 0, 0.5f, 0, -150, 25, 150, 110);
        UnityEventTools.AddPersistentListener(bCerrarReto.onClick, new UnityAction(mgr.BtnCerrarReto));
        mgr.panelReto = reto.gameObject;

        // ---------- Pasaporte ----------
        var pas = Rect("Pasaporte", root);
        Anclar(pas, 0, 0, 1, 0, 0, 0, 0, 330);
        Imagen("Fondo", pas, new Color(0.03f, 0.04f, 0.08f, 0.92f), false);
        var pasTitulo = Texto("PasaporteTitulo", pas, "Pasaporte", 36, Color.white);
        pasTitulo.alignment = TextAlignmentOptions.Left;
        Anclar(pasTitulo.rectTransform, 0, 1, 0.5f, 1, 40, -70, 0, -10);

        mgr.coloresSello = new[]
        {
            new Color(0.20f, 0.50f, 1.00f), // azul   - Administración (Lobo)
            new Color(0.20f, 0.80f, 0.40f), // verde  - Ingeniería (Vikingo)
            new Color(0.90f, 0.25f, 0.25f), // rojo   - Economía (Toro)
            new Color(1.00f, 0.85f, 0.20f), // amarillo - Ciencias (León)
        };
        string[] mascotas = { "Lobo", "Vikingo", "Toro", "León" };
        var fila = Rect("Sellos", pas);
        Anclar(fila, 0, 0, 1, 1, 40, 110, -40, -80);
        var hs = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
        hs.spacing = 24; hs.childAlignment = TextAnchor.MiddleCenter;
        hs.childForceExpandWidth = true; hs.childForceExpandHeight = true;
        hs.childControlWidth = true; hs.childControlHeight = true;
        mgr.sellos = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var s = Imagen("Sello" + (i + 1), fila, mgr.colorSelloVacio, false);
            var le = s.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 110;
            var t = Texto("Nombre", s.rectTransform, mascotas[i], 30, Color.white);
            t.fontStyle = FontStyles.Bold;
            Estirar(t.rectTransform);
            mgr.sellos[i] = s;
        }

        // Barra 0/4
        var sliderRt = Rect("BarraProgreso", pas);
        Anclar(sliderRt, 0, 0, 1, 0, 40, 30, -220, 70);
        var slider = sliderRt.gameObject.AddComponent<Slider>();
        var bg = Imagen("Fondo", sliderRt, new Color(1, 1, 1, 0.18f), false);
        Estirar(bg.rectTransform);
        var fillArea = Rect("Fill Area", sliderRt);
        Estirar(fillArea);
        var fill = Imagen("Fill", fillArea, new Color(0.2f, 0.8f, 0.4f), false);
        Estirar(fill.rectTransform);
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = null;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0; slider.maxValue = 4; slider.wholeNumbers = true;
        slider.interactable = false;
        slider.value = 0;
        mgr.barraProgreso = slider;

        mgr.progresoText = Texto("ProgresoText", pas, "0/4", 40, Color.white);
        mgr.progresoText.alignment = TextAlignmentOptions.Right;
        Anclar(mgr.progresoText.rectTransform, 1, 0, 1, 0, -200, 20, -40, 80);

        // ---------- Insignia final ----------
        var ins = Rect("PanelInsignia", root);
        Estirar(ins);
        Imagen("Fondo", ins, new Color(0.02f, 0.03f, 0.08f, 0.95f), true);
        var insT = Texto("InsigniaText", ins, "¡Completaste los 4 sellos!\nInsignia Campus AR Guardians", 56, new Color(1f, 0.85f, 0.2f));
        insT.fontStyle = FontStyles.Bold;
        Anclar(insT.rectTransform, 0, 0.5f, 1, 0.5f, 60, -200, -60, 200);
        var bCerrarIns = Boton_("BtnCerrar", ins, "Cerrar", Cerrar, 40);
        Anclar((RectTransform)bCerrarIns.transform, 0.5f, 0.3f, 0.5f, 0.3f, -170, -60, 170, 60);
        UnityEventTools.AddPersistentListener(bCerrarIns.onClick, new UnityAction(mgr.BtnCerrarInsignia));
        mgr.panelInsignia = ins.gameObject;
        ins.gameObject.SetActive(false);

        // ---------- Botón de pruebas ----------
        var bReset = Boton_("BtnReiniciarProgreso", root, "Reiniciar", new Color(0.5f, 0.15f, 0.15f), 26);
        Anclar((RectTransform)bReset.transform, 1, 1, 1, 1, -230, -90, -20, -20);
        UnityEventTools.AddPersistentListener(bReset.onClick, new UnityAction(mgr.BtnReiniciarProgreso));

        // ---------- Contenido de EJEMPLO por facultad ----------
        string[] ids = { "administracion", "ingenieria", "economia", "ciencias" };
        string[] nombres = { "Facultad de Administración", "Facultad de Ingeniería", "Facultad de Economía", "Facultad de Ciencias" };
        string[][] opciones =
        {
            new[] { "Lobo", "Vikingo", "León" },
            new[] { "Vikingo", "Lobo", "Toro" },
            new[] { "Toro", "León", "Lobo" },
            new[] { "León", "Toro", "Vikingo" },
        };
        mgr.facultades = new FacultyInfo[4];
        for (int i = 0; i < 4; i++)
        {
            mgr.facultades[i] = new FacultyInfo
            {
                id = ids[i],
                nombreFacultad = nombres[i],
                mascota = mascotas[i],
                descripcion = "[EJEMPLO] Descripción de la " + nombres[i] + ". Reemplazar con la ficha de contenido de Stiven.",
                carreras = "[EJEMPLO] Carreras de la " + nombres[i] + ". Reemplazar con la ficha de contenido.",
                eventos = "[EJEMPLO] Eventos y oportunidades de la " + nombres[i] + ". Reemplazar con la ficha de contenido.",
                pregunta = "[EJEMPLO] ¿Qué mascota representa a la " + nombres[i] + "?",
                opciones = opciones[i],
                respuestaCorrecta = 0,
            };
        }

        // ---------- Cámara AR (Vuforia) ----------
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<Vuforia.VuforiaBehaviour>() == null)
            cam.gameObject.AddComponent<Vuforia.VuforiaBehaviour>();

        EditorUtility.SetDirty(mgr);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = mgrGo;
        Debug.Log("Campus AR: escena construida y guardada. Falta: licencia de Vuforia, Image Target (Vikingo) y asignar ARTargetHandler.");
    }

    // ---------- Helpers ----------
    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Anclar(RectTransform rt, float minX, float minY, float maxX, float maxY,
        float offMinX, float offMinY, float offMaxX, float offMaxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = new Vector2(offMinX, offMinY);
        rt.offsetMax = new Vector2(offMaxX, offMaxY);
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image Imagen(string name, Transform parent, Color color, bool estirar)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (estirar) Estirar(rt);
        return img;
    }

    static TMP_Text Texto(string name, Transform parent, string texto, float size, Color color)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Button Boton_(string name, Transform parent, string label, Color color, float fontSize)
    {
        var img = Imagen(name, parent, color, false);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = Texto("Texto", img.rectTransform, label, fontSize, Color.white);
        Estirar(t.rectTransform);
        return btn;
    }
}
#endif
