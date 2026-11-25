#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

namespace CampusAR.EditorTools
{
    // Menú: Campus AR > Construir proyecto (Inicio + ExploracionAR + datos + ajustes Android)
    // Idempotente: se puede ejecutar de nuevo para regenerar escenas y datos.
    public static class CampusARConstructor
    {
        const string Raiz = "Assets/CampusAR";
        const string RutaDatos = Raiz + "/Datos";
        const string RutaPrefabs = Raiz + "/Prefabs";
        const string RutaMarcadores = Raiz + "/Marcadores";
        const string RutaEscenas = Raiz + "/Escenas";
        const string RutaRecursos = Raiz + "/Resources";

        class Est
        {
            public string id, target, nombre, grupo, mascota, fbx, resumen, pregunta, explicacion, pista, insignia, mensaje;
            public Color color;
            public CareerLink[] carreras;
            public string[] opciones;
            public int correcta;
        }

        static readonly Dictionary<string, string> InfoCorta = new Dictionary<string, string>
        {
            {"admin", "¡Hola! Soy el Lobo de Administración y Negocios. Aquí aprendes a dirigir empresas, liderar equipos y tomar buenas decisiones. Puedes estudiar Administración, Marketing o Negocios Internacionales."},
            {"ingenieria", "¡Hola! Soy el Vikingo de Ingeniería. Aquí aprendes a crear soluciones con tecnología: software, datos, inteligencia artificial y procesos industriales."},
            {"economia", "¡Hola! Soy el Toro de Economía. Aquí aprendes cómo se mueven el dinero, los precios y los mercados para tomar mejores decisiones. Puedes estudiar Economía, Economía y Finanzas o Economía y Negocios Internacionales."},
            {"ciencias", "¡Hola! Soy el León de Derecho y Ciencias Sociales. Aquí aprendes sobre las leyes, la justicia y el comportamiento de las personas. Puedes estudiar Derecho, Derecho Corporativo o Psicología."},
        };

