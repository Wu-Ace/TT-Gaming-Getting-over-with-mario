if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play before restoring editor scene.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Do not discard newly unsaved edits.");
string previous=UnityEditor.SessionState.GetString("LocalizationAcceptance.OriginalScene","");
if(!string.IsNullOrEmpty(previous) && scene.path!=previous)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(previous);
UnityEngine.Time.timeScale=1;
return new{restoredScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,playing=UnityEditor.EditorApplication.isPlaying};
