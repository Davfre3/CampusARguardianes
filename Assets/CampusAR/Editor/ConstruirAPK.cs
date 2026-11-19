using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CampusAR.EditorTools
{
    /// <summary>Compila el APK de Android y guarda un informe con el resultado en Builds/informe_build.txt.</summary>
    public static class ConstruirAPK
    {
        [MenuItem("Campus AR/Construir APK (con informe)")]
        public static void Construir()
        {
            Directory.CreateDirectory("Builds");
            // Vuforia 11 exige Android 10 (API 29) o superior.
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            var sb = new StringBuilder();
            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            sb.AppendLine("Escenas: " + string.Join(", ", escenas));
            sb.AppendLine("Plataforma activa: " + EditorUserBuildSettings.activeBuildTarget);
            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = "Builds/CampusAR.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            try
            {
                BuildReport r = BuildPipeline.BuildPlayer(opciones);
                sb.AppendLine("Resultado: " + r.summary.result + " · errores: " + r.summary.totalErrors + " · avisos: " + r.summary.totalWarnings);
                foreach (var paso in r.steps)
                    foreach (var m in paso.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                            sb.AppendLine("[" + paso.name + "] " + m.type + ": " + m.content);
            }
            catch (System.Exception ex) { sb.AppendLine("EXCEPCIÓN: " + ex); }
            File.WriteAllText("Builds/informe_build.txt", sb.ToString());
            Debug.Log("[CampusAR] Informe de build guardado en Builds/informe_build.txt");
        }
    }
}
