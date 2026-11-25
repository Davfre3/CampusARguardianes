using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Vuforia;

namespace CampusAR
{
    // Registra los 4 Image Targets en tiempo de ejecución desde las imágenes del catálogo, recibe los
    // cambios de seguimiento de Vuforia y activa SOLO la mascota de la estación activa.
    // Detectar una imagen no completa la estación: el sello solo lo concede QuizController.
    public class ARStationController : MonoBehaviour
    {
        class Entrada
        {
            public StationDefinition def;
            public ImageTargetBehaviour target;
            public GameObject contenido;
            public bool tracked;          // true si cualquiera de los dos disparadores está activo
            public bool imgTracked, modelTracked;
            public ModelTargetBehaviour modelo;
            public Action<ObserverBehaviour, TargetStatus> handlerModelo;
            public Action<ObserverBehaviour, TargetStatus> handler;
        }

        public AppFlowController flow;
        public StationCatalog catalogo;
        public Transform targetsRoot;

        [Header("Interfaz del escáner")]
        public TMP_Text estado;
        public GameObject guiaEscaneo, tarjetaEstacion, panelNoReconoce, panelPermiso;
        public TMP_Text tarjetaTitulo;
        [Header("Acciones de la mascota")]
        public GameObject panelAcciones, burbuja;
        public TMP_Text burbujaTexto, etiquetaGirar, etiquetaCaminar;
        public float segundosParaAyuda = 10f;
        public float retardoMenu = 0.15f;   // el menú aparece directamente tras escanear la placa

        [Header("Mascota virtual")]
        public float alturaMascota = 0.20f;
        public Vector3 desplazamientoLocal = Vector3.zero;

        readonly Dictionary<string, Entrada> entradas = new Dictionary<string, Entrada>();
        Entrada actual;
        bool creados;
        float tBusqueda;
        string errorInicio;

        void Awake()
        {
            if (catalogo == null) catalogo = Resources.Load<StationCatalog>("CatalogoEstaciones");
        }

        void OnEnable()
        {
            var app = VuforiaApplication.Instance;
            app.OnVuforiaInitialized += AlInicializar;
            app.OnVuforiaStarted += CrearTargets;
            if (flow != null) { flow.PanelCambiado += Reevaluar; flow.PanelCambiado += AlVolverAlEscaner; }
            if (app.IsRunning) CrearTargets();
        }

        void OnDisable()
        {
            var app = VuforiaApplication.Instance;
            if (app != null) { app.OnVuforiaInitialized -= AlInicializar; app.OnVuforiaStarted -= CrearTargets; }
            if (flow != null) { flow.PanelCambiado -= Reevaluar; flow.PanelCambiado -= AlVolverAlEscaner; }
            foreach (var e in entradas.Values)
            {
                if (e.target != null && e.handler != null) e.target.OnTargetStatusChanged -= e.handler;
                if (e.modelo != null && e.handlerModelo != null) e.modelo.OnTargetStatusChanged -= e.handlerModelo;
            }
        }

        void Start()
        {
            Mostrar(false);
            Estado("Buscando… apunta la cámara a la imagen de la placa.");
        }

        void Update()
        {
            // RF16: si tras unos segundos no se reconoce ninguna placa, ofrecer ayuda y una salida clara.
            if (panelNoReconoce == null || flow == null) return;
            bool buscando = actual == null && !AlgunoTracked() && flow.EnRaiz && errorInicio == null;
            if (buscando) { tBusqueda += Time.unscaledDeltaTime; if (tBusqueda > segundosParaAyuda) panelNoReconoce.SetActive(true); }
            else { tBusqueda = 0f; if (actual != null || !flow.EnRaiz) panelNoReconoce.SetActive(false); }
        }

        // ---------- inicialización ----------
        void AlInicializar(VuforiaInitError error)
        {
            if (error == VuforiaInitError.NONE) return;
            errorInicio = error.ToString();
            Debug.LogWarning("[CampusAR] Vuforia no inició: " + error);
            if (error == VuforiaInitError.PERMISSION_ERROR && panelPermiso != null) panelPermiso.SetActive(true);
            else { if (panelNoReconoce != null) panelNoReconoce.SetActive(true); Estado("No se pudo iniciar la cámara AR (" + error + "). Usa «Información sin cámara».");  }
        }

