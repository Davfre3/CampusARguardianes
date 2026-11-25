#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusAR
{
    /// <summary>
    /// Solo Editor: simula que se detecta una placa para probar la app sin la placa física.
    /// F1 Lobo · F2 Vikingo · F3 Toro · F4 León · F5 perder la placa · F6 completar 3 estaciones · F7 reiniciar progreso.
    /// </summary>
    public class SimuladorPlacaEditor : MonoBehaviour
    {
        static readonly string[] ids = { "admin", "ingenieria", "economia", "ciencias" };
        ARStationController ctl;
        readonly BindingFlags priv = BindingFlags.NonPublic | BindingFlags.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (FindAnyObjectByType<ARStationController>() == null) return;
            new GameObject("SimuladorPlacaEditor").AddComponent<SimuladorPlacaEditor>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (ctl == null) ctl = FindAnyObjectByType<ARStationController>();
            if (ctl == null) return;
            if (kb.f1Key.wasPressedThisFrame) Simular(0, true);
            if (kb.f2Key.wasPressedThisFrame) Simular(1, true);
            if (kb.f3Key.wasPressedThisFrame) Simular(2, true);
            if (kb.f4Key.wasPressedThisFrame) Simular(3, true);
            if (kb.f5Key.wasPressedThisFrame) for (int i = 0; i < ids.Length; i++) Simular(i, false);
            if (kb.f6Key.wasPressedThisFrame) { for (int i = 0; i < 3; i++) ProgressService.Completar(ids[i]); Debug.Log("[Simulador] 3 estaciones completadas."); }
            if (kb.f7Key.wasPressedThisFrame) { ProgressService.Reiniciar(ctl.catalogo); Debug.Log("[Simulador] Progreso reiniciado."); }
        }

        void Simular(int i, bool visible)
        {
            var dic = ctl.GetType().GetField("entradas", priv)?.GetValue(ctl) as System.Collections.IDictionary;
            if (dic == null || !dic.Contains(ids[i])) { Debug.LogWarning("[Simulador] Placa no cargada: " + ids[i]); return; }
            object e = dic[ids[i]];
            var t = e.GetType();
            var target = t.GetField("target").GetValue(e) as Component;
            if (visible && target != null && Camera.main != null)
            {
                var cam = Camera.main.transform;
                target.transform.SetPositionAndRotation(cam.position + cam.forward * 0.5f - cam.up * 0.12f, Quaternion.LookRotation(cam.forward, cam.up));
            }
            t.GetField("imgTracked").SetValue(e, visible);
            ctl.GetType().GetMethod("ActualizarEntrada", priv).Invoke(ctl, new[] { e });
            Debug.Log("[Simulador] " + ids[i] + (visible ? " detectada" : " perdida"));
        }
    }
}
#endif
