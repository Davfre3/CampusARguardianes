using UnityEngine;

namespace CampusAR
{
    // Sonidos y música de la app. Si existen archivos en Assets/CampusAR/Resources/Audio/
    // (clic, acierto, error, insignia, mascota, musica) se usan; si no, se generan por código
    // para que la app suene desde el primer momento sin descargar nada.
    public class AudioManager : MonoBehaviour
    {
        const int Fs = 22050;
        static AudioManager inst;
        AudioSource sfx, musica;
        AudioClip cClic, cAcierto, cError, cInsignia, cMascota, cMusica;
        bool activo = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Crear()
        {
            if (inst != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            activo = PlayerPrefs.GetInt("sonido", 1) == 1;
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false; sfx.spatialBlend = 0f;
            musica = gameObject.AddComponent<AudioSource>(); musica.playOnAwake = false; musica.loop = true; musica.spatialBlend = 0f; musica.volume = 0.16f;

            cClic = Cargar("clic") ?? Tono("clic", 0.05f, new[] { 1200f }, 0.35f);
            cAcierto = Cargar("acierto") ?? Notas("acierto", 0.13f, 0.35f, 523.25f, 659.25f, 783.99f, 1046.5f);
            cError = Cargar("error") ?? Notas("error", 0.17f, 0.28f, 330f, 262f);
            cInsignia = Cargar("insignia") ?? Notas("insignia", 0.14f, 0.35f, 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f);
            cMascota = Cargar("mascota") ?? Boing();
            cMusica = Cargar("musica") ?? MusicaSuave();
            musica.clip = cMusica;
            Aplicar();
        }

        static AudioClip Cargar(string n) { return Resources.Load<AudioClip>("Audio/" + n); }

        void Aplicar()
        {
            AudioListener.pause = false;
            if (activo) { if (!musica.isPlaying) musica.Play(); }
            else musica.Stop();
        }

        // ---------- API
        public static bool Activo { get { return inst != null && inst.activo; } }
        public static bool AlternarSonido()
        {
            if (inst == null) return false;
            inst.activo = !inst.activo;
            PlayerPrefs.SetInt("sonido", inst.activo ? 1 : 0); PlayerPrefs.Save();
            inst.Aplicar();
            if (inst.activo) Clic();
            else VozSintetica.Detener();
            return inst.activo;
        }
        static void Tocar(AudioClip c, float vol = 1f) { if (inst != null && inst.activo && c != null) inst.sfx.PlayOneShot(c, vol); }
        public static void Clic() { if (inst != null) Tocar(inst.cClic, 0.6f); }
        public static void Acierto() { if (inst != null) Tocar(inst.cAcierto); }
        public static void Error() { if (inst != null) Tocar(inst.cError, 0.8f); }
        public static void Insignia() { if (inst != null) Tocar(inst.cInsignia); }
        public static void Mascota() { if (inst != null) Tocar(inst.cMascota, 0.8f); }

        // ---------- síntesis
        static AudioClip Crear(string nombre, float[] d)
        {
            var c = AudioClip.Create(nombre, d.Length, 1, Fs, false);
            c.SetData(d, 0);
            return c;
        }

        static float Env(float t, float dur, float ataque = 0.005f)
        {
            float a = Mathf.Clamp01(t / ataque);
            float r = Mathf.Clamp01((dur - t) / Mathf.Max(0.02f, dur * 0.8f));
            return a * r * r;
        }

        static AudioClip Tono(string n, float dur, float[] f, float vol)
        {
            int N = (int)(dur * Fs); var d = new float[N];
            for (int i = 0; i < N; i++)
            {
                float t = (float)i / Fs, s = 0f;
                foreach (var fr in f) s += Mathf.Sin(2f * Mathf.PI * fr * t);
                d[i] = s / f.Length * vol * Env(t, dur, 0.002f);
            }
            return Crear(n, d);
        }

        static AudioClip Notas(string n, float paso, float vol, params float[] fr)
        {
            float dur = paso * fr.Length + 0.45f;
            int N = (int)(dur * Fs); var d = new float[N];
            for (int k = 0; k < fr.Length; k++)
            {
                int i0 = (int)(k * paso * Fs);
                float len = k == fr.Length - 1 ? 0.45f : 0.28f;
                int L = (int)(len * Fs);
                for (int i = 0; i < L && i0 + i < N; i++)
                {
                    float t = (float)i / Fs;
                    float s = Mathf.Sin(2f * Mathf.PI * fr[k] * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * fr[k] * t);
                    d[i0 + i] += s * vol * Env(t, len, 0.004f);
                }
            }
            return Crear(n, d);
        }

        static AudioClip Boing()
        {
            float dur = 0.22f; int N = (int)(dur * Fs); var d = new float[N]; float ph = 0f;
            for (int i = 0; i < N; i++)
            {
                float t = (float)i / Fs, f = Mathf.Lerp(280f, 640f, Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur) * 0.5f));
                ph += 2f * Mathf.PI * f / Fs;
                d[i] = Mathf.Sin(ph) * 0.4f * Env(t, dur, 0.004f);
            }
            return Crear("mascota", d);
        }

        // Música ambiental tranquila de 16 s en bucle: acordes suaves + arpegio ligero.
        static AudioClip MusicaSuave()
        {
            float[][] acordes = {
                new[]{130.81f,164.81f,196f},     // Do
                new[]{110f,130.81f,164.81f},     // La menor
                new[]{87.31f,130.81f,174.61f},   // Fa
                new[]{98f,123.47f,196f}          // Sol
            };
            float[][] arp = {
                new[]{523.25f,659.25f,783.99f,659.25f},
                new[]{440f,523.25f,659.25f,523.25f},
                new[]{349.23f,440f,523.25f,440f},
                new[]{392f,493.88f,587.33f,493.88f}
            };
            float compas = 4f; int N = (int)(compas * 4 * Fs); var d = new float[N];
            for (int c = 0; c < 4; c++)
            {
                int i0 = (int)(c * compas * Fs), L = (int)(compas * Fs);
                for (int i = 0; i < L; i++)
                {
                    float t = (float)i / Fs, s = 0f;
                    foreach (var f in acordes[c]) s += Mathf.Sin(2f * Mathf.PI * f * t);
                    float suave = Mathf.Min(Mathf.Clamp01(t / 0.6f), Mathf.Clamp01((compas - t) / 0.6f));
                    d[i0 + i] += s / 3f * 0.30f * suave;
                }
                for (int k = 0; k < 8; k++)
                {
                    float f = arp[c][k % 4] * (k >= 4 ? 1f : 1f);
                    int j0 = i0 + (int)(k * 0.5f * Fs), LL = (int)(0.9f * Fs);
                    for (int i = 0; i < LL && j0 + i < N; i++)
                    {
                        float t = (float)i / Fs;
                        d[j0 + i] += Mathf.Sin(2f * Mathf.PI * f * t) * 0.10f * Mathf.Exp(-3.2f * t) * Mathf.Clamp01(t / 0.01f);
                    }
                }
            }
            // el final enlaza con el inicio sin clic
            for (int i = 0; i < 200; i++) { float a = i / 200f; d[i] *= a; d[N - 1 - i] *= a; }
            return Crear("musica", d);
        }
    }
}
