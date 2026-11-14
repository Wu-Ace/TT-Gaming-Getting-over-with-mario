System.Collections.IEnumerator Check(){
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Preserving dirty scene");var originalPath=original.path;var reports=new System.Collections.Generic.List<object>();
var paths=Codely.Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("scripts/clearance-extended-20261112/full-paths.json"));
try{foreach(var path in paths){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene((string)path["scene"]);
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Collider2D>(true)).ToArray();
 var terrain=all.Where(c=>c.enabled&&!c.isTrigger&&c.name!="Closed door seal"&&(c.attachedRigidbody==null||c.attachedRigidbody.bodyType==UnityEngine.RigidbodyType2D.Static)).ToArray();
 var body=all.First(c=>c.name=="Cauldron");var origin=body.bounds.center;var go=new UnityEngine.GameObject("Temporary full-rig clearance probe");var probes=new System.Collections.Generic.List<UnityEngine.PolygonCollider2D>();
 try{
 foreach(var source in all.OfType<UnityEngine.PolygonCollider2D>().Where(c=>c.enabled&&(c.name=="Cauldron"||c.name=="Mario"||c.name=="Shape"))){var pc=go.AddComponent<UnityEngine.PolygonCollider2D>();pc.pathCount=source.pathCount;for(int j=0;j<source.pathCount;j++)pc.SetPath(j,source.GetPath(j).Select(v=>(UnityEngine.Vector2)(source.transform.TransformPoint(v+source.offset)-origin)).ToArray());probes.Add(pc);}
 var points=path["points"];float minimum=100;int samples=0;var failures=new System.Collections.Generic.List<object>();
 for(int i=1;i<points.Count();i++){
 var a=new UnityEngine.Vector2((float)points[i-1][0],(float)points[i-1][1]);var b=new UnityEngine.Vector2((float)points[i][0],(float)points[i][1]);
 for(int j=0;j<=5;j++){var p=UnityEngine.Vector2.Lerp(a,b,j/5f);go.transform.position=p;UnityEngine.Physics2D.SyncTransforms();float dmin=100;
 foreach(var pc in probes){var region=pc.bounds;region.Expand(.3f);foreach(var c in terrain)if(region.Intersects(c.bounds)){var d=pc.Distance(c);if(d.isValid)dmin=UnityEngine.Mathf.Min(dmin,d.distance);}}
 minimum=UnityEngine.Mathf.Min(minimum,dmin);if(dmin<.005f&&failures.Count<100)failures.Add(new{point=new[]{p.x,p.y},distance=dmin});samples++;
 }
 if(i%100==0)yield return null;
 }
 reports.Add(new{scene=(string)path["scene"],samples=samples,minDistance=minimum,failures=failures});UnityEngine.Debug.Log((string)path["scene"]+" samples="+samples+" min="+minimum+" failures="+failures.Count);
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 System.IO.File.WriteAllText("scripts/clearance-extended-20261112/full-path-results.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(reports));
}}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalPath);}
}
return Check();

