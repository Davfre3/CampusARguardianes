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

                // 0) Rig hecho en Blender: un solo FBX con las acciones Idle/Caminar/Bailar (rig Generic)
                if (!File.Exists(d + "/Idle.fbx"))
                {
                    var ig = AssetImporter.GetAtPath(pPers) as ModelImporter;
                    ig.animationType = ModelImporterAnimationType.Generic;
                    ig.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    ig.importAnimation = true;
                    ig.SaveAndReimport();
                    var todos = AssetDatabase.LoadAllAssetsAtPath(pPers).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
                    AnimationClip Buscar(string n) { return todos.FirstOrDefault(c => c.name == n || c.name.EndsWith("|" + n) || c.name.Contains(n)); }
                    var cl2 = ig.defaultClipAnimations;
                    foreach (var c in cl2) { bool una = c.name.EndsWith("Golpear") || c.name.EndsWith("Escribir"); c.loopTime = !una; c.loopPose = !una; string n = c.name; int k = n.LastIndexOf('|'); if (k >= 0) c.name = n.Substring(k + 1); }
                    ig.clipAnimations = cl2; ig.SaveAndReimport();
                    todos = AssetDatabase.LoadAllAssetsAtPath(pPers).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
                    var gi = Buscar("Idle"); var gc = Buscar("Caminar"); var gb = Buscar("Bailar");
                    if (gi == null) { Debug.LogWarning("[Animaciones] " + id + ": el FBX no trae la acción Idle."); continue; }
                    string pc2 = d + "/Animator_" + id + ".controller";
                    AssetDatabase.DeleteAsset(pc2);
                    var ct2 = AnimatorController.CreateAnimatorControllerAtPath(pc2);
                    ct2.AddParameter("Caminar", AnimatorControllerParameterType.Bool);
                    ct2.AddParameter("Baile", AnimatorControllerParameterType.Bool);
                    ct2.AddParameter("Golpear", AnimatorControllerParameterType.Trigger);
                    ct2.AddParameter("Escribir", AnimatorControllerParameterType.Trigger);
                    var sm2 = ct2.layers[0].stateMachine;
                    var si = sm2.AddState("Idle"); si.motion = gi; sm2.defaultState = si;
                    if (gc != null) { var s = sm2.AddState("Caminar"); s.motion = gc; Enlazar(si, s, "Caminar", true); Enlazar(s, si, "Caminar", false); }
                    if (gb != null) { var s = sm2.AddState("Bailar"); s.motion = gb; Enlazar(si, s, "Baile", true); Enlazar(s, si, "Baile", false); }
                    foreach (var par in new[] { "Golpear", "Escribir" })
                    {
                        var cp = Buscar(par); if (cp == null) continue;
                        var sa = sm2.AddState(par); sa.motion = cp;
                        var ta = sm2.AddAnyStateTransition(sa); ta.hasExitTime = false; ta.duration = 0.1f; ta.canTransitionToSelf = false;
                        ta.AddCondition(AnimatorConditionMode.If, 0f, par);
                        var tb = sa.AddTransition(si); tb.hasExitTime = true; tb.exitTime = 0.95f; tb.duration = 0.15f;
                    }
                    EditorUtility.SetDirty(ct2); AssetDatabase.SaveAssets();
                    listas++;
                    Debug.Log("[Animaciones] " + id + ": listo con rig de Blender (" + pc2 + ")");
                    continue;
                }

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