        void CrearTargets()
        {
            if (creados || catalogo == null) return;
            creados = true;
            foreach (var def in catalogo.estaciones)
            {
                if (def == null) continue;
                ImageTargetBehaviour tgt = null;

                // 1) Base de datos del Target Manager (StreamingAssets/Vuforia)
                if (!string.IsNullOrEmpty(def.databaseTargetName))
                {
                    try { tgt = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(def.databasePath, def.databaseTargetName); }
                    catch (Exception ex) { Debug.LogWarning("[CampusAR] Base de datos no disponible para " + def.targetName + ": " + ex.Message); }
                    // La base puede traer otro tamaño: se fuerza el ancho físico real de la imagen impresa (0,14 m).
                    if (tgt != null) tgt.SetWidth(def.markerWidthMeters);
                }
                // 2) Respaldo: imagen incluida en el proyecto
                if (tgt == null && def.markerImage != null)
                    tgt = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(def.markerImage, def.markerWidthMeters, def.targetName);
                if (tgt == null) { Debug.LogError("[CampusAR] No se pudo crear el target " + def.targetName); continue; }
                tgt.gameObject.name = "Target_" + def.targetName;
                if (targetsRoot != null) tgt.transform.SetParent(targetsRoot, true);

                var root = new GameObject("ContentRoot");
                root.transform.SetParent(tgt.transform, false);
                root.transform.localPosition = desplazamientoLocal;
                GameObject mascota = def.mascotPrefab != null ? Instantiate(def.mascotPrefab, root.transform) : FiguraTemporal(def, root.transform);
                mascota.name = "Mascota" + def.mascotaNombre;
                mascota.transform.localPosition = Vector3.zero;
                mascota.AddComponent<MascotaEfectos>().Configurar(def, 0.26f);
                DiagnosticoMascota(def, mascota);
                root.SetActive(false);

                var en = new Entrada { def = def, target = tgt, contenido = root };
                en.handler = (obs, st) => AlCambiarEstado(en, st);
                tgt.OnTargetStatusChanged += en.handler;
                entradas[def.stationId] = en;

                // 3) Disparador adicional por MODELO (Model Target). La placa sigue funcionando como respaldo.
                if (usarModelTargets && !string.IsNullOrEmpty(def.modelTargetName))
                {
                    try
                    {
                        var mt = VuforiaBehaviour.Instance.ObserverFactory.CreateModelTarget(def.modelDatabasePath, def.modelTargetName);
                        if (mt != null)
                        {
                            mt.gameObject.name = "ModelTarget_" + def.targetName;
                            if (targetsRoot != null) mt.transform.SetParent(targetsRoot, true);
                            en.modelo = mt;
                            en.handlerModelo = (obs, st) => AlCambiarEstadoModelo(en, st);
                            mt.OnTargetStatusChanged += en.handlerModelo;
                            int nGuias = 0;
                            try { nGuias = mt.GetNumGuideViews(); } catch { }
                            Debug.Log("[CampusAR] Model Target cargado: " + def.modelTargetName + " · vistas guía: " + nGuias);
                            ForzarGuiaVisible(mt);
                            if (nGuias > 1) StartCoroutine(CiclarVistasGuia(en, mt, nGuias));
                        }
                    }
                    catch (Exception ex) { Debug.LogWarning("[CampusAR] Model Target no disponible para " + def.targetName + ": " + ex.Message); }
                }
            }
            Debug.Log("[CampusAR] Placas cargadas: " + entradas.Count + " de " + catalogo.estaciones.Length);
            if (entradas.Count == 0) Estado("No se cargó ninguna placa. Revisa la base de datos y la licencia.");
            else Estado("Buscando… " + entradas.Count + " placas cargadas. Apunta a la imagen de la placa.");
        }

