using UnityEditor;
using UnityEngine;

// Reconcile scene handles with the authored orbit, without changing sprite proportions.
[InitializeOnLoad]
public static class OrbitPlatformDrag
{
    static OrbitPlatformDrag() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var orbit in Object.FindObjectsOfType<OrbitRidePlatform>())
        {
            if (EditorUtility.IsPersistent(orbit)) continue;
            float angle = orbit.phase * Mathf.PI * 2;
            Vector2 expected = orbit.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbit.radius;
            Vector2 delta = (Vector2)orbit.transform.position - expected;
            var visual = orbit.transform.Find("Native Beach cell");
            Vector3 visualDelta = visual == null ? Vector3.zero : visual.position - orbit.transform.position;
            if (delta.sqrMagnitude < .00000001f && visualDelta.sqrMagnitude < .00000001f) continue;
            Undo.RecordObject(orbit, "Move orbit center with platform");
            if (visual != null && visualDelta.sqrMagnitude >= .00000001f)
            {
                Undo.RecordObject(orbit.transform, "Move orbit collision with artwork");
                Undo.RecordObject(visual, "Center orbit artwork");
                orbit.transform.position += visualDelta;
                visual.localPosition = Vector3.zero;
            }
            orbit.center += delta + (Vector2)visualDelta;
            EditorUtility.SetDirty(orbit);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(orbit.gameObject.scene);
        }
    }
}

