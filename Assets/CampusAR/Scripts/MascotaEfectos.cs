using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CampusAR
{
    // Hace que la mascota 3D resalte sobre la placa impresa y sea interactiva:
    //  - aparece con rebote, de frente a la cámara, flota y se balancea;
    //  - arrastrar con un dedo = girarla; pellizcar con dos dedos = agrandar/achicar; tocar = saluda (salto y vuelta);
    //  - aro de color de la facultad que pulsa y nombre flotando de frente a la cámara.
    public class MascotaEfectos : MonoBehaviour
    {
        public static float UltimaGlobal = -99f;   // último momento en que el usuario tocó la mascota
        Transform modelo, aro, etiqueta;
        Vector3 escalaModelo, posModelo, escalaAro;
        Quaternion rotModelo;
        float t0, altura = 0.26f;

        // interacción
        float yawFrente, yawUsuario, zoom = 1f;
        float tUltimaInteraccion = -99f, tHop = -99f;
        bool bloqueado, tocando;
        float tInicioToque, distanciaPrev;
        Vector2 posInicio;
        bool seMovio;

        // modos de los botones
        StationDefinition defActual;
        bool girando, caminando, bailando;
        float yawAuto, wCaminar, wBaile, tBaile;
        Vector2 mov;                       // joystick (x = derecha, y = adelante, respecto a la cámara)
        Vector3 desp;                      // desplazamiento acumulado sobre la placa (m, local)
        float yawMov;                      // hacia dónde mira al caminar
        const float RadioMax = 1.6f, Velocidad = 0.30f;
        Vector3 despPrev, velocidad; Vector3 posAro; bool arrastrando; Vector3 offArrastre; float giroPrevDedos; bool tieneGiroDedos;
        float tAccion = -99f; int tipoAccion; bool impactoHecho; float tTecla;
        Animator anim;                     // si el modelo trae esqueleto (Mixamo), se usan sus clips
        bool tieneAnim;
        public bool Girando { get { return girando; } }
        public bool Caminando { get { return caminando; } }
        public bool Bailando { get { return bailando; } }
        public void SetMovimiento(Vector2 v) { mov = Vector2.ClampMagnitude(v, 1f); if (mov.sqrMagnitude > 0.01f) tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }

        public void AlternarGiro() { girando = !girando; if (girando) { caminando = false; bailando = false; } mov = Vector2.zero; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; AudioManager.Clic(); }
        public void AlternarCaminar() { caminando = !caminando; if (caminando) { girando = false; bailando = false; } mov = Vector2.zero; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; AudioManager.Mascota(); Chispas(10); }
        public void AlternarBaile() { bailando = !bailando; if (bailando) { girando = false; caminando = false; tBaile = 0f; } mov = Vector2.zero; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; AudioManager.Mascota(); Chispas(16); }
        public void VolverAlCentro() { desp = Vector3.zero; }
        public void Saltar() { tHop = Time.unscaledTime; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; AudioManager.Mascota(); Chispas(18); }
        public void Golpear() { tAccion = Time.unscaledTime; tipoAccion = 1; impactoHecho = false; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; if (tieneAnim) anim.SetTrigger("Golpear"); }
        public void Escribir() { tAccion = Time.unscaledTime; tipoAccion = 2; tTecla = 0f; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; if (tieneAnim) anim.SetTrigger("Escribir"); }
        public void DetenerModos() { girando = caminando = bailando = false; mov = Vector2.zero; }

        void Chispas(int n)
        {
            if (defActual == null || modelo == null) return;
            FxChispas.Rafaga(defActual.stationId, modelo.position + Vector3.up * 0.04f, defActual.colorFacultad, n);
        }

        public void Configurar(StationDefinition def, float alturaMascota)
        {
            altura = alturaMascota;
            defActual = def;
            foreach (Transform h in transform)
            {
                if (h.name == "Aro") aro = h;
                else if (modelo == null && h.name != "Etiqueta") modelo = h;
            }
            if (modelo == null) modelo = transform;
            anim = modelo.GetComponentInChildren<Animator>(true);
            tieneAnim = anim != null && anim.runtimeAnimatorController != null;
            escalaModelo = modelo.localScale; posModelo = modelo.localPosition; rotModelo = modelo.localRotation;
            if (aro != null) { escalaAro = aro.localScale; posAro = aro.localPosition; }

            var go = new GameObject("Etiqueta");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, altura * 1.12f, 0);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = def.mascotaNombre.ToUpperInvariant();
            tmp.fontSize = 0.40f;               // en TextMeshPro 3D, el cuerpo del texto = fontSize × 0,1 unidades
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.28f; tmp.outlineColor = (Color32)def.colorFacultad;
            tmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.08f);
            etiqueta = go.transform;
        }

        void OnEnable()
        {
            t0 = Time.unscaledTime;
            yawUsuario = 0f; zoom = 1f; tocando = false; tUltimaInteraccion = -99f;
            girando = caminando = bailando = false; yawAuto = 0f; wCaminar = 0f; wBaile = 0f; desp = Vector3.zero; despPrev = Vector3.zero; velocidad = Vector3.zero; arrastrando = false; mov = Vector2.zero;
            OrientarDeFrente();
            Aplicar(0f);
            if (defActual != null) { Chispas(20); AudioManager.Mascota(); }
        }

        // Vuelve a aparecer con rebote y de frente (por ejemplo, al cerrar el menú para verla).
        public void Reiniciar()
        {
            t0 = Time.unscaledTime;
            yawUsuario = 0f; zoom = 1f; tUltimaInteraccion = -99f;
            girando = caminando = bailando = false; yawAuto = 0f; wCaminar = 0f; wBaile = 0f; desp = Vector3.zero; despPrev = Vector3.zero; velocidad = Vector3.zero; arrastrando = false; mov = Vector2.zero;
            OrientarDeFrente();
            Chispas(20); AudioManager.Mascota();
        }

        // La mascota arranca mirando a la cámara (su frente es -Z local).
        void OrientarDeFrente()
        {
            var cam = Camera.main;
            if (cam == null) { yawFrente = 0f; return; }
            Vector3 d = transform.InverseTransformPoint(cam.transform.position); d.y = 0f;
            yawFrente = d.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(Vector3.back, d.normalized, Vector3.up) : 0f;
        }

        void Update()
        {
            LeerEntrada();
            Aplicar(Time.unscaledTime - t0);
        }

        // ---------------- interacción (toque en el teléfono, ratón en el Editor) ----------------
        void LeerEntrada()
        {
            var ts = Touchscreen.current;
            if (ts != null)
            {
                int activos = 0; TouchControl c0 = null, c1 = null;
                foreach (var tc in ts.touches)
                {
                    if (!tc.press.isPressed) continue;
                    activos++;
                    if (activos == 1) c0 = tc; else if (activos == 2) c1 = tc;
                }
                if (activos >= 1)
                {
                    if (!tocando)
                    {
                        tocando = true; seMovio = false; tInicioToque = Time.unscaledTime;
                        posInicio = c0.position.ReadValue(); distanciaPrev = 0f;
                        bloqueado = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(c0.touchId.ReadValue());
                    }
                    if (!bloqueado)
                    {
                        if (activos >= 2 && c1 != null)
                        {
                            Vector2 p0 = c0.position.ReadValue(), p1 = c1.position.ReadValue();
                            float dist = Vector2.Distance(p0, p1);
                            if (distanciaPrev > 1f) Zoom(dist / distanciaPrev);
                            distanciaPrev = dist; seMovio = true; arrastrando = false;
                            float ang = Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg;
                            if (tieneGiroDedos) yawUsuario -= Mathf.DeltaAngle(giroPrevDedos, ang);
                            giroPrevDedos = ang; tieneGiroDedos = true;
                        }
                        else
                        {
                            if ((c0.position.ReadValue() - posInicio).sqrMagnitude > 400f) seMovio = true;
                            if (seMovio) Girar(c0.delta.ReadValue().x);
                            distanciaPrev = 0f; tieneGiroDedos = false;
                        }
                    }
                    return;
                }
                if (tocando) FinToque();
                return;
            }

            var m = Mouse.current;
            if (m == null) return;
            if (m.leftButton.isPressed)
            {
                if (!tocando)
                {
                    tocando = true; seMovio = false; tInicioToque = Time.unscaledTime; posInicio = m.position.ReadValue();
                    bloqueado = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                }
                if (!bloqueado)
                {
                    if ((m.position.ReadValue() - posInicio).sqrMagnitude > 400f) seMovio = true;
                    if (seMovio) Girar(m.delta.ReadValue().x);
                }
            }
            else if (tocando) FinToque();
            float rueda = m.scroll.ReadValue().y;
            if (Mathf.Abs(rueda) > 0.01f && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                Zoom(1f + Mathf.Sign(rueda) * 0.06f);
        }

        // Arrastrar con un dedo: la mascota sigue al dedo sobre el plano de la placa (como los filtros de Instagram/TikTok).
        void Arrastrar(Vector2 pantalla)
        {
            var cam = Camera.main; if (cam == null) return;
            var plano = new Plane(transform.up, transform.position);
            Ray r = cam.ScreenPointToRay(pantalla);
            if (!plano.Raycast(r, out float d)) return;
            Vector3 loc = transform.InverseTransformPoint(r.GetPoint(d)); loc.y = 0f;
            if (!arrastrando) { arrastrando = true; offArrastre = desp - loc; }
            Vector3 objetivo = loc + offArrastre;
            if (objetivo.magnitude > RadioMax) objetivo = objetivo.normalized * RadioMax;
            desp = Vector3.Lerp(desp, objetivo, 0.5f);
            tUltimaInteraccion = UltimaGlobal = Time.unscaledTime;
        }

        void FinToque()
        {
            tocando = false; arrastrando = false; tieneGiroDedos = false;
            if (!bloqueado && !seMovio && Time.unscaledTime - tInicioToque < 0.3f) Saltar();
        }

        void Girar(float dx) { yawUsuario -= dx * 0.4f; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }

        void Zoom(float factor) { zoom = Mathf.Clamp(zoom * factor, 0.35f, 4f); tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }

        // ---------------- animación ----------------
        void Aplicar(float t)
        {
            if (modelo == null) return;
            float k = t >= 0.6f ? 1f : EaseOutBack(Mathf.Clamp01(t / 0.6f));      // aparición con rebote

            // balanceo suave solo cuando nadie la está moviendo
            float dt = Time.unscaledDeltaTime;
            if (girando) yawAuto -= 110f * dt;
            // caminar con joystick: se mueve sobre el plano de la placa, relativo a la cámara
            float mag = mov.magnitude;
            if (mag > 0.05f)
            {
                Vector3 dirLocal = DireccionLocal(mov);
                desp += dirLocal * (Velocidad * mag * dt);
                if (desp.magnitude > RadioMax) desp = desp.normalized * RadioMax;
                yawMov = Vector3.SignedAngle(Vector3.back, dirLocal, Vector3.up);
            }
            Vector3 vv = dt > 1e-5f ? (desp - despPrev) / dt : Vector3.zero; despPrev = desp;
            velocidad = Vector3.Lerp(velocidad, vv, 0.25f);
            bool seMueve = velocidad.magnitude > 0.035f;
            if (seMueve && mag <= 0.05f) yawMov = Vector3.SignedAngle(Vector3.back, velocidad.normalized, Vector3.up);
            mag = seMueve ? 1f : 0f;
            wCaminar = Mathf.MoveTowards(wCaminar, seMueve ? 1f : 0f, dt * 5f);
            wBaile = Mathf.MoveTowards(wBaile, bailando ? 1f : 0f, dt * 3f);
            if (bailando) tBaile += dt;
            if (tieneAnim) { anim.SetBool("Caminar", mag > 0.05f); anim.SetBool("Baile", bailando); }
            float quieto = Mathf.Clamp01((Time.unscaledTime - tUltimaInteraccion - 1.5f) / 1.5f);
            if (girando || caminando || bailando) quieto = 0f;
            float giro = 16f * Mathf.Sin(t * 0.9f) * quieto;

            // saludo: salto y vuelta completa al tocar
            float th = Time.unscaledTime - tHop, salto = 0f, vuelta = 0f;
            if (th >= 0f && th < 0.7f)
            {
                float u = th / 0.7f;
                salto = 0.07f * Mathf.Sin(Mathf.PI * u);
                vuelta = 360f * (u * u * (3f - 2f * u));
            }

            float flota = 0.008f * Mathf.Sin(t * 2.2f) + 0.008f;
            // acciones: golpear (embestida + impacto) y escribir (rebote de dedos)
            float ta = Time.unscaledTime - tAccion; float avance = 0f, squash = 0f, rollAcc = 0f;
            if (tipoAccion == 1 && ta >= 0f && ta < 1.5f)
            {
                float u = ta / 1.5f;
                avance = (u > .3f && u < .6f) ? 0.07f * Mathf.Sin(Mathf.PI * (u - .3f) / .3f) : 0f;
                if (!tieneAnim) { rollAcc = u < .3f ? -14f * (u / .3f) : (u < .6f ? 20f * Mathf.Sin(Mathf.PI * (u - .3f) / .3f) : 0f); }
                if (!impactoHecho && u >= .42f) { impactoHecho = true; Chispas(26); AudioManager.Mascota(); }
            }
            else if (tipoAccion == 2 && ta >= 0f && ta < 2.4f)
            {
                float env = Mathf.Clamp01(Mathf.Min(ta, 2.4f - ta) * 4f);
                if (!tieneAnim) { squash = 0.03f * env * Mathf.Abs(Mathf.Sin(ta * 8f * Mathf.PI)); rollAcc = 4f * env * Mathf.Sin(ta * 8f * Mathf.PI); }
                if (ta - tTecla > 0.15f) { tTecla = ta; AudioManager.Clic(); }
            }
            Vector3 frenteLocal = Quaternion.AngleAxis(yawFrente + yawUsuario, Vector3.up) * Vector3.back;
            modelo.localScale = escalaModelo * (k * zoom * (1f - squash));
            Vector3 paseo = desp; float yawBase = yawFrente + yawUsuario + giro + vuelta + yawAuto; float roll = 0f, paso = 0f;
            if (wCaminar > 0.001f)
            {
                yawBase = Mathf.LerpAngle(yawBase, yawMov, wCaminar);
                if (!tieneAnim)
                {
                    float fase = Time.unscaledTime * 2.6f * Mathf.PI;
                    paso = Mathf.Abs(Mathf.Sin(fase)) * 0.011f * wCaminar;
                    roll = Mathf.Sin(fase) * 6f * wCaminar;
                }
            }
            if (wBaile > 0.001f && !tieneAnim)
            {
                // baile: rebote al ritmo, balanceo, giros de cuerpo y vuelta completa cada 4 tiempos
                float ritmo = tBaile * 2f * Mathf.PI * 2f;                 // 2 tiempos por segundo
                paso += Mathf.Abs(Mathf.Sin(ritmo * 0.5f)) * 0.022f * wBaile;
                roll += Mathf.Sin(ritmo * 0.5f) * 12f * wBaile;
                float ciclo = (tBaile * 2f) % 8f;                          // 8 tiempos
                float giroBaile = ciclo > 6f ? 360f * Mathf.SmoothStep(0f, 1f, (ciclo - 6f) / 2f) : 35f * Mathf.Sin(ritmo * 0.25f);
                yawBase += giroBaile * wBaile;
                if (Mathf.Floor(tBaile * 2f) != Mathf.Floor((tBaile - dt) * 2f) && ((int)(tBaile * 2f)) % 4 == 0) Chispas(8);
            }
            modelo.localRotation = Quaternion.AngleAxis(yawBase, Vector3.up) * Quaternion.AngleAxis(roll + rollAcc, Vector3.forward) * rotModelo;
            Vector3 dirMira = Quaternion.AngleAxis(yawBase, Vector3.up) * Vector3.back;
            modelo.localPosition = posModelo + new Vector3(paseo.x, flota + salto + paso, paseo.z) + dirMira * avance;

            if (aro != null) { aro.localScale = escalaAro * (k * (1f + 0.07f * Mathf.Sin(t * 3.2f)) * Mathf.Max(1f, zoom)); aro.localPosition = posAro + new Vector3(paseo.x, 0f, paseo.z); }

            var cam = Camera.main;
            if (etiqueta != null && cam != null)
            {
                Vector3 d = etiqueta.position - cam.transform.position; d.y = 0;
                if (d.sqrMagnitude > 1e-8f) etiqueta.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                etiqueta.localScale = Vector3.one * k;
                etiqueta.localPosition = new Vector3(paseo.x, altura * 1.12f * Mathf.Max(1f, zoom), paseo.z);
            }
        }

        // Dirección del joystick convertida al plano de la placa (espacio local de la mascota)
        Vector3 DireccionLocal(Vector2 j)
        {
            var cam = Camera.main;
            if (cam == null) return new Vector3(j.x, 0f, j.y).normalized;
            Vector3 arriba = transform.up;
            Vector3 fw = Vector3.ProjectOnPlane(cam.transform.forward, arriba);
            if (fw.sqrMagnitude < 0.04f) fw = Vector3.ProjectOnPlane(cam.transform.up, arriba);
            fw.Normalize();
            Vector3 dr = Vector3.Cross(arriba, fw).normalized;            // derecha de la cámara sobre la placa
            Vector3 mundo = fw * j.y + dr * j.x;
            Vector3 loc = transform.InverseTransformDirection(mundo); loc.y = 0f;
            return loc.sqrMagnitude > 1e-6f ? loc.normalized : Vector3.forward;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
