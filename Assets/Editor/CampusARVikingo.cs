#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vuforia;

// Menú: Campus AR > Preparar Image Target del Vikingo
// Toma el Image Target de la escena, le pone ARTargetHandler (ingenieria) y monta el modelo del Vikingo como hijo.
public static class CampusARVikingo
{
    [MenuItem("Campus AR/Preparar Image Target del Vikingo")]
    public static void Preparar()
    {
        var target = Object.FindAnyObjectByType<ImageTargetBehaviour>();
        if (target == null)
        {
            Debug.LogError("No hay Image Target en la escena. Crea uno con GameObject > Vuforia Engine > Image Target.");
            return;
        }

        var go = target.gameObject;
        go.name = "ImageTarget_Vikingo";

        // El ARTargetHandler reemplaza al handler por defecto
        var porDefecto = go.GetComponent<DefaultObserverEventHandler>();
        if (porDefecto != null) Object.DestroyImmediate(porDefecto);

        var handler = go.GetComponent<ARTargetHandler>();
        if (handler == null) handler = go.AddComponent<ARTargetHandler>();
        handler.facultyId = "ingenieria";

        // Modelo del Vikingo como hijo
        var existente = go.transform.Find("Techno_Viking");
        if (existente == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Techno_Viking_texture.fbx");
            if (prefab == null)
            {
                Debug.LogError("No se encontró Assets/Modelos/Techno_Viking_texture.fbx");
                return;
            }
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
            modelo.name = "Techno_Viking";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;

            // Escala: que mida ~0.5 de alto respecto al target (el target mide 1 unidad de ancho en local)
            var rends = modelo.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float alto = Mathf.Max(b.size.y, 0.0001f);
                modelo.transform.localScale = Vector3.one * (0.5f / alto) * (1f / Mathf.Max(go.transform.lossyScale.y, 0.0001f));
            }
            existente = modelo.transform;
        }
        handler.contenido3D = existente.gameObject;

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("Campus AR: ImageTarget_Vikingo listo (handler + modelo). Falta asignarle la imagen de la placa y la licencia de Vuforia.");
    }
}
#endif