        // ---------- seguimiento ----------
        void AlCambiarEstado(Entrada e, TargetStatus st)
        {
            e.imgTracked = st.Status == Status.TRACKED || st.Status == Status.EXTENDED_TRACKED;
            Debug.Log("[CampusAR] " + e.def.targetName + " (placa) -> " + st.Status);
            ActualizarEntrada(e);
        }

        void AlCambiarEstadoModelo(Entrada e, TargetStatus st)
        {
            e.modelTracked = st.Status == Status.TRACKED || st.Status == Status.EXTENDED_TRACKED;
            Debug.Log("[CampusAR] " + e.def.targetName + " (modelo) -> " + st.Status + " · info: " + st.StatusInfo);
            ActualizarEntrada(e);
        }

        // Une ambos disparadores: el contenido se cuelga del que esté activo (la placa tiene prioridad).
        void ActualizarEntrada(Entrada e)
        {
            e.tracked = e.imgTracked || e.modelTracked;
            if (e.tracked && e.contenido != null)
            {
                Transform padre = e.imgTracked ? e.target.transform : (e.modelo != null ? e.modelo.transform : e.target.transform);
                if (e.contenido.transform.parent != padre)
                {
                    e.contenido.transform.SetParent(padre, false);
                    e.contenido.transform.localRotation = Quaternion.identity;
                    e.contenido.transform.localPosition = e.imgTracked ? desplazamientoLocal : Vector3.zero;
                }
            }
            Reevaluar();
        }

        bool AlgunoTracked() { foreach (var e in entradas.Values) if (e.tracked) return true; return false; }

        void Reevaluar()
        {
            if (flow == null) return;
            if (actual != null && actual.tracked)
            {
                Activar(actual, true);
                return;
            }

            Entrada candidato = null;
            foreach (var e in entradas.Values) if (e.tracked) { candidato = e; break; }

            if (candidato != null)
            {
                if (flow.EsOcupado && actual != null)
                {
                    flow.MostrarAviso("Se detectó otra estación. Vuelve al escáner para cambiar.");
                    return;
                }
                Activar(candidato, true);
            }
            else if (actual != null)
            {
                // Seguimiento perdido: se espera un instante (evita parpadeos) y luego se oculta la mascota.
                if (perdidaCo != null) StopCoroutine(perdidaCo);
                perdidaCo = StartCoroutine(OcultarSiSigueperdida(actual));
            }
        }

        [Tooltip("Reconocimiento por modelo 3D impreso (tótem). Desactivado: el disparador es la placa.")]
        public bool usarModelTargets = false;

        // ---------- botones de la mascota: Girar / Caminar / Info ----------
        MascotaEfectos FxActual()
        {
            return actual != null && actual.contenido != null ? actual.contenido.GetComponentInChildren<MascotaEfectos>(true) : null;
        }

        void RefrescarEtiquetasAcciones()
        {
            var fx = FxActual();
            if (etiquetaGirar != null) etiquetaGirar.text = fx != null && fx.Girando ? "Parar" : "Girar";
            if (etiquetaCaminar != null) etiquetaCaminar.text = fx != null && fx.Caminando ? "Parar" : "Caminar";
        }

        public void GirarMascota() { var fx = FxActual(); if (fx == null) return; fx.AlternarGiro(); RefrescarEtiquetasAcciones(); }
        public void CaminarMascota() { var fx = FxActual(); if (fx == null) return; fx.AlternarCaminar(); RefrescarEtiquetasAcciones(); }

        Coroutine burbujaCo;
        public void InfoMascota()
        {
            var fx = FxActual();
            if (actual == null || fx == null) return;
            var d = actual.def;
            string texto = !string.IsNullOrWhiteSpace(d.infoCorta) ? d.infoCorta : d.mensajePersonalidad;
            fx.Saltar();
            if (burbuja != null && burbujaTexto != null)
            {
                burbujaTexto.text = texto;
                burbuja.SetActive(true);
                if (burbujaCo != null) StopCoroutine(burbujaCo);
                burbujaCo = StartCoroutine(OcultarBurbuja(Mathf.Clamp(texto.Length * 0.075f, 7f, 20f)));
            }
            VozSintetica.Hablar(texto);
        }

