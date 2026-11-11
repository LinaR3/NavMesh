#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class RunCitadelPhysicalReview
{
    const string ScenePath = "Assets/Scenes/CiudadelaMaya_AssetsPreview.unity";
    const string GuardKey = "CitadelPhysicalReview.AutoStarted";

    static RunCitadelPhysicalReview()
    {
        EditorApplication.delayCall += StartOnce;
    }

    static void StartOnce()
    {
        if (EditorApplication.isPlaying || SessionState.GetBool(GuardKey, false)) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene == null || scene.path != ScenePath) return;
        SessionState.SetBool(GuardKey, true);
        Debug.Log("[Auditoría Player] Iniciando Play Mode de revisión física en la escena aislada de la ciudadela.");
        EditorApplication.isPlaying = true;
    }
}
#endif
