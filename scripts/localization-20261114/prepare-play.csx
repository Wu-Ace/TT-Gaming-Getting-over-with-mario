if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Editor must be stopped before isolated UI acceptance.");
if(UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new System.Exception("Preserve additive scene setup: UI test not started.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Unsaved scene: UI test not started.");
UnityEditor.SessionState.SetString("LocalizationAcceptance.OriginalScene",scene.path);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
return "Prepared clean MainMenu; original editor scene recorded for restoration.";
