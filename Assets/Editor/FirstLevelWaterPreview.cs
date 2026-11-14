using UnityEditor;
[InitializeOnLoad]
public static class FirstLevelWaterPreview {
 static FirstLevelWaterPreview(){ EditorApplication.update+=Tick; }
 static void Tick(){ if(!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode) FirstLevelWater.Attach(); }
}
