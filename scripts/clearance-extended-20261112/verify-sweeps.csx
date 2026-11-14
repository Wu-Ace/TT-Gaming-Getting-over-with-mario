
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Preserving dirty scene");var originalPath="Assets/Scenes/Level01.unity";var reports=new System.Collections.Generic.List<object>();
var paths=Codely.Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("scripts/clearance-extended-20261112/compressed-paths.json"));
try{foreach(var path in paths){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene((string)path["scene"]);
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Collider2D>(true)).ToArray();
 var terrain=all.Where(c=>c.enabled&&!c.isTrigger&&c.name!="Closed door seal"&&(c.attachedRigidbody==null||c.attachedRigidbody.bodyType==UnityEngine.RigidbodyType2D.Static)).ToArray();
 var body=all.First(c=>c.name=="Cauldron");var origin=body.bounds.center;var go=new UnityEngine.GameObject("Temporary full-rig clearance probe");var probeBody=go.AddComponent<UnityEngine.Rigidbody2D>();probeBody.bodyType=UnityEngine.RigidbodyType2D.Kinematic;var probes=new System.Collections.Generic.List<UnityEngine.PolygonCollider2D>();
 try{
 foreach(var source in all.OfType<UnityEngine.PolygonCollider2D>().Where(c=>c.enabled&&(c.name=="Cauldron"||c.name=="Mario"||c.name=="Shape"))){var pc=go.AddComponent<UnityEngine.PolygonCollider2D>();pc.pathCount=source.pathCount;for(int j=0;j<source.pathCount;j++)pc.SetPath(j,source.GetPath(j).Select(v=>(UnityEngine.Vector2)(source.transform.TransformPoint(v+source.offset)-origin)).ToArray());probes.Add(pc);}

 var points=path["points"];float minimum=100;int samples=0;var failures=new System.Collections.Generic.List<object>();var hits=new UnityEngine.RaycastHit2D[64];var filter=new UnityEngine.ContactFilter2D();filter.SetLayerMask(1<<6);filter.useTriggers=false;
 for(int i=1;i<points.Count();i++){
 var a=new UnityEngine.Vector2((float)points[i-1][0],(float)points[i-1][1]);var b=new UnityEngine.Vector2((float)points[i][0],(float)points[i][1]);var delta=b-a;go.transform.position=a;UnityEngine.Physics2D.SyncTransforms();
 foreach(var pc in probes){int count=pc.Cast(delta.normalized,filter,hits,delta.magnitude,true);for(int k=0;k<count;k++)if(terrain.Contains(hits[k].collider)&&hits[k].distance<delta.magnitude-.001f)failures.Add(new{segment=i,probe=pc.GetInstanceID(),distance=hits[k].distance,obstacle=hits[k].collider.name});samples++;}
 foreach(var p in new[]{a,b}){go.transform.position=p;UnityEngine.Physics2D.SyncTransforms();foreach(var pc in probes){var region=pc.bounds;region.Expand(.3f);foreach(var c in terrain)if(region.Intersects(c.bounds)){var d=pc.Distance(c);if(d.isValid)minimum=UnityEngine.Mathf.Min(minimum,d.distance);}}}
 }
 reports.Add(new{scene=(string)path["scene"],samples=samples,minDistance=minimum,failures=failures});UnityEngine.Debug.Log((string)path["scene"]+" samples="+samples+" min="+minimum+" failures="+failures.Count);
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 System.IO.File.WriteAllText("scripts/clearance-extended-20261112/sweep-results.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(reports));
}}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalPath);}