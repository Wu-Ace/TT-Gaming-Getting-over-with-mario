import json,numpy as np
from pathlib import Path
from collections import deque
p=Path(__file__).parent; paths=[]
for i,patch in enumerate(json.loads((p/'unity-local-grids.json').read_text())):
 s=patch['samples'];xs=sorted(set(x['x'] for x in s));ys=sorted(set(x['y'] for x in s));a=np.array([x['distance'] for x in s]).reshape(len(ys),len(xs));free=a>.02;h,w=free.shape
 start=[(0,x) for x in range(w) if free[0,x]] if i<2 else [(y,w-1) for y in range(h) if free[y,w-1]]
 goals={(h-1,x) for x in range(w) if free[-1,x]} if i==0 else {(y,0) for y in range(h) if free[y,0]}
 q=deque(start);prev={x:None for x in start};goal=None
 while q:
  y,x=q.popleft()
  if (y,x) in goals:goal=(y,x);break
  for yy,xx in [(y-1,x),(y+1,x),(y,x-1),(y,x+1)]:
   if 0<=yy<h and 0<=xx<w and free[yy,xx] and (yy,xx) not in prev:prev[(yy,xx)]=(y,x);q.append((yy,xx))
 assert goal is not None, i
 path=[]
 while goal is not None:y,x=goal;path.append([xs[x],ys[y]]);goal=prev[goal]
 paths.append(path[::-1])
(p/'local-witness-paths.json').write_text(json.dumps(paths));print([(len(x),x[0],x[-1]) for x in paths])
script=(p/'probe-before.csx').read_text(encoding='utf-8-sig');start=script.index(' var points=');end=script.index(' UnityEngine.Object.DestroyImmediate(go);',start)
script=script[:start]+'''
 var paths=Codely.Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("scripts/clearance-20261112/local-witness-paths.json"));var checks=new System.Collections.Generic.List<object>();
 foreach(var path in paths){float min=100;int count=0;for(int k=1;k<path.Count();k++){var a=new UnityEngine.Vector2((float)path[k-1][0],(float)path[k-1][1]);var b=new UnityEngine.Vector2((float)path[k][0],(float)path[k][1]);for(int j=0;j<=4;j++){go.transform.position=UnityEngine.Vector2.Lerp(a,b,j/4f);UnityEngine.Physics2D.SyncTransforms();foreach(var pc in probes)foreach(var c in terrain){var d=pc.Distance(c);if(d.isValid)min=UnityEngine.Mathf.Min(min,d.distance);}count++;}}checks.Add(new{samples=count,minDistance=min});if(min<.01f)throw new System.Exception("Witness too close: "+min);}
''' +script[end:];script=script.replace('unity-before.json','unity-witness-results.json');(p/'verify-witness.csx').write_text(script);(p/'verify-witness.json').write_text(json.dumps({'name':'exec_editor_script','arguments':{'script':script,'summary':'Verify continuous local passage witnesses at 5 mm intervals using actual Unity body colliders'}}))