        IEnumerator OcultarBurbuja(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            CerrarBurbuja();
        }

        public void CerrarBurbuja()
        {
            if (burbujaCo != null) { StopCoroutine(burbujaCo); burbujaCo = null; }
            if (burbuja != null) burbuja.SetActive(false);
            VozSintetica.Detener();
        }

        // Orden de prueba de las vistas guía: primero la frontal (índice 1), luego el resto.
        // Vuforia solo reconoce el objeto desde la vista guía ACTIVA; sin esto queda fija la vista 0 (aérea).
        // Pide que se dibuje la silueta (guía 2D) y registra el modo actual. Con reflexión para no depender del nombre exacto del enum.
        static void ForzarGuiaVisible(ModelTargetBehaviour mt)
        {
            try
            {
                var prop = mt.GetType().GetProperty("GuideViewMode");
                if (prop == null) { Debug.Log("[CampusAR] GuideViewMode no disponible en esta versión."); return; }
                Debug.Log("[CampusAR] GuideViewMode antes: " + prop.GetValue(mt));
                if (prop.PropertyType.IsEnum && prop.CanWrite)
                {
                    foreach (var nombre in Enum.GetNames(prop.PropertyType))
                        if (nombre.IndexOf("2D", StringComparison.OrdinalIgnoreCase) >= 0)
                        { prop.SetValue(mt, Enum.Parse(prop.PropertyType, nombre)); break; }
                    Debug.Log("[CampusAR] GuideViewMode ahora: " + prop.GetValue(mt) + " (opciones: " + string.Join(", ", Enum.GetNames(prop.PropertyType)) + ")");
                }
            }
            catch (Exception ex) { Debug.LogWarning("[CampusAR] No se pudo ajustar GuideViewMode: " + ex.Message); }
        }

        static readonly int[] ordenGuias = { 1, 0, 5, 2, 4, 3, 6 };
        IEnumerator CiclarVistasGuia(Entrada e, ModelTargetBehaviour mt, int n)
        {
            int k = 0;
            while (mt != null)
            {
                int idx = ordenGuias[k % ordenGuias.Length];
                if (idx < n)
                {
                    try { mt.SetActiveGuideViewIndex(idx); Debug.Log("[CampusAR] Vista guía activa: " + idx); } catch (Exception ex) { Debug.LogWarning("[CampusAR] No se pudo cambiar la vista guía: " + ex.Message); }
                    float t = 0f;
                    while (t < 12f) { if (e.modelTracked) break; t += Time.unscaledDeltaTime; yield return null; }
                    while (e.modelTracked) yield return null;   // si lo reconoce, se queda en esa vista
                }
                k++;
                yield return null;
            }
        }

        static bool pistaMostrada;
        bool habiaMenu;

        // El menú se abre al detectar la placa. La mascota solo se ve al cerrarlo («Ver la mascota en AR»):
        // entonces reaparece con su animación y, la primera vez, se explican los gestos.
        void AlVolverAlEscaner()
        {
            if (flow == null) return;
            if (!flow.EnRaiz) { habiaMenu = true; return; }
            if (!habiaMenu || actual == null || !actual.tracked) return;
            habiaMenu = false;
            var fx = actual.contenido.GetComponentInChildren<MascotaEfectos>(true);
            if (fx != null) fx.Reiniciar();
            if (!pistaMostrada) { pistaMostrada = true; flow.MostrarAviso("Arrastra para girar · Pellizca para agrandar · Toca para que salude", 5f); }
        }

        Coroutine perdidaCo;
        IEnumerator OcultarSiSigueperdida(Entrada e)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            perdidaCo = null;
            if (e == null || e.tracked || actual != e) yield break;
            {
                e.contenido.SetActive(false);
                if (panelAcciones != null) panelAcciones.SetActive(false);
                CerrarBurbuja();
                Estado("Seguimiento perdido. Vuelve a enfocar la imagen de " + actual.def.displayName + ".");
                if (guiaEscaneo != null) guiaEscaneo.SetActive(true);
            }
        }

