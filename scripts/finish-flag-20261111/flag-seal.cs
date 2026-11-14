using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
var setup=EditorSceneManager.GetSceneManagerSetup();var lines=new System.Collections.Generic.List<string>();
try{foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled&&!s.path.EndsWith("MainMenu.unity"))){var scene=EditorSceneManager.OpenScene(entry.path);foreach(var goal in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignInteractable>(true)).Where(c=>c.kind==CampaignInteractable.Kind.Door)){if(goal.blocker){foreach(var r in goal.blocker.GetComponentsInChildren<Renderer>(true)){r.enabled=false;EditorUtility.SetDirty(r);}lines.Add(entry.path+" hidden legacy seal renderers; blocker components preserved");}}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}System.IO.File.WriteAllLines("scripts/finish-flag-20261111/seal.txt",lines);return string.Join("\n",lines);