        static Sprite CargarInsignia(string id)
        {
            string ruta = Raiz + "/Insignias/Insignia_" + id + ".png";
            if (!File.Exists(ruta)) return null;
            var ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (ti != null && (ti.textureType != TextureImporterType.Sprite || !ti.alphaIsTransparency))
            {
                ti.textureType = TextureImporterType.Sprite; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        // Material sin luz para las chispas 3D (en Resources para que entre en la build con su shader).
        static void AsegurarFxMat()
        {
            string p = RutaRecursos + "/FxMat.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(p) != null) return;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) return;
            var m = new Material(sh); m.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(m, p);
        }

        static readonly Est[] Estaciones = new Est[]
        {
        new Est{ id="admin", target="ADMIN_LOBO", nombre="Administración", grupo="Administración y negocios", mascota="Lobo", color=new Color(0.122f,0.310f,0.878f), fbx="Business_Wolf_texture",
            insignia="Estratega", mensaje="Piensa como quien dirige y toma decisiones.",
            resumen="Administración y negocios. Piensa como quien dirige y toma decisiones. Estilo del reto: decisión, estrategia o gestión. En Carreras puedes abrir la malla, las certificaciones y las áreas de especialización de cada carrera en la página oficial.",
            carreras=new[]{
                new CareerLink{nombre="Administración", url="https://www.ue.edu.pe/pregrado/carreras/administracion", urlMalla="https://www.ue.edu.pe/pregrado/carreras/administracion", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Administracion.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Administracion.pdf"},
                new CareerLink{nombre="Administración y Finanzas", url="https://www.ue.edu.pe/pregrado/carreras/administracion-finanzas", urlMalla="https://www.ue.edu.pe/pregrado/carreras/administracion-finanzas", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Administracion%20y%20Finanzas.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Administracion%20y%20Finanzas.pdf"},
                new CareerLink{nombre="Administración y Marketing", url="https://www.ue.edu.pe/pregrado/carreras/administracion-marketing", urlMalla="https://www.ue.edu.pe/pregrado/carreras/administracion-marketing", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Administracion%20y%20Marketing.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Administracion%20y%20Marketing.pdf"},
                new CareerLink{nombre="Administración y Negocios Internacionales", url="https://www.ue.edu.pe/pregrado/carreras/administracion-y-negocios-internacionales", urlMalla="https://www.ue.edu.pe/pregrado/carreras/administracion-y-negocios-internacionales", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Administracion%20y%20Negocios%20Internacionales.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Administracion%20y%20Negocios%20Internacionales.pdf"},
                new CareerLink{nombre="Comunicación Empresarial y Marketing Digital", url="https://www.ue.edu.pe/pregrado/carreras/comunicacion-empresarial-y-marketing-digital", urlMalla="https://www.ue.edu.pe/pregrado/carreras/comunicacion-empresarial-y-marketing-digital", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Comunicacion%20Empresarial%20y%20Marketing%20Digital.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Comunicacion%20Empresarial%20y%20Marketing%20Digital.pdf"} },
            pregunta="¿Qué hace una persona que estudia Administración y Negocios?", opciones=new[]{"Organiza y dirige empresas","Repara computadoras","Opera a pacientes"}, correcta=0, explicacion="Quien estudia Administración aprende a organizar equipos, recursos y empresas para lograr sus metas.", pista="Piensa en quién dirige un negocio o un equipo de trabajo." },
        new Est{ id="ingenieria", target="ING_VIKINGO", nombre="Ingeniería", grupo="Ingeniería", mascota="Vikingo", color=new Color(0.071f,0.600f,0.290f), fbx="Techno_Viking_texture",
            insignia="Innovador", mensaje="Resuelve el reto y construye una solución.",
            resumen="Ingeniería. Resuelve el reto y construye una solución. Estilo del reto: lógica, tecnología o innovación. En Carreras puedes abrir la malla, las certificaciones y las áreas de especialización de cada carrera en la página oficial.",
            carreras=new[]{
                new CareerLink{nombre="Ingeniería Industrial y Comercial", url="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria%20Industrial%20y%20Comercial.pdf", urlMalla="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria%20Industrial%20y%20Comercial.pdf", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/Certificaciones/Ingenieria/Ingenieria%20Industrial%20y%20Comercial_Certificados.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/Especializaciones/Ingenieria/Ingenieria%20Industrial%20y%20Comercial_Especializacion.pdf"},
                new CareerLink{nombre="Ingeniería de Software", url="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria_de_Software.pdf", urlMalla="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria_de_Software.pdf", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Certificacion-INGENIERIA-DE-SOFTWARE.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Areas-de-especializacion-INGENIERIA-DE-SOFTWARE.pdf"},
                new CareerLink{nombre="Ingeniería de Tecnologías de Información y Sistemas", url="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria%20_de_tecnologias_de_Informacion.pdf", urlMalla="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria%20_de_tecnologias_de_Informacion.pdf", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/Certificaciones/Ingenieria/Ingenieria%20de%20Tecnologias%20de%20Informacion%20y%20Sistemas_Certificados.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/Especializaciones/Ingenieria/Ingenieria%20de%20Tecnologias%20de%20Informacion%20y%20Sistemas_Especializacion.pdf"},
                new CareerLink{nombre="Ingeniería en Ciencia de Datos", url="https://www.ue.edu.pe/pregrado/carreras/ingenieria-en-ciencia-de-datos", urlMalla="https://www.ue.edu.pe/pregrado/carreras/ingenieria-en-ciencia-de-datos", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Certificacion-INGENIERIA-DE-CIENCIA-DE-DATOS.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Areas-de-especializacion-INGENIERIA-DE-CIENCIA-DE-DATOS.pdf"},
                new CareerLink{nombre="Ingeniería en Inteligencia Artificial", url="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria_en_Inteligencia%20Artificial.pdf", urlMalla="https://www.ue.edu.pe/pregrado/images/files/2025/Mallas%20Curriculares/Ingenieria/Malla_Ingenieria_en_Inteligencia%20Artificial.pdf", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/Certificaciones/Ingenieria/Ingenieria%20en%20Inteligencia%20Artificial_Certificados.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/Especializaciones/Ingenieria/Ingenieria%20en%20Inteligencia%20Artificial_Especializacion.pdf"} },
            pregunta="¿Para qué sirve la Ingeniería?", opciones=new[]{"Para crear soluciones a problemas con tecnología","Para escribir poemas","Para preparar recetas de cocina"}, correcta=0, explicacion="La Ingeniería usa la ciencia y la tecnología para crear cosas útiles: apps, máquinas, sistemas y más.", pista="Piensa en quien inventa o mejora cosas como apps, puentes o máquinas." },
        new Est{ id="economia", target="ECO_TORO", nombre="Economía", grupo="Economía", mascota="Toro", color=new Color(0.878f,0.125f,0.169f), fbx="Bullish_Banker_texture",
            insignia="Analista", mensaje="Observa los datos y encuentra la mejor lectura.",
            resumen="Economía. Observa los datos y encuentra la mejor lectura. Estilo del reto: análisis económico o financiero. En Carreras puedes abrir la malla, las certificaciones y las áreas de especialización de cada carrera en la página oficial.",
            carreras=new[]{
                new CareerLink{nombre="Economía", url="https://www.ue.edu.pe/pregrado/carreras/economia", urlMalla="https://www.ue.edu.pe/pregrado/carreras/economia", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Economia.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Economia.pdf"},
                new CareerLink{nombre="Economía y Finanzas", url="https://www.ue.edu.pe/pregrado/carreras/economia-y-finanzas", urlMalla="https://www.ue.edu.pe/pregrado/carreras/economia-y-finanzas", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Economia%20y%20Finanzas.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Economia%20y%20Finanzas.pdf"},
                new CareerLink{nombre="Economía y Negocios Internacionales", url="https://www.ue.edu.pe/pregrado/carreras/economia-negocios-internacionales", urlMalla="https://www.ue.edu.pe/pregrado/carreras/economia-negocios-internacionales", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/certificaciones/Economia%20y%20Negocios%20Internacionales.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2025/FCEA/especializaciones/Economia%20y%20Negocios%20Internacionales.pdf"} },
            pregunta="¿Qué estudia la Economía?", opciones=new[]{"Cómo se usan el dinero y los recursos","Cómo se construyen puentes","Cómo se curan las enfermedades"}, correcta=0, explicacion="La Economía estudia cómo personas, empresas y países usan el dinero y los recursos para tomar decisiones.", pista="Piensa en dinero, precios y en cómo decidimos qué comprar." },
        new Est{ id="ciencias", target="CIE_LEON", nombre="Derecho y Ciencias Sociales", grupo="Derecho y Ciencias Sociales", mascota="León", color=new Color(0.965f,0.710f,0.000f), fbx="Lab_Lion_texture",
            insignia="Explorador social", mensaje="Analiza el caso y comprende a las personas y su entorno.",
            resumen="Derecho y Ciencias Sociales. Analiza el caso y comprende a las personas y su entorno. Estilo del reto: interpretación, caso breve o comportamiento humano. En Carreras puedes abrir la malla, las certificaciones y las áreas de especialización de cada carrera en la página oficial.",
            carreras=new[]{
                new CareerLink{nombre="Derecho", url="https://www.ue.edu.pe/pregrado/carreras/derecho", urlMalla="https://www.ue.edu.pe/pregrado/carreras/derecho", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Certificacion-DERECHO.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Areas-de-especializacion-DERECHO.pdf"},
                new CareerLink{nombre="Derecho Corporativo", url="https://www.ue.edu.pe/pregrado/carreras/derecho-corporativo", urlMalla="https://www.ue.edu.pe/pregrado/carreras/derecho-corporativo", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2024/Certificaciones-Especializaciones/Certificados%20de%20especialidad%20y%20certificaciones%20transversales%20-%20Derecho%20Corporativo-2.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2024/Certificaciones-Especializaciones/Certificados%20de%20especialidad%20y%20certificaciones%20transversales%20-%20Derecho%20Corporativo-1.pdf"},
                new CareerLink{nombre="Psicología", url="https://www.ue.edu.pe/pregrado/carreras/psicologia", urlMalla="https://www.ue.edu.pe/pregrado/carreras/psicologia", urlCertificaciones="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Certificacion-PSICOLOGIA.pdf", urlEspecializacion="https://www.ue.edu.pe/pregrado/images/files/2026/certificaciones/Areas-de-especializacion-PSICOLOGIA.pdf"} },
            pregunta="¿Qué estudia el Derecho?", opciones=new[]{"Las leyes y las normas para vivir en sociedad","Los planetas y las estrellas","Los motores de los autos"}, correcta=0, explicacion="El Derecho estudia las leyes y normas que ayudan a que las personas convivan con justicia.", pista="Piensa en jueces, abogados y en las reglas que todos debemos cumplir." },
        };

        // Targets de la base "Trabajo_final" (Target Manager). Los 8 cuadrados (QR) NO se usan.
        static readonly Dictionary<string, string> ModelosAR = new Dictionary<string, string>
        { {"admin","Business_Wolf_texture"} }; // solo el lobo tiene figura física por ahora
        static readonly Dictionary<string, string> BaseDatos = new Dictionary<string, string>
        {
            { "admin", "image" },        // lobo
            { "ciencias", "image1" },    // león
            { "economia", "image2" },    // toro
            { "ingenieria", "image3" }   // vikingo
        };

        // Colores de interfaz
        static readonly Color Fondo = new Color(0.06f, 0.09f, 0.16f, 1f);
        static readonly Color Tarjeta = new Color(0.10f, 0.15f, 0.26f, 0.96f);
        static readonly Color Boton = new Color(0.16f, 0.36f, 0.82f, 1f);
        static readonly Color BotonSec = new Color(0.22f, 0.27f, 0.40f, 1f);
        static readonly Color Texto = new Color(0.95f, 0.97f, 1f, 1f);
        static readonly Color TextoSuave = new Color(0.72f, 0.80f, 0.92f, 1f);

        [MenuItem("Campus AR/Construir proyecto (Inicio + AR + datos)")]
        public static void ConstruirTodo()
        {
            AsegurarCarpetas();
            ConfigurarMarcadores();
            PrepararIlustraciones();
            var prefabs = CrearPrefabsMascotas();
            var catalogo = CrearDatos(prefabs);
            ConfigurarAndroid();
            var inicio = ConstruirEscena(false, catalogo);
            var ar = ConstruirEscena(true, catalogo);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(inicio, true),
                new EditorBuildSettingsScene(ar, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(inicio);
            Debug.Log("[Campus AR] Proyecto construido. Escenas: " + inicio + " y " + ar);
        }

        // ---------------------------------------------------------------- datos
        static void AsegurarCarpetas()
        {
            foreach (var p in new[] { Raiz, RutaDatos, RutaPrefabs, RutaMarcadores, RutaEscenas, RutaRecursos, Raiz + "/Materiales", Raiz + "/Texturas" })
                if (!AssetDatabase.IsValidFolder(p))
                {
                    var padre = Path.GetDirectoryName(p).Replace('\\', '/');
                    AssetDatabase.CreateFolder(padre, Path.GetFileName(p));
                }
        }

        static void ConfigurarMarcadores()
        {
            AssetDatabase.Refresh();
            foreach (var e in Estaciones)
            {
                string p = RutaMarcadores + "/" + e.target + ".png";
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) { Debug.LogError("[Campus AR] Falta el marcador " + p); continue; }
                ti.textureType = TextureImporterType.Default;
                ti.isReadable = true;                       // necesario para CreateImageTarget en tiempo de ejecución
                ti.mipmapEnabled = false;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.maxTextureSize = 2048;                   // límite de Vuforia
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.SaveAndReimport();
            }
        }

        static Dictionary<string, GameObject> CrearPrefabsMascotas()
        {
            var res = new Dictionary<string, GameObject>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var e in Estaciones)
            {
                string rutaAnim = Raiz + "/Animaciones/" + e.id;
                var ctrlAnim = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(rutaAnim + "/Animator_" + e.id + ".controller");
                var fbxAnim = AssetDatabase.LoadAssetAtPath<GameObject>(rutaAnim + "/Personaje.fbx");
                bool conAnim = ctrlAnim != null && fbxAnim != null;
                var fbx = conAnim ? fbxAnim : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/" + e.fbx + ".fbx");
                if (fbx == null) { Debug.LogWarning("[Campus AR] Sin modelo para " + e.nombre + ": se usará la figura temporal."); continue; }

                var root = new GameObject("Mascota_" + e.mascota);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.transform.SetParent(root.transform, false);

                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) { Object.DestroyImmediate(root); Debug.LogWarning("[Campus AR] " + e.fbx + " no tiene mallas."); continue; }

                // Material con las texturas reales del personaje (vienen incrustadas en el FBX y Unity no las aplica solo)
                var matMasc = CrearMaterialMascota(e.mascota, lit);
                if (matMasc != null)
                {
                    foreach (var r in rs)
                    {
                        var ms = r.sharedMaterials;
                        for (int i = 0; i < ms.Length; i++) ms[i] = matMasc;
                        r.sharedMaterials = ms;
                    }
                }

                if (conAnim)
                {
                    inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // Mixamo mira a +Z; la app usa -Z como frente
                }
                else
                // Orientación: en estos FBX la cabeza está en +Z y el frente en -Y (datos del archivo).
                // Se calcula dónde quedan en el mundo y se corrige para que el personaje quede de pie
                // (cabeza arriba) y mirando hacia el borde inferior de la placa, donde está quien la escanea.
                {
                    var r0 = inst.GetComponentInChildren<Renderer>();
                    Vector3 arriba = r0.transform.TransformDirection(Vector3.forward).normalized;
                    Vector3 frente = r0.transform.TransformDirection(Vector3.down).normalized;
                    Quaternion corr = Quaternion.FromToRotation(arriba, Vector3.up);
                    Vector3 f2 = corr * frente; f2.y = 0f;
                    if (f2.sqrMagnitude > 1e-6f) corr = Quaternion.FromToRotation(f2.normalized, Vector3.back) * corr;
                    inst.transform.rotation = corr * inst.transform.rotation;
                }

                // Evita que el modelo se recorte por límites mal calculados del esqueleto
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
                foreach (var an in inst.GetComponentsInChildren<Animator>(true)) an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (conAnim)
                {
                    var an0 = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
                    an0.runtimeAnimatorController = ctrlAnim; an0.applyRootMotion = false;
                    an0.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    Debug.Log("[Campus AR] " + e.mascota + ": usando animaciones de Mixamo.");
                }

                // Normalizar: altura 0,20 m, apoyo en y=0, centrado en x/z
                Bounds b = Limites(rs);
                float s = 0.26f / Mathf.Max(b.size.y, 0.0001f);
                // Se MULTIPLICA la escala que trae el FBX (suele traer 100): antes se pisaba y quedaba 100 veces más chico.
                inst.transform.localScale = inst.transform.localScale * s;
                b = Limites(inst.GetComponentsInChildren<Renderer>());
                inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

                // Aro luminoso en el color de la facultad, bajo los pies, para resaltar al personaje sobre la placa
                {
                    var aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    aro.name = "Aro";
                    Object.DestroyImmediate(aro.GetComponent<Collider>());
                    aro.transform.SetParent(root.transform, false);
                    aro.transform.localScale = new Vector3(0.20f, 0.0015f, 0.20f);
                    aro.transform.localPosition = new Vector3(0f, 0.0015f, 0f);
                    var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlit != null)
                    {
                        if (!AssetDatabase.IsValidFolder(Raiz + "/Materiales")) AssetDatabase.CreateFolder(Raiz, "Materiales");
                        string mp2 = Raiz + "/Materiales/Aro_" + e.mascota + ".mat";
                        AssetDatabase.DeleteAsset(mp2);
                        var am = new Material(unlit);
                        am.SetColor("_BaseColor", e.color);
                        AssetDatabase.CreateAsset(am, mp2);
                        aro.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(mp2);
                    }
                }

                string path = RutaPrefabs + "/Mascota_" + e.mascota + ".prefab";
                var pf = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                res[e.id] = pf;
            }
            return res;
        }

        static Material CrearMaterialMascota(string mascota, Shader lit)
        {
            if (lit == null) return null;
            string arch = mascota.Replace("ó", "o").Replace("Ó", "O");   // los archivos de textura no llevan tilde (León -> Leon)
            string baseP = Raiz + "/Texturas/" + arch + "_base.jpg";
            string normP = Raiz + "/Texturas/" + arch + "_normal.jpg";
            var ib = AssetImporter.GetAtPath(baseP) as TextureImporter;
            if (ib != null) { ib.textureType = TextureImporterType.Default; ib.sRGBTexture = true; ib.maxTextureSize = 2048; ib.mipmapEnabled = true; ib.SaveAndReimport(); }
            var inr = AssetImporter.GetAtPath(normP) as TextureImporter;
            if (inr != null) { inr.textureType = TextureImporterType.NormalMap; inr.maxTextureSize = 2048; inr.SaveAndReimport(); }
            var tb = AssetDatabase.LoadAssetAtPath<Texture2D>(baseP);
            var tn = AssetDatabase.LoadAssetAtPath<Texture2D>(normP);
            if (tb == null) { Debug.LogWarning("[Campus AR] Falta la textura " + baseP); return null; }
            if (!AssetDatabase.IsValidFolder(Raiz + "/Materiales")) AssetDatabase.CreateFolder(Raiz, "Materiales");
            string mp = Raiz + "/Materiales/Mascota_" + mascota + ".mat";
            AssetDatabase.DeleteAsset(mp);
            var m = new Material(lit) { name = "Mascota_" + mascota };
            m.SetTexture("_BaseMap", tb); m.SetColor("_BaseColor", Color.white);
            if (tn != null) { m.SetTexture("_BumpMap", tn); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(m, mp);
            return AssetDatabase.LoadAssetAtPath<Material>(mp);
        }

        static Bounds Limites(Renderer[] rs)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static readonly Dictionary<string, Sprite> Ilus = new Dictionary<string, Sprite>();

        // Copia el arte de cada placa como sprite de interfaz (los marcadores de Vuforia no se tocan).
        static void PrepararIlustraciones()
        {
            Ilus.Clear();
            if (!AssetDatabase.IsValidFolder(Raiz + "/UI")) AssetDatabase.CreateFolder(Raiz, "UI");
            foreach (var e in Estaciones)
            {
                string origen = Raiz + "/Marcadores/" + e.target + ".png";
                string destino = Raiz + "/UI/Placa_" + e.target + ".png";
                AssetDatabase.DeleteAsset(destino);
                AssetDatabase.CopyAsset(origen, destino);
                var ti = AssetImporter.GetAtPath(destino) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                    ti.mipmapEnabled = false; ti.maxTextureSize = 1024; ti.isReadable = false;
                    ti.textureCompression = TextureImporterCompression.Compressed;
                    ti.SaveAndReimport();
                }
                Ilus[e.id] = AssetDatabase.LoadAssetAtPath<Sprite>(destino);
            }
        }

        static StationCatalog CrearDatos(Dictionary<string, GameObject> prefabs)
        {
            var lista = new List<StationDefinition>();
            AsegurarFxMat();
            foreach (var e in Estaciones)
            {
                string p = RutaDatos + "/Estacion_" + e.id + ".asset";
                var def = AssetDatabase.LoadAssetAtPath<StationDefinition>(p);
                if (def == null) { def = ScriptableObject.CreateInstance<StationDefinition>(); AssetDatabase.CreateAsset(def, p); }
                def.stationId = e.id;
                def.ilustracion = Ilus.ContainsKey(e.id) ? Ilus[e.id] : null;
                def.targetName = e.target;
                def.displayName = e.nombre;
                def.mascotaNombre = e.mascota;
                def.colorFacultad = e.color;
                def.mascotPrefab = prefabs.ContainsKey(e.id) ? prefabs[e.id] : null;
                def.markerImage = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaMarcadores + "/" + e.target + ".png");
                def.markerWidthMeters = 0.14f;
                def.databasePath = "Vuforia/Trabajo_final.xml";
                def.databaseTargetName = BaseDatos.ContainsKey(e.id) ? BaseDatos[e.id] : "";
                if (ModelosAR.ContainsKey(e.id)) { def.modelDatabasePath = "Vuforia/" + ModelosAR[e.id] + ".xml"; def.modelTargetName = ModelosAR[e.id]; }
                def.summary = e.resumen;
                def.grupoNombre = e.grupo;
                def.insigniaNombre = e.insignia;
                def.insigniaIcono = CargarInsignia(e.id);
                def.infoCorta = InfoCorta.ContainsKey(e.id) ? InfoCorta[e.id] : e.mensaje;
                def.mensajePersonalidad = e.mensaje;
                def.careers = e.carreras;
                // Datos sin aprobación: se dejan explícitos como pendientes (no inventar).
                def.directions = "";
                def.contact = "Oficina de Admisión de Pregrado\nTeléfono: 989 094 988\nCorreo: exitoesan@ue.edu.pe";
                def.serviceHours = "Atención presencial: lunes a viernes, de 10:00 a. m. a 6:45 p. m.";
                def.webUrl = "https://davfre3.github.io/campus-ar-guardians-web/" + (e.id=="admin"?"lobo":e.id=="economia"?"toro":e.id=="ingenieria"?"vikingo":"leon") + ".html";
                def.mapUrl = "";
                def.quiz = new QuizData { pregunta = e.pregunta, opciones = e.opciones, indiceCorrecto = e.correcta, explicacion = e.explicacion, pista = e.pista };
                def.contentVersion = "0.3 (contenido de la guía de gamificación y del zip; mapa fuera por decisión del equipo)";
                EditorUtility.SetDirty(def);
                lista.Add(def);
            }
            string pc = RutaRecursos + "/CatalogoEstaciones.asset";
            var cat = AssetDatabase.LoadAssetAtPath<StationCatalog>(pc);
            if (cat == null) { cat = ScriptableObject.CreateInstance<StationCatalog>(); AssetDatabase.CreateAsset(cat, pc); }
            cat.estaciones = lista.ToArray();
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            return cat;
        }

        // ---------------------------------------------------------------- Android
        static void ConfigurarAndroid()
        {
            PlayerSettings.productName = "Campus AR Guardians";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.campusar.guardians");   // provisional
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
        }

        // ---------------------------------------------------------------- UI helpers
        static RectTransform Rt(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        static void Estirar(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
        }

        static void Anclar(RectTransform r, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = amin; r.anchorMax = amax; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
        }

        static TMP_Text Txt(Transform padre, string nombre, string texto, float tam, Color color,
                            TextAlignmentOptions al = TextAlignmentOptions.TopLeft, bool negrita = false)
        {
            var rt = Rt(nombre, padre);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = texto; t.fontSize = tam; t.color = color; t.alignment = al;
            t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            return t;
        }

        static Button Btn(Transform padre, string nombre, string etiqueta, Color fondo, float alto = 140f, float tam = 44f,
                          TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var rt = Rt(nombre, padre);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = fondo;
            var b = rt.gameObject.AddComponent<Button>();
            rt.gameObject.AddComponent<PressScale>();
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f); cb.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
            b.colors = cb; b.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = alto; le.preferredHeight = alto;
            var t = Txt(rt, "Etiqueta", etiqueta, tam, Texto, al, true);
            Estirar((RectTransform)t.transform, 24, 8, 24, 8);
            return b;
        }

        // Botón con borde grueso y sombra dura, como el arte de las placas.
        static Button BtnPlaca(Transform padre, string nombre, string etiqueta, Color relleno, Color texto, Color borde, Color sombra,
                               float alto = 130f, float tam = 46f, RolTema? rolRelleno = null, RolTema? rolSombra = null)
        {
            var rt = Rt(nombre, padre);
            var img = rt.gameObject.AddComponent<Image>(); img.color = borde;
            var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = sombra; sh.effectDistance = new Vector2(10, -10); sh.useGraphicAlpha = false;
            if (rolSombra.HasValue) rt.gameObject.AddComponent<TemaElemento>().rol = rolSombra.Value;
            var b = rt.gameObject.AddComponent<Button>();
            rt.gameObject.AddComponent<PressScale>();
            var cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            b.colors = cb; b.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.minHeight = alto; le.preferredHeight = alto;
            var fill = Rt("Relleno", rt); Estirar(fill, 7, 7, 7, 7);
            var fi = fill.gameObject.AddComponent<Image>(); fi.color = relleno; fi.raycastTarget = false;
            if (rolRelleno.HasValue) fill.gameObject.AddComponent<TemaElemento>().rol = rolRelleno.Value;
            var t = Txt(fill, "Etiqueta", etiqueta, tam, texto, TextAlignmentOptions.Center, true);
            Estirar((RectTransform)t.transform, 24, 6, 24, 6);
            return b;
        }

        // Recolorea por facultad un panel ya construido: fondo oscuro de la facultad y botones en su color.
        static void TemarPanelEstacion(GameObject panel)
        {
            if (panel == null) return;
            var bg = panel.GetComponent<Image>();
            if (bg != null) { var te = panel.AddComponent<TemaElemento>(); te.rol = RolTema.FondoOscuro; }
            foreach (var b in panel.GetComponentsInChildren<Button>(true))
            {
                var im = b.GetComponent<Image>();
                if (im != null && b.GetComponent<TemaElemento>() == null) b.gameObject.AddComponent<TemaElemento>().rol = RolTema.Acento;
                var et = b.transform.Find("Etiqueta");
                if (et != null && et.GetComponent<TemaElemento>() == null && et.GetComponent<TMP_Text>() != null) et.gameObject.AddComponent<TemaElemento>().rol = RolTema.Tinta;
            }
        }

        static RectTransform Panel(Transform padre, string nombre, bool opaco = true, Color? color = null)
        {
            var rt = Rt(nombre, padre);
            Estirar(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = opaco ? (color ?? Fondo) : new Color(0, 0, 0, 0);
            img.raycastTarget = opaco;
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(48, 48, 48, 48); v.spacing = 24;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return rt;
        }

        static TMP_Text Titulo(Transform padre, string texto, float tam = 68f)
        {
            var t = Txt(padre, "Titulo", texto, tam, Texto, TextAlignmentOptions.Center, true);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.minHeight = tam * 1.5f;
            return t;
        }

        // Zona desplazable: devuelve el contenido (VerticalLayoutGroup).
        static RectTransform Scroll(Transform padre, out ScrollRect sr)
        {
            var rt = Rt("Scroll", padre);
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.flexibleHeight = 1; le.minHeight = 300;
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = new Color(1, 1, 1, 0.04f);
            sr = rt.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 40;
            var vp = Rt("Viewport", rt); Estirar(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            var ct = Rt("Contenido", vp);
            ct.anchorMin = new Vector2(0, 1); ct.anchorMax = new Vector2(1, 1); ct.pivot = new Vector2(0.5f, 1);
            ct.offsetMin = Vector2.zero; ct.offsetMax = Vector2.zero;
            var v = ct.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 24, 24); v.spacing = 20;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            ct.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp; sr.content = ct;
            return ct;
        }

        static void Al(Button b, UnityAction a) { UnityEventTools.AddPersistentListener(b.onClick, a); }
        static void AlInt(Button b, UnityAction<int> a, int v) { UnityEventTools.AddIntPersistentListener(b.onClick, a, v); }

        // ---------------------------------------------------------------- escenas
        static string ConstruirEscena(bool ar, StationCatalog catalogo)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Cámara
            var camGo = new GameObject(ar ? "ARCamera" : "Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ar ? Color.black : Fondo;
            cam.nearClipPlane = ar ? 0.01f : 0.3f; cam.farClipPlane = ar ? 100f : 1000f;
            camGo.AddComponent<AudioListener>();
            if (ar) camGo.AddComponent<VuforiaBehaviour>();

            if (ar)
            {
                var luz = new GameObject("Directional Light").AddComponent<Light>();
                luz.type = LightType.Directional; luz.intensity = 1.2f;
                luz.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Canvas y EventSystem
            var cgo = new GameObject(ar ? "CanvasAR" : "CanvasInicio", typeof(RectTransform));
            var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920); sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            cgo.AddComponent<CanvasFit>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var mod = es.AddComponent<InputSystemUIInputModule>();
            mod.AssignDefaultActions();     // sin esto los botones no responden al clic/toque

            var safe = Rt("AreaSegura", cgo.transform); Estirar(safe);
            safe.gameObject.AddComponent<SafeArea>();

            var appRoot = new GameObject("AppRoot");
            var flow = appRoot.AddComponent<AppFlowController>();
            flow.catalogo = catalogo;

            // ---- pantallas comunes
            var panelAyuda = ConstruirAyuda(safe, flow, ar);
            var panelFac = ConstruirFacultades(safe, flow, catalogo);
            var info = ConstruirInformacion(safe, flow, out var infoCtl);
            flow.panelInfoHub = ConstruirInfoHub(safe, flow);
            var reto = ConstruirReto(safe, flow, out var quizCtl);
            var rec = ConstruirRecorrido(safe, flow, catalogo, out var progCtl);
            var insignia = ConstruirInsignia(safe, flow, catalogo);
            var conf = ConstruirConfirmacion(safe, flow);
            TemarPanelEstacion(info); TemarPanelEstacion(reto); TemarPanelEstacion(insignia);
            GameObject menuEst = ar ? ConstruirMenuEstacion(safe, flow) : null;

            GameObject raiz;
            ARStationController arCtl = null;
            if (!ar) raiz = ConstruirInicioRaiz(safe, flow);
            else raiz = ConstruirHudAR(safe, flow, catalogo, out arCtl);

            // Aviso (toast) siempre arriba
            var toast = Rt("Aviso", safe);
            Anclar(toast, new Vector2(0.05f, 0), new Vector2(0.95f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(0, 150));
            var tImg = toast.gameObject.AddComponent<Image>(); tImg.color = new Color(0, 0, 0, 0.88f); tImg.raycastTarget = false;
            var tTxt = Txt(toast, "Texto", "", 38, Texto, TextAlignmentOptions.Center);
            Estirar((RectTransform)tTxt.transform, 24, 8, 24, 8);
            toast.gameObject.SetActive(false);

            flow.panelRaiz = raiz; flow.panelAyuda = panelAyuda; flow.panelFacultades = panelFac; flow.panelInformacion = info;
            flow.panelMenu = menuEst; flow.panelReto = reto; flow.panelRecorrido = rec; flow.panelInsignia = insignia; flow.panelConfirmar = conf;
            flow.info = infoCtl; flow.quiz = quizCtl; flow.progreso = progCtl; flow.aviso = tTxt;

            if (ar)
            {
                var tr = new GameObject("TargetsRoot").transform;
                arCtl.flow = flow; arCtl.catalogo = catalogo; arCtl.targetsRoot = tr;
            }

            string ruta = RutaEscenas + "/" + (ar ? "ExploracionAR" : "Inicio") + ".unity";
            EditorSceneManager.SaveScene(escena, ruta);
            return ruta;
        }

        // ---- Inicio
        static GameObject ConstruirInicioRaiz(Transform safe, AppFlowController flow)
        {
            // Inicio = fusión de las cuatro facultades: franja de 4 colores, las 4 placas y botones con sus colores.
            var azul = Estaciones[0].color; var verde = Estaciones[1].color; var rojo = Estaciones[2].color; var amarillo = Estaciones[3].color;
            var negro = new Color(0.04f, 0.04f, 0.05f, 1f);
            var p = Panel(safe, "PanelInicio", true, negro);
            var v = p.GetComponent<VerticalLayoutGroup>(); v.childAlignment = TextAnchor.MiddleCenter; v.spacing = 26;
            v.padding = new RectOffset(48, 48, 96, 96);

            foreach (var arriba in new[] { true, false })
            {
                var fr = Rt(arriba ? "FranjaSuperior" : "FranjaInferior", p);
                fr.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Anclar(fr, new Vector2(0, arriba ? 1 : 0), new Vector2(1, arriba ? 1 : 0), new Vector2(0.5f, arriba ? 1 : 0), Vector2.zero, new Vector2(0, 40));
                var h = fr.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
                foreach (var c in new[] { azul, rojo, verde, amarillo })
                {
                    var bloque = Rt("Color", fr); var bi = bloque.gameObject.AddComponent<Image>(); bi.color = c; bi.raycastTarget = false;
                }
            }

            var t = Txt(p, "Titulo", "CAMPUS AR\nGUARDIANS", 104, Color.white, TextAlignmentOptions.Center, true);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 270;
            var s = Txt(p, "Subtitulo", "Cuatro mascotas. Una facultad cada una.", 42, new Color(0.85f, 0.88f, 0.95f), TextAlignmentOptions.Center);
            s.gameObject.AddComponent<LayoutElement>().minHeight = 70;

            // Rejilla 2×2 con la placa de cada facultad
            var rej = Rt("Placas", p);
            rej.gameObject.AddComponent<LayoutElement>().minHeight = 700;
            var gl = rej.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(470, 336); gl.spacing = new Vector2(24, 24); gl.childAlignment = TextAnchor.MiddleCenter;
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 2;
            foreach (var e in new[] { Estaciones[0], Estaciones[2], Estaciones[1], Estaciones[3] })
            {
                var cel = Rt("Placa_" + e.mascota, rej);
                var marco = cel.gameObject.AddComponent<Image>(); marco.color = e.color; marco.raycastTarget = false;
                var dentro = Rt("Arte", cel); Estirar(dentro, 8, 8, 8, 8);
                var ai = dentro.gameObject.AddComponent<Image>(); ai.preserveAspect = true; ai.raycastTarget = false;
                if (Ilus.ContainsKey(e.id)) ai.sprite = Ilus[e.id];
            }

            var negroBorde = Color.white;
            Al(BtnPlaca(p, "BtnExplorar", "EXPLORAR EN AR", amarillo, new Color(0.05f, 0.05f, 0.05f), negroBorde, azul, 150, 52), flow.IrAExploracion);
            Al(BtnPlaca(p, "BtnRecorrido", "MI RECORRIDO", Color.white, new Color(0.05f, 0.05f, 0.05f), negroBorde, verde, 130, 46), flow.AbrirRecorrido);
            Al(BtnPlaca(p, "BtnAyuda", "AYUDA", Color.white, new Color(0.05f, 0.05f, 0.05f), negroBorde, rojo, 130, 46), flow.AbrirAyuda);
            return p.gameObject;
        }

        // ---- HUD del escáner (AR)
        static GameObject ConstruirHudAR(Transform safe, AppFlowController flow, StationCatalog cat, out ARStationController ctl)
        {
            var hud = Rt("HudEscaner", safe); Estirar(hud);

            // barra superior
            var barra = Rt("BarraSuperior", hud);
            Anclar(barra, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 360));
            var bImg = barra.gameObject.AddComponent<Image>(); bImg.color = new Color(0, 0, 0, 0.55f);
            var fila = Rt("Botones", barra);
            Anclar(fila, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(-32, 130));
            var h = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            Al(Btn(fila, "BtnInicio", "Inicio", BotonSec, 130, 34), flow.IrAInicio);
            Al(Btn(fila, "BtnRecorrido", "Mi recorrido", BotonSec, 130, 34), flow.AbrirRecorrido);
            Al(Btn(fila, "BtnAyuda", "Ayuda", BotonSec, 130, 34), flow.AbrirAyuda);
            Al(Btn(fila, "BtnSonido", "Sonido", BotonSec, 130, 34), flow.AlternarSonido);
            var estado = Txt(barra, "Estado", "Buscando…", 38, Texto, TextAlignmentOptions.Center, true);
            Anclar((RectTransform)estado.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-48, 190));

            // guía de escaneo
            var guia = Rt("GuiaEscaneo", hud);
            Anclar(guia, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(840, 600));
            var gImg = guia.gameObject.AddComponent<Image>(); gImg.color = new Color(1, 1, 1, 0.08f); gImg.raycastTarget = false;
            var og = guia.gameObject.AddComponent<Outline>(); og.effectColor = new Color(1, 1, 1, 0.9f); og.effectDistance = new Vector2(8, -8);
            var gt = Txt(guia, "Texto", "Apunta a la imagen de la placa\n(no al código QR)", 44, Texto, TextAlignmentOptions.Center, true);
            Estirar((RectTransform)gt.transform, 30, 30, 30, 30);

            // barra inferior de la estación detectada (el menú completo es una pantalla aparte)
            var tar = Rt("PanelEstacion", hud);
            Anclar(tar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 210));
            var tImg = tar.gameObject.AddComponent<Image>(); tImg.color = Tarjeta;
            var th = tar.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.padding = new RectOffset(40, 40, 30, 30); th.spacing = 24; th.childAlignment = TextAnchor.MiddleCenter;
            th.childControlWidth = true; th.childControlHeight = true; th.childForceExpandWidth = false; th.childForceExpandHeight = true;
            var tt = Txt(tar, "Titulo", "Estación", 44, Texto, TextAlignmentOptions.MidlineLeft, true);
            tt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var bm = Btn(tar, "BtnMenu", "Abrir menú", Boton, 150, 40);
            bm.GetComponent<LayoutElement>().minWidth = 360; bm.GetComponent<LayoutElement>().preferredWidth = 360;
            Al(bm, flow.AbrirMenuEstacion);
            tar.gameObject.SetActive(false);

            // acciones de la mascota: Girar / Caminar / Info (solo con mascota visible)
            var acc = Rt("PanelAcciones", hud);
            Anclar(acc, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 225), new Vector2(-48, 140));
            var ah = acc.gameObject.AddComponent<HorizontalLayoutGroup>();
            ah.spacing = 16; ah.childControlWidth = true; ah.childControlHeight = true; ah.childForceExpandWidth = true; ah.childForceExpandHeight = true;
            var bGirar = Btn(acc, "BtnGirar", "Girar", Boton, 140, 38);
            var bBailar = Btn(acc, "BtnBailar", "Bailar", new Color(0.62f, 0.25f, 0.80f, 1f), 140, 38);
            var bGolpe = Btn(acc, "BtnGolpear", "Golpear", new Color(0.85f, 0.22f, 0.22f, 1f), 140, 38);
            var bEscribe = Btn(acc, "BtnEscribir", "Escribir", new Color(0.15f, 0.55f, 0.75f, 1f), 140, 38);
            var bInfo = Btn(acc, "BtnInfo", "Info", new Color(0.95f, 0.65f, 0.10f, 1f), 140, 38);
            acc.gameObject.SetActive(false);

            // burbuja de información (se cierra al tocarla)
            var bub = Btn(hud, "Burbuja", "", Tarjeta, 330, 38, TextAlignmentOptions.MidlineLeft);
            Anclar((RectTransform)bub.transform, new Vector2(0.04f, 0), new Vector2(0.96f, 0), new Vector2(0.5f, 0), new Vector2(0, 385), new Vector2(0, 340));
            var bubTxt = bub.transform.Find("Etiqueta").GetComponent<TMP_Text>();
            bubTxt.fontStyle = FontStyles.Normal; bubTxt.fontSize = 40; bubTxt.alignment = TextAlignmentOptions.MidlineLeft;
            var bubOut = bub.gameObject.AddComponent<Outline>(); bubOut.effectColor = new Color(1f, 0.82f, 0.2f, 1f); bubOut.effectDistance = new Vector2(6, -6);
            bub.gameObject.SetActive(false);

            // palanca para caminar (siempre visible con la mascota)
            var jb = Rt("Joystick", hud);
            Anclar(jb, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(50, 400), new Vector2(340, 340));
            var jbImg = jb.gameObject.AddComponent<Image>(); jbImg.sprite = JoystickVirtual.Circulo(); jbImg.color = new Color(1f, 1f, 1f, 0.28f);
            var jh = Rt("Mango", jb);
            Anclar(jh, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var jhImg = jh.gameObject.AddComponent<Image>(); jhImg.sprite = JoystickVirtual.Circulo(); jhImg.color = new Color(1f, 1f, 1f, 0.85f); jhImg.raycastTarget = false;
            var joy = jb.gameObject.AddComponent<JoystickVirtual>(); joy.baseRt = jb; joy.mango = jh;
            jb.gameObject.SetActive(false);

            // ¿no reconoce?
            var nr = Rt("PanelNoReconoce", hud);
            Anclar(nr, new Vector2(0.05f, 0), new Vector2(0.95f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(0, 420));
            var nImg = nr.gameObject.AddComponent<Image>(); nImg.color = new Color(0.30f, 0.12f, 0.12f, 0.95f);
            var nv = nr.gameObject.AddComponent<VerticalLayoutGroup>();
            nv.padding = new RectOffset(32, 32, 24, 24); nv.spacing = 14;
            nv.childControlWidth = true; nv.childControlHeight = true; nv.childForceExpandWidth = true; nv.childForceExpandHeight = false;
            var nt = Txt(nr, "Texto", "¿No reconoce la placa? Acércate a 30–60 cm, ilumina la imagen completa y evita reflejos. Mientras tanto puedes consultar la información sin cámara.", 34, Texto, TextAlignmentOptions.Center);
            nt.gameObject.AddComponent<LayoutElement>().minHeight = 170;
            var nf = Rt("Botones", nr); nf.gameObject.AddComponent<LayoutElement>().minHeight = 110;
            var nh = nf.gameObject.AddComponent<HorizontalLayoutGroup>(); nh.spacing = 16;
            nh.childControlWidth = true; nh.childControlHeight = true; nh.childForceExpandWidth = true; nh.childForceExpandHeight = true;
            Al(Btn(nf, "BtnConsejos", "Ver consejos", BotonSec, 110, 34), flow.AbrirAyuda);
            Al(Btn(nf, "BtnSinCamara", "Información sin cámara", Boton, 110, 34), flow.AbrirFacultades);
            nr.gameObject.SetActive(false);

            // permiso de cámara rechazado (RF02)
            var pp = Panel(safe, "PanelPermiso", true, new Color(0.05f, 0.07f, 0.12f, 0.98f));
            pp.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Titulo(pp, "Se necesita la cámara", 60);
            var pt = Txt(pp, "Texto",
                "Campus AR Guardians usa la cámara solo para reconocer la imagen de la placa. No guarda fotos ni videos.\n\n" +
                "Si la rechazaste: abre Ajustes del teléfono > Aplicaciones > Campus AR Guardians > Permisos > Cámara y actívala. Luego pulsa Reintentar.\n\n" +
                "También puedes consultar la información desde Inicio sin usar la cámara.", 40, Texto, TextAlignmentOptions.Center);
            pt.gameObject.AddComponent<LayoutElement>().minHeight = 520;
            Al(Btn(pp, "BtnReintentar", "Reintentar", Boton, 150), flow.CargarAR);
            Al(Btn(pp, "BtnInfoSinCamara", "Ver información sin cámara", BotonSec, 150), flow.AbrirFacultades);
            Al(Btn(pp, "BtnInicio", "Volver a Inicio", BotonSec, 150), flow.IrAInicio);
            pp.gameObject.SetActive(false);
            pp.SetAsLastSibling();

            var ctlGo = new GameObject("ARStationController");
            ctl = ctlGo.AddComponent<ARStationController>();
            ctl.estado = estado; ctl.guiaEscaneo = guia.gameObject; ctl.tarjetaEstacion = tar.gameObject; ctl.tarjetaTitulo = tt;
            ctl.panelNoReconoce = nr.gameObject; ctl.panelPermiso = pp.gameObject;
            ctl.usarModelTargets = false;        // el tótem queda fuera: el disparador es la placa
            ctl.panelAcciones = acc.gameObject; ctl.burbuja = bub.gameObject; ctl.burbujaTexto = bubTxt;
            ctl.etiquetaGirar = bGirar.transform.Find("Etiqueta").GetComponent<TMP_Text>();
            ctl.etiquetaBailar = bBailar.transform.Find("Etiqueta").GetComponent<TMP_Text>();
            ctl.joystick = joy;
            Al(bGirar, ctl.GirarMascota); Al(bBailar, ctl.BailarMascota); Al(bGolpe, ctl.GolpearMascota); Al(bEscribe, ctl.EscribirMascota); Al(bInfo, ctl.InfoMascota); Al(bub, ctl.CerrarBurbuja);
            return hud.gameObject;
        }

        // ---- Menú de la estación (pantalla completa, se despliega al detectar la placa)
        static GameObject ConstruirMenuEstacion(Transform safe, AppFlowController flow)
        {
            // Menú a pantalla completa con el color y el arte de la facultad detectada.
            var p = Panel(safe, "PanelMenuEstacion", true, Boton);
            p.gameObject.AddComponent<TemaElemento>().rol = RolTema.Fondo;
            var v = p.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(48, 48, 70, 48); v.spacing = 24;

            // Figuras decorativas (detrás del contenido), en el tono oscuro de la facultad
            var deco = Rt("Decoracion", p); deco.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; Estirar(deco);
            deco.SetAsFirstSibling();
            var figuras = new[] {
                new Vector4(0.95f, 0.92f, 520, 45), new Vector4(0.05f, 0.30f, 360, 30), new Vector4(0.9f, 0.12f, 420, 15), new Vector4(0.1f, 0.78f, 220, 60) };
            foreach (var f in figuras)
            {
                var fr = Rt("Figura", deco);
                Anclar(fr, new Vector2(f.x, f.y), new Vector2(f.x, f.y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(f.z, f.z));
                fr.localRotation = Quaternion.Euler(0, 0, f.w);
                var fi = fr.gameObject.AddComponent<Image>(); fi.color = new Color(0, 0, 0, 0.22f); fi.raycastTarget = false;
                fr.gameObject.AddComponent<TemaElemento>().rol = RolTema.FondoOscuro;
            }
            var acentoRt = Rt("Acento", p);
            var acento = acentoRt.gameObject.AddComponent<Image>(); acento.color = Color.black; acento.raycastTarget = false;
            acentoRt.gameObject.AddComponent<LayoutElement>().minHeight = 16;
            flow.menuAcento = acento;
            acentoRt.gameObject.SetActive(false);

            // Arte de la placa en marco negro con sombra dura
            var marco = Rt("Marco", p);
            var mi = marco.gameObject.AddComponent<Image>(); mi.color = Color.black; mi.raycastTarget = false;
            var sh = marco.gameObject.AddComponent<Shadow>(); sh.effectDistance = new Vector2(14, -14); sh.useGraphicAlpha = false;
            marco.gameObject.AddComponent<TemaElemento>().rol = RolTema.Sombra;
            marco.gameObject.AddComponent<LayoutElement>().minHeight = 560;
            marco.GetComponent<LayoutElement>().preferredHeight = 560;
            var arte = Rt("Ilustracion", marco); Estirar(arte, 10, 10, 10, 10);
            var ai = arte.gameObject.AddComponent<Image>(); ai.preserveAspect = true; ai.raycastTarget = false;
            flow.menuIlustracion = ai;

            flow.menuTitulo = Txt(p, "Titulo", "Estación", 84, Texto, TextAlignmentOptions.Center, true);
            flow.menuTitulo.gameObject.AddComponent<LayoutElement>().minHeight = 120;
            flow.menuTitulo.gameObject.AddComponent<TemaElemento>().rol = RolTema.Tinta;
            flow.menuTitulo.enableAutoSizing = true; flow.menuTitulo.fontSizeMin = 48; flow.menuTitulo.fontSizeMax = 84;
            flow.menuSubtitulo = Txt(p, "Subtitulo", "", 38, Texto, TextAlignmentOptions.Center);
            flow.menuSubtitulo.gameObject.AddComponent<LayoutElement>().minHeight = 110;
            flow.menuSubtitulo.gameObject.AddComponent<TemaElemento>().rol = RolTema.Tinta;

            var lista = Rt("Opciones", p);
            lista.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var lv = lista.gameObject.AddComponent<VerticalLayoutGroup>(); lv.spacing = 26; lv.childAlignment = TextAnchor.MiddleCenter;
            lv.padding = new RectOffset(0, 14, 0, 14);
            lv.childControlWidth = true; lv.childControlHeight = true; lv.childForceExpandWidth = true; lv.childForceExpandHeight = false;
            lista.gameObject.AddComponent<EntradaEscalonada>();
            var tinta = new Color(0.05f, 0.05f, 0.05f);
            Al(BtnPlaca(lista, "BtnInformacion", "INFORMACIÓN", Color.white, tinta, Color.black, Color.black, 150, 50, null, RolTema.Sombra), flow.AbrirInformacion);
            // Mapa y cómo llegar: fuera por ahora (decisión del equipo).
            Al(BtnPlaca(lista, "BtnReto", "MINI RETO · GANA TU SELLO", Boton, Color.white, Color.black, Color.black, 140, 44, RolTema.FondoOscuro, RolTema.Sombra), flow.AbrirReto);

            var vol = BtnPlaca(p, "BtnVolver", "VER LA MASCOTA EN AR", Color.white, tinta, Color.black, Color.black, 112, 38, null, RolTema.Sombra);
            Al(vol, flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Información: 3 botones (Facultad, Carreras, Atención); cada uno abre su pantalla grande
        static GameObject ConstruirInfoHub(Transform safe, AppFlowController flow)
        {
            var p = Panel(safe, "PanelInfoHub", true, Boton);
            p.gameObject.AddComponent<TemaElemento>().rol = RolTema.Fondo;
            var v = p.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(48, 48, 70, 48); v.spacing = 26;

            var deco = Rt("Decoracion", p); deco.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; Estirar(deco);
            deco.SetAsFirstSibling();
            foreach (var f in new[] { new Vector4(0.95f, 0.95f, 520, 45), new Vector4(0.04f, 0.45f, 340, 30), new Vector4(0.92f, 0.10f, 440, 15) })
            {
                var fr = Rt("Figura", deco);
                Anclar(fr, new Vector2(f.x, f.y), new Vector2(f.x, f.y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(f.z, f.z));
                fr.localRotation = Quaternion.Euler(0, 0, f.w);
                var fi = fr.gameObject.AddComponent<Image>(); fi.color = new Color(0, 0, 0, 0.22f); fi.raycastTarget = false;
                fr.gameObject.AddComponent<TemaElemento>().rol = RolTema.FondoOscuro;
            }

            var marco = Rt("Marco", p);
            var mi = marco.gameObject.AddComponent<Image>(); mi.color = Color.black; mi.raycastTarget = false;
            var sh = marco.gameObject.AddComponent<Shadow>(); sh.effectDistance = new Vector2(12, -12); sh.useGraphicAlpha = false;
            marco.gameObject.AddComponent<TemaElemento>().rol = RolTema.Sombra;
            var le = marco.gameObject.AddComponent<LayoutElement>(); le.minHeight = 400; le.preferredHeight = 400;
            var arte = Rt("Ilustracion", marco); Estirar(arte, 10, 10, 10, 10);
            var ai = arte.gameObject.AddComponent<Image>(); ai.preserveAspect = true; ai.raycastTarget = false;
            flow.hubIlustracion = ai;

            flow.hubTitulo = Txt(p, "Titulo", "INFORMACIÓN", 76, Texto, TextAlignmentOptions.Center, true);
            flow.hubTitulo.gameObject.AddComponent<LayoutElement>().minHeight = 110;
            flow.hubTitulo.gameObject.AddComponent<TemaElemento>().rol = RolTema.Tinta;
            flow.hubSubtitulo = Txt(p, "Subtitulo", "", 40, Texto, TextAlignmentOptions.Center);
            flow.hubSubtitulo.gameObject.AddComponent<LayoutElement>().minHeight = 70;
            flow.hubSubtitulo.gameObject.AddComponent<TemaElemento>().rol = RolTema.Tinta;

            var lista = Rt("Opciones", p);
            lista.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var lv = lista.gameObject.AddComponent<VerticalLayoutGroup>(); lv.spacing = 30; lv.childAlignment = TextAnchor.MiddleCenter;
            lv.padding = new RectOffset(0, 14, 0, 14);
            lv.childControlWidth = true; lv.childControlHeight = true; lv.childForceExpandWidth = true; lv.childForceExpandHeight = false;
            lista.gameObject.AddComponent<EntradaEscalonada>();
            var tinta = new Color(0.05f, 0.05f, 0.05f);
            Al(BtnPlaca(lista, "BtnFacultad", "FACULTAD", Color.white, tinta, Color.black, Color.black, 170, 54, null, RolTema.Sombra), flow.AbrirFacultad);
            Al(BtnPlaca(lista, "BtnCarreras", "CARRERAS", Color.white, tinta, Color.black, Color.black, 170, 54, null, RolTema.Sombra), flow.AbrirCarreras);
            Al(BtnPlaca(lista, "BtnAtencion", "ATENCIÓN AL VISITANTE", Color.white, tinta, Color.black, Color.black, 170, 46, null, RolTema.Sombra), flow.AbrirAtencion);

            Al(BtnPlaca(p, "BtnVolver", "VOLVER", Color.white, tinta, Color.black, Color.black, 120, 42, null, RolTema.Sombra), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Ayuda
        static GameObject ConstruirAyuda(Transform safe, AppFlowController flow, bool ar)
        {
            var p = Panel(safe, "PanelAyuda");
            Titulo(p, "Ayuda", 70);
            var ct = Scroll(p, out _);
            Txt(ct, "Texto",
                "<b>QR e imagen AR no son lo mismo</b>\n" +
                "• El código QR de la placa abre información básica en el navegador del teléfono, sin esta app.\n" +
                "• La imagen grande de la placa, con la mascota, es la que reconoce esta app con la cámara.\n\n" +
                "<b>Cómo usar la cámara</b>\n" +
                "1. Pulsa Explorar en AR. Si el teléfono pide acceso a la cámara, acéptalo: solo se usa para reconocer la placa.\n" +
                "2. Apunta a la imagen de la placa, completa dentro de la pantalla, a unos 30–60 cm.\n" +
                "3. Usa buena luz y evita reflejos o sombras sobre la imagen.\n" +
                "4. Si pierdes la imagen, vuelve a enfocarla: la mascota regresa y lo que tengas abierto sigue disponible.\n\n" +
                "<b>Sellos e insignia</b>\n" +
                "Responde bien el mini reto de cada estación para obtener su sello. Con los 4 sellos se desbloquea la insignia final.", 42, Texto);
            if (ar) Al(Btn(p, "BtnVolver", "Entendido, volver", Boton, 150), flow.Atras);
            else
            {
                Al(Btn(p, "BtnAbrir", "Entendido, abrir la cámara", Boton, 150), flow.AyudaEntendidaYAbrirCamara);
                Al(Btn(p, "BtnVolver", "Volver", BotonSec, 140), flow.Atras);
            }
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Lista de facultades (acceso sin cámara, RF14)
        static GameObject ConstruirFacultades(Transform safe, AppFlowController flow, StationCatalog cat)
        {
            var p = Panel(safe, "PanelFacultades");
            Titulo(p, "Información de facultades", 62);
            var s = Txt(p, "Sub", "Elige una estación. No necesitas la cámara.", 40, TextoSuave, TextAlignmentOptions.Center);
            s.gameObject.AddComponent<LayoutElement>().minHeight = 70;
            var ct = Scroll(p, out _);
            for (int i = 0; i < cat.estaciones.Length; i++)
            {
                var e = cat.estaciones[i];
                var b = Btn(ct, "Btn_" + e.stationId, e.displayName + "  ·  " + e.mascotaNombre, BotonSec, 160, 46);
                AlInt(b, flow.AbrirInfoDeIndice, i);
            }
            Al(Btn(p, "BtnVolver", "Volver", Boton, 140), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Información
        static GameObject ConstruirInformacion(Transform safe, AppFlowController flow, out InformationPanelController ctl)
        {
            var p = Panel(safe, "PanelInformacion");
            ctl = p.gameObject.AddComponent<InformationPanelController>();
            ctl.titulo = Titulo(p, "Sección", 84);
            ctl.subtitulo = Txt(p, "Subtitulo", "", 40, TextoSuave, TextAlignmentOptions.Center);
            ctl.subtitulo.gameObject.AddComponent<LayoutElement>().minHeight = 64;

            ScrollRect sr;
            var ct = Scroll(p, out sr);
            ctl.scroll = sr;
            ctl.cuerpo = Txt(ct, "Cuerpo", "", 50, Texto);

            var imgRt = Rt("ImagenMapa", ct);
            var img = imgRt.gameObject.AddComponent<Image>(); img.preserveAspect = true; img.raycastTarget = false;
            var le = imgRt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 700; le.minHeight = 400;
            imgRt.gameObject.SetActive(false);
            ctl.imagenMapa = img;

            var enl = Rt("Enlaces", ct);
            var ev = enl.gameObject.AddComponent<VerticalLayoutGroup>(); ev.spacing = 14;
            ev.childControlWidth = true; ev.childControlHeight = true; ev.childForceExpandWidth = true; ev.childForceExpandHeight = false;
            ctl.contenedorEnlaces = enl;

            var plantilla = Btn(p, "PlantillaEnlace", "Enlace", Boton, 140, 42);
            plantilla.transform.SetParent(p, false);
            plantilla.gameObject.SetActive(false);
            ctl.plantillaEnlace = plantilla;

            var pt = Txt(p, "PlantillaTitulo", "Carrera", 46, Texto, TextAlignmentOptions.TopLeft, true);
            pt.gameObject.SetActive(false);
            ctl.plantillaTitulo = pt;

            Al(Btn(p, "BtnVolver", "Volver", Boton, 140), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Reto
        static GameObject ConstruirReto(Transform safe, AppFlowController flow, out QuizController ctl)
        {
            var p = Panel(safe, "PanelReto");
            ctl = p.gameObject.AddComponent<QuizController>();
            ctl.titulo = Titulo(p, "Mini reto", 58);
            var ct = Scroll(p, out _);
            var ig = Rt("Insignia", ct);
            var igImg = ig.gameObject.AddComponent<Image>(); igImg.preserveAspect = true; igImg.raycastTarget = false;
            var igLe = ig.gameObject.AddComponent<LayoutElement>(); igLe.minHeight = 280; igLe.preferredHeight = 280;
            ig.gameObject.AddComponent<PopIn>().respirar = false;
            ig.gameObject.GetComponent<PopIn>().enabled = false;
            ig.gameObject.SetActive(false);
            ctl.insigniaImg = igImg;
            ctl.estadoSello = Txt(ct, "EstadoSello", "", 36, TextoSuave);
            ctl.pregunta = Txt(ct, "Pregunta", "", 48, Texto, TextAlignmentOptions.TopLeft, true);

            ctl.opciones = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Btn(ct, "Opcion" + i, "", BotonSec, 170, 40, TextAlignmentOptions.MidlineLeft);
                ctl.opciones[i] = b;
                AlInt(b, ctl.Elegir, i);
            }
            ctl.confirmar = Btn(ct, "BtnConfirmar", "Confirmar respuesta", Boton, 140, 44);
            Al(ctl.confirmar, ctl.Confirmar);
            ctl.retroalimentacion = Txt(ct, "Retroalimentacion", "", 42, Texto);
            ctl.reintentar = Btn(ct, "BtnReintentar", "Intentar de nuevo", BotonSec, 140, 44);
            Al(ctl.reintentar, ctl.Reintentar);
            ctl.verRecorrido = Btn(ct, "BtnVerRecorrido", "Ver mi recorrido", Boton, 140, 44);
            Al(ctl.verRecorrido, flow.AbrirRecorrido);

            Al(Btn(p, "BtnVolver", "Volver", BotonSec, 140), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Mi recorrido
        static GameObject ConstruirRecorrido(Transform safe, AppFlowController flow, StationCatalog cat, out ProgressPanelController ctl)
        {
            var p = Panel(safe, "PanelRecorrido");
            ctl = p.gameObject.AddComponent<ProgressPanelController>();
            Titulo(p, "Mi recorrido", 66);
            ctl.contador = Txt(p, "Contador", "0 de 4 sellos", 64, Texto, TextAlignmentOptions.Center, true);
            ctl.contador.gameObject.AddComponent<LayoutElement>().minHeight = 100;
            var ct = Scroll(p, out _);
            ctl.filas = new TMP_Text[cat.estaciones.Length];
            ctl.iconos = new Image[cat.estaciones.Length];
            for (int i = 0; i < ctl.filas.Length; i++)
            {
                var fila = Rt("Fila" + i, ct);
                fila.gameObject.AddComponent<LayoutElement>().minHeight = 170;
                var fh = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
                fh.spacing = 24; fh.childAlignment = TextAnchor.MiddleLeft;
                fh.childControlWidth = true; fh.childControlHeight = true; fh.childForceExpandWidth = false; fh.childForceExpandHeight = false;
                var ic = Rt("Icono", fila);
                var icImg = ic.gameObject.AddComponent<Image>(); icImg.preserveAspect = true; icImg.raycastTarget = false;
                var icLe = ic.gameObject.AddComponent<LayoutElement>(); icLe.minWidth = 150; icLe.preferredWidth = 150; icLe.minHeight = 150; icLe.preferredHeight = 150;
                ctl.iconos[i] = icImg;
                var f = Txt(fila, "Texto", "", 44, Texto);
                f.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                ctl.filas[i] = f;
            }
            ctl.botonInsignia = Btn(p, "BtnInsignia", "Insignia bloqueada", Boton, 150, 42);
            Al(ctl.botonInsignia, flow.AbrirInsignia);
            Al(Btn(p, "BtnReiniciar", "Reiniciar recorrido", BotonSec, 130, 40), flow.PedirReinicio);
            Al(Btn(p, "BtnVolver", "Volver", BotonSec, 130), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Insignia
        static GameObject ConstruirInsignia(Transform safe, AppFlowController flow, StationCatalog cat)
        {
            var p = Panel(safe, "PanelInsignia");
            p.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Titulo(p, "¡Recorrido completado!", 80);
            var grid = Rt("Insignias", p);
            var gle = grid.gameObject.AddComponent<LayoutElement>(); gle.minHeight = 720; gle.preferredHeight = 720;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(330, 330); gl.spacing = new Vector2(30, 30); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 2;
            gl.childAlignment = TextAnchor.MiddleCenter;
            var fin = p.gameObject.AddComponent<InsigniaFinal>(); fin.catalogo = cat;
            fin.iconos = new Image[cat.estaciones.Length];
            for (int i = 0; i < fin.iconos.Length; i++)
            {
                var c = Rt("Insignia" + i, grid);
                var ci = c.gameObject.AddComponent<Image>(); ci.preserveAspect = true; ci.raycastTarget = false;
                c.gameObject.AddComponent<PopIn>();
                fin.iconos[i] = ci;
            }
            var t = Txt(p, "Texto",
                "Reuniste los 4 sellos: Lobo, Vikingo, Toro y León.\nGracias por conocer las facultades del campus.", 46, Texto, TextAlignmentOptions.Center);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 300;
            Al(Btn(p, "BtnVolver", "Volver", Boton, 150), flow.Atras);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        // ---- Confirmación de reinicio
        static GameObject ConstruirConfirmacion(Transform safe, AppFlowController flow)
        {
            var p = Panel(safe, "PanelConfirmar", true, new Color(0.04f, 0.06f, 0.11f, 0.98f));
            p.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Titulo(p, "¿Reiniciar tu recorrido?", 60);
            var t = Txt(p, "Texto", "Se borrarán los sellos guardados en este teléfono y el contador volverá a 0.", 46, Texto, TextAlignmentOptions.Center);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 240;
            Al(Btn(p, "BtnCancelar", "Cancelar", Boton, 150), flow.CancelarReinicio);
            Al(Btn(p, "BtnConfirmar", "Sí, reiniciar", BotonSec, 150), flow.ConfirmarReinicio);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }
    }
}
#endif