        // La placa de Vuforia lleva una escala propia (ancho de la imagen). Se compensa para que la mascota
        // mida de verdad ~0,20 m en el mundo (si no, queda de milímetros y no se ve).
        void AjustarEscala(Entrada e)
        {
            if (e == null || e.target == null) return;
            float ls = e.target.transform.lossyScale.x;
            if (ls > 1e-6f) e.contenido.transform.localScale = Vector3.one / ls;
            Debug.Log("[CampusAR] Escala de la placa " + e.def.targetName + ": " + ls.ToString("F4") + " -> compensación " + e.contenido.transform.localScale.x.ToString("F2"));
        }

        // Registro para revisar por qué una mascota no se ve (mallas, tamaño, materiales).
        void DiagnosticoMascota(StationDefinition def, GameObject m)
        {
            var rs = m.GetComponentsInChildren<Renderer>(true);
            string mats = "";
            foreach (var r in rs) foreach (var mt in r.sharedMaterials) mats += (mt != null && mt.shader != null ? mt.shader.name : "NULL") + "; ";
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log("[CampusAR] Mascota " + def.mascotaNombre + ": mallas=" + rs.Length + " tamaño=" + b.size.ToString("F3") + " m, materiales=" + mats);
        }

        void Activar(Entrada e, bool visible)
        {
            bool cambio = actual != e;
            bool recuperado = !cambio && !e.contenido.activeSelf;
            if (cambio)
            {
                if (actual != null) { var fx0 = FxActual(); if (fx0 != null) fx0.DetenerModos(); actual.contenido.SetActive(false); CerrarBurbuja(); }   // RF04: una sola mascota
                actual = e;
                flow.EstacionActual = e.def;
                if (flow.EnRaiz) StartCoroutine(AbrirMenuTras(e));
                tarjetaTitulo.text = e.def.displayName + " · " + e.def.mascotaNombre;
            }
            if (visible) AjustarEscala(e);
            if (cambio) RefrescarEtiquetasAcciones();
            e.contenido.SetActive(visible);
            if (visible) Debug.Log("[CampusAR] Mostrando mascota " + e.def.mascotaNombre + " (activa=" + e.contenido.activeInHierarchy + ")");
            Mostrar(true);
            if (guiaEscaneo != null) guiaEscaneo.SetActive(false);
            if (panelNoReconoce != null) panelNoReconoce.SetActive(false);
            Estado(recuperado ? "Seguimiento recuperado: " + e.def.displayName : "Detectado: " + e.def.displayName);
        }

        // El menú se abre apenas se detecta la placa (con un instante mínimo para que se asiente el seguimiento).
        IEnumerator AbrirMenuTras(Entrada e)
        {
            yield return new WaitForSecondsRealtime(0.15f);   // fijo: no depende del valor guardado en la escena
            if (actual == e && flow != null && flow.EnRaiz) flow.AbrirMenuEstacion();
        }

        void Mostrar(bool detectada)
        {
            if (tarjetaEstacion != null) tarjetaEstacion.SetActive(detectada);
            if (panelAcciones != null) panelAcciones.SetActive(detectada);
            if (!detectada) CerrarBurbuja();
            if (guiaEscaneo != null && !detectada) guiaEscaneo.SetActive(true);
        }

        void Estado(string t) { if (estado != null) estado.text = t; }

        // ---------- figura temporal (desarrollo) ----------
        GameObject FiguraTemporal(StationDefinition def, Transform padre)
        {
            var g = new GameObject("FiguraTemporal");
            g.transform.SetParent(padre, false);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = def.colorFacultad;
            float h = alturaMascota;
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.transform.SetParent(g.transform, false);
            cuerpo.transform.localScale = new Vector3(h * 0.5f, h * 0.3f, h * 0.5f);
            cuerpo.transform.localPosition = new Vector3(0, h * 0.3f, 0);
            var cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cabeza.transform.SetParent(g.transform, false);
            cabeza.transform.localScale = Vector3.one * h * 0.35f;
            cabeza.transform.localPosition = new Vector3(0, h * 0.82f, 0);
            foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            return g;
        }
    }
}
