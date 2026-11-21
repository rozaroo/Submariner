using System.IO;
using UnityEditor;
using UnityEngine;

internal static class SubmarinerFloodVisualSetup
{
    private const string ResultPath = "Temp/SubmarinerFloodVisualPlayTest.txt";
    private static float startTime = -1;
    private static float drainageTime = -1;
    private static float startLevel;
    private static float raisedLevel;
    private static int cracks;
    private static Renderer surface;

    [MenuItem("Tools/Submariner/Test Flood Visual", true)]
    private static bool CanTestFloodVisual()
    {
        return EditorApplication.isPlaying && !EditorApplication.isPaused;
    }

    [MenuItem("Tools/Submariner/Test Flood Visual")]
    private static void RunFloodVisualTest()
    {
        startTime = -1f;
        drainageTime = -1f;
        surface = null;
        EditorApplication.update -= TestFloodVisual;
        EditorApplication.update += TestFloodVisual;
    }

    private static void TestFloodVisual()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= TestFloodVisual;
            return;
        }

        if (EditorApplication.isPaused || Time.time < 1f)
            return;

        if (!surface)
        {
            GameObject target = GameObject.Find("StylisedWaterSurface");
            if (!target)
                return;
            surface = target.GetComponent<Renderer>();
        }

        if (startTime < 0f)
        {
            startTime = Time.time;
            startLevel = surface.bounds.max.y;
            cracks = Object.FindFirstObjectByType<HullDamageManager>().SpawnImmediateDamage(5);
            File.WriteAllText(ResultPath, $"Started with {cracks} forced hull cracks at surface Y {startLevel:F4}\n");
            return;
        }

        if (drainageTime < 0f)
        {
            if (Time.time - startTime < 5f)
                return;
            raisedLevel = surface.bounds.max.y;
            drainageTime = Time.time;
            GameEventChannel<OnDrainagePropertyChange>.RaiseEvent(new OnDrainagePropertyChange(1f));
            return;
        }

        if (Time.time - drainageTime < 1.5f)
            return;

        EditorApplication.update -= TestFloodVisual;
        float drainedLevel = surface.bounds.max.y;
        File.AppendAllText(ResultPath,
            $"New rendered surface Y after flooding: {raisedLevel:F4}\n" +
            $"New rendered surface Y after drainage event: {drainedLevel:F4}\n" +
            $"Shader compilation errors: {ShaderUtil.ShaderHasError(surface.sharedMaterial.shader)}\n" +
            $"Surface followed both directions: {raisedLevel > startLevel && drainedLevel < raisedLevel}\n");
        EditorApplication.isPaused = true;
    }
}
