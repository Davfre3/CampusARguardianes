#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusAR.EditorTools
{
    // Prepara los FBX bajados de Mixamo y crea el Animator de cada mascota.
    // Carpeta por mascota: Assets/CampusAR/Animaciones/<id>/ con estos archivos:
    //   Personaje.fbx  (personaje con esqueleto, "With Skin", pose T)
    //   Idle.fbx       (animación en reposo)
    //   Caminar.fbx    (animación de caminar, con "In Place" activado)
    //   Bailar.fbx     (animación de baile)
    // <id> = admin (Lobo), ingenieria (Vikingo), economia (Toro), ciencias (León).
    // Después de ejecutar esto, vuelve a ejecutar «Campus AR > Construir proyecto».
    public static class AnimacionesMixamo
    {
        static readonly string[] Ids = { "admin", "ingenieria", "economia", "ciencias" };
        const string Raiz = "Assets/CampusAR/Animaciones";

        [MenuItem("Campus AR/Animaciones/Preparar animaciones de Mixamo")]
        public static void Preparar()
        {
            int listas = 0;
            foreach (var id in Ids)
            {
                string d = Raiz + "/" + id;
                string pPers = d + "/Personaje.fbx";
                if (!File.Exists(pPers)) { Debug.Log("[Animaciones] " + id + ": sin Personaje.fbx, se omite (usará la animación por código)."); continue; }

                // 1) personaje: rig humanoide, crea el avatar
                var ip = AssetImporter.GetAtPath(pPers) as ModelImporter;
                ip.animationType = ModelImporterAnimationType.Human;
                ip.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                ip.SaveAndReimport();
                var avatar = AssetDatabase.LoadAllAssetsAtPath(pPers).OfType<Avatar>().FirstOrDefault();
                if (avatar == null || !avatar.isHuman)
                {
                    Debug.LogWarning("[Animaciones] " + id + ": Unity no pudo crear un avatar humanoide con Personaje.fbx. Revisa el rig en Mixamo.");
                    continue;
                }

                // 2) clips: mismo avatar, en bucle
                var idle = Clip(d + "/Idle.fbx", avatar, "Idle");
                var camina = Clip(d + "/Caminar.fbx", avatar, "Caminar");
                var baila = Clip(d + "/Bailar.fbx", avatar, "Bailar");
                if (idle == null) { Debug.LogWarning("[Animaciones] " + id + ": falta Idle.fbx"); continue; }

                // 3) animator con tres estados
                string pc = d + "/Animator_" + id + ".controller";
                AssetDatabase.DeleteAsset(pc);
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(pc);
                ctrl.AddParameter("Caminar", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Baile", AnimatorControllerParameterType.Bool);
                var sm = ctrl.layers[0].stateMachine;
                var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
                sm.defaultState = sIdle;
                if (camina != null) { var s = sm.AddState("Caminar"); s.motion = camina; Enlazar(sIdle, s, "Caminar", true); Enlazar(s, sIdle, "Caminar", false); }
                if (baila != null) { var s = sm.AddState("Bailar"); s.motion = baila; Enlazar(sIdle, s, "Baile", true); Enlazar(s, sIdle, "Baile", false); }
                EditorUtility.SetDirty(ctrl);
                AssetDatabase.SaveAssets();
                listas++;
                Debug.Log("[Animaciones] " + id + ": listo (" + pc + ")");
            }
            AssetDatabase.Refresh();
            Debug.Log("[Animaciones] Mascotas preparadas: " + listas + ". Ahora ejecuta «Campus AR > Construir proyecto».");
        }

        static void Enlazar(AnimatorState de, AnimatorState a, string param, bool valor)
        {
            var t = de.AddTransition(a);
            t.hasExitTime = false; t.duration = 0.18f;
            t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
        }

        static AnimationClip Clip(string ruta, Avatar avatar, string nombre)
        {
            if (!File.Exists(ruta)) return null;
            var im = AssetImporter.GetAtPath(ruta) as ModelImporter;
            im.animationType = ModelImporterAnimationType.Human;
            im.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            im.sourceAvatar = avatar;
            var cl = im.defaultClipAnimations;
            foreach (var c in cl) { c.loopTime = true; c.loopPose = true; c.name = nombre; }
            im.clipAnimations = cl;
            im.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        }
    }
}
#endif
