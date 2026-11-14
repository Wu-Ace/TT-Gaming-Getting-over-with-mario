import json
from pathlib import Path
out=Path('scripts/jk-tilemap-20261111')
script='''var s=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level05_Cave.unity");var l=UnityEngine.Object.FindObjectOfType<CampaignLevel>();var root=l.transform.Find("Terrain");var cols=root.GetComponentsInChildren<UnityEngine.Collider2D>();var lines=new System.Collections.Generic.List<string>();lines.Add("spawn="+l.initialSpawn+" terrain="+cols.Length);foreach(var c in cols)lines.Add(c.name+" "+c.GetType().Name+" "+c.bounds);foreach(var i in UnityEngine.Object.FindObjectsOfType<CampaignInteractable>())lines.Add("interaction "+i.name+" "+i.transform.position);System.IO.File.WriteAllLines("scripts/jk-tilemap-20261111/current.txt",lines);'''
(out/'audit.json').write_text(json.dumps(dict(name='exec_editor_script',arguments=dict(script=script,summary='Inspect fifth level current colliders and authored additions'))))
