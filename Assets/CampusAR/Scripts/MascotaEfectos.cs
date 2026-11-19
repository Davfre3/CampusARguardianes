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

        public void Configurar(StationDefinition def, float alturaMascota)
        {
            altura = alturaMascota;
            foreach (Transform h in transform)
            {
                if (h.name == "Aro") aro = h;
                else if (modelo == null && h.name != "Etiqueta") modelo = h;
            }
            if (modelo == null) modelo = transform;
            escalaModelo = modelo.localScale; posModelo = modelo.localPosition; rotModelo = modelo.localRotation;
            if (aro != null) escalaAro = aro.localScale;

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
            OrientarDeFrente();
            Aplicar(0f);
        }

        // Vuelve a aparecer con rebote y de frente (por ejemplo, al cerrar el menú para verla).
        public void Reiniciar()
        {
            t0 = Time.unscaledTime;
            yawUsuario = 0f; zoom = 1f; tUltimaInteraccion = -99f;
            OrientarDeFrente();
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
                            float dist = Vector2.Distance(c0.position.ReadValue(), c1.position.ReadValue());
                            if (distanciaPrev > 1f) Zoom(dist / distanciaPrev);
                            distanciaPrev = dist; seMovio = true;
                        }
                        else
                        {
                            Vector2 dl = c0.delta.ReadValue();
                            if ((c0.position.ReadValue() - posInicio).sqrMagnitude > 400f) seMovio = true;
                            if (seMovio) Girar(dl.x);
                            distanciaPrev = 0f;
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

        void FinToque()
        {
            tocando = false;
            if (!bloqueado && !seMovio && Time.unscaledTime - tInicioToque < 0.3f) { tHop = Time.unscaledTime; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }
        }

        void Girar(float dx) { yawUsuario -= dx * 0.4f; tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }

        void Zoom(float factor) { zoom = Mathf.Clamp(zoom * factor, 0.5f, 2.2f); tUltimaInteraccion = UltimaGlobal = Time.unscaledTime; }

        // ---------------- animación ----------------
        void Aplicar(float t)
        {
            if (modelo == null) return;
            float k = t >= 0.6f ? 1f : EaseOutBack(Mathf.Clamp01(t / 0.6f));      // aparición con rebote

            // balanceo suave solo cuando nadie la está moviendo
            float quieto = Mathf.Clamp01((Time.unscaledTime - tUltimaInteraccion - 1.5f) / 1.5f);
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
            modelo.localScale = escalaModelo * (k * zoom);
            modelo.localPosition = posModelo + Vector3.up * ((t > 0.6f ? flota : flota * k) + salto);
            modelo.localRotation = Quaternion.AngleAxis(yawFrente + yawUsuario + giro + vuelta, Vector3.up) * rotModelo;

            if (aro != null) aro.localScale = escalaAro * (k * (1f + 0.07f * Mathf.Sin(t * 3.2f)) * Mathf.Max(1f, zoom));

            var cam = Camera.main;
            if (etiqueta != null && cam != null)
            {
                Vector3 d = etiqueta.position - cam.transform.position; d.y = 0;
                if (d.sqrMagnitude > 1e-8f) etiqueta.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                etiqueta.localScale = Vector3.one * k;
                etiqueta.localPosition = new Vector3(0, altura * 1.12f * Mathf.Max(1f, zoom), 0);
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
