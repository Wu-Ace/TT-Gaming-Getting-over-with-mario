using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
var setup=EditorSceneManager.GetSceneManagerSetup();var lines=new System.Collections.Generic.List<string>();
try{foreach(var e in EditorBuildSettings.scenes.Where(s=>s.enabled&&!s.path.EndsWith("MainMenu.unity"))){var scene=EditorSceneManager.OpenScene(e.path);if(scene.isDirty)throw new Exception("Dirty scene");File.Copy(e.path,"scripts/no-diamond-gate-20261111/backup/"+Path.GetFileName(e.path),true);foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true))){if(text.text.Contains("EXIT")){lines.Add(e.path+": "+text.text+" -> 终点");text.text="终点";EditorUtility.SetDirty(text);}}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}File.WriteAllLines("scripts/no-diamond-gate-20261111/labels.txt",lines);return string.Join("\n",lines);
