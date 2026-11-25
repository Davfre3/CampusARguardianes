using UnityEngine;

namespace CampusAR
{
    // Voz del teléfono (Android TextToSpeech) en español. No necesita archivos de audio ni internet.
    // En el Editor no suena: solo se escribe en la consola.
    public static class VozSintetica
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject tts;
        static bool listo, iniciando;
        static string pendiente;

        class Escucha : AndroidJavaProxy
        {
            public Escucha() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
            void onInit(int estado)
            {
                if (estado != 0) { iniciando = false; return; }
                try
                {
                    string[][] idiomas = { new[] { "es", "PE" }, new[] { "es", "MX" }, new[] { "es", "ES" }, new[] { "es", "" } };
                    foreach (var l in idiomas)
                    {
                        using (var loc = new AndroidJavaObject("java.util.Locale", l[0], l[1]))
                        {
                            int r = tts.Call<int>("setLanguage", loc);
                            if (r >= 0) break;       // -1 y -2 = idioma no disponible
                        }
                    }
                    tts.Call<int>("setSpeechRate", 0.95f);
                    listo = true;
                }
                catch (System.Exception e) { Debug.LogWarning("[CampusAR] Voz: " + e.Message); }
                iniciando = false;
                if (!string.IsNullOrEmpty(pendiente)) { var p = pendiente; pendiente = null; Hablar(p); }
            }
        }

        static void Iniciar()
        {
            if (tts != null || iniciando) return;
            iniciando = true;
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var act = up.GetStatic<AndroidJavaObject>("currentActivity");
                    act.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try { tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", act, new Escucha()); }
                        catch (System.Exception e) { Debug.LogWarning("[CampusAR] Voz no disponible: " + e.Message); iniciando = false; }
                    }));
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[CampusAR] Voz no disponible: " + e.Message); iniciando = false; }
        }
#endif

        public static void Hablar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || !AudioManager.Activo) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (tts == null || !listo) { pendiente = texto; Iniciar(); return; }
            try { tts.Call<int>("speak", texto, 0, null, "campusar"); }   // 0 = QUEUE_FLUSH
            catch (System.Exception e) { Debug.LogWarning("[CampusAR] Voz: " + e.Message); }
#else
            Debug.Log("[CampusAR] (voz) " + texto);
#endif
        }

        public static void Detener()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            pendiente = null;
            try { if (tts != null && listo) tts.Call<int>("stop"); } catch { }
#endif
        }
    }
}
