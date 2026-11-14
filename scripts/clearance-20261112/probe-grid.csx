var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Active scene is dirty; preserving it");var originalPath=original.path;
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level04_Town.unity");
try{
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Collider2D>(true)).ToArray();
 var terrain=all.Where(c=>c.enabled&&!c.isTrigger&&c.name!="Closed door seal"&&(c.attachedRigidbody==null||c.attachedRigidbody.bodyType==UnityEngine.RigidbodyType2D.Static)).ToArray();
 var cauldron=all.First(c=>c.name=="Cauldron");var origin=cauldron.bounds.center;
 var go=new UnityEngine.GameObject("Clearance probe (temporary)");
 var probes=new System.Collections.Generic.List<UnityEngine.PolygonCollider2D>();
 foreach(var source in all.OfType<UnityEngine.PolygonCollider2D>().Where(c=>c.name=="Cauldron"||c.name=="Mario")){
 var pc=go.AddComponent<UnityEngine.PolygonCollider2D>();pc.pathCount=source.pathCount;for(int j=0;j<source.pathCount;j++)pc.SetPath(j,source.GetPath(j).Select(v=>(UnityEngine.Vector2)(source.transform.TransformPoint(v+source.offset)-origin)).ToArray());probes.Add(pc);
 }

 var patches=new[]{new[]{26.25f,27.85f,28.2f,29.2f},new[]{26.55f,27.85f,30.8f,31.6f},new[]{15.45f,16.85f,38.05f,38.85f}};
 var checks=new System.Collections.Generic.List<object>();
 foreach(var patch in patches){var samples=new System.Collections.Generic.List<object>();for(float y=patch[2];y<=patch[3];y+=.02f)for(float x=patch[0];x<=patch[1];x+=.02f){go.transform.position=new UnityEngine.Vector3(x,y);UnityEngine.Physics2D.SyncTransforms();float distance=100;foreach(var pc in probes)foreach(var c in terrain){var d=pc.Distance(c);if(d.isValid)distance=UnityEngine.Mathf.Min(distance,d.distance);}samples.Add(new {x=x,y=y,distance=distance});}checks.Add(new{bounds=patch,samples=samples});}
 UnityEngine.Object.DestroyImmediate(go);
 var map=all.Select(c=>c.GetComponent<UnityEngine.Tilemaps.Tilemap>()).First(t=>t!=null);var cells=new System.Collections.Generic.List<object>();foreach(var pos in map.cellBounds.allPositionsWithin)if(map.HasTile(pos)){var wp=map.GetCellCenterWorld(pos);cells.Add(new{cell=new[]{pos.x,pos.y,pos.z},world=new[]{wp.x,wp.y},tile=UnityEditor.AssetDatabase.GetAssetPath(map.GetTile(pos))});}
 System.IO.File.WriteAllText("scripts/clearance-20261112/fourth-cells-all.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(cells));
 System.IO.File.WriteAllText("scripts/clearance-20261112/unity-local-grids.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(checks));UnityEngine.Debug.Log("Exact polygon overlap samples="+checks.Count);
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalPath);}
