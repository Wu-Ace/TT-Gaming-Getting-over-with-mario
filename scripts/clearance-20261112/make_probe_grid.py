import json,pathlib
p=pathlib.Path('scripts/clearance-20261112'); script=(p/'probe-before.csx').read_text(encoding='utf-8-sig');start=script.index(' var points=');end=script.index(' UnityEngine.Object.DestroyImmediate(go);',start)
script=script[:start]+'''
 var patches=new[]{new[]{26.25f,27.85f,28.2f,29.2f},new[]{26.55f,27.85f,30.8f,31.6f},new[]{15.45f,16.85f,38.05f,38.85f}};
 var checks=new System.Collections.Generic.List<object>();
 foreach(var patch in patches){var samples=new System.Collections.Generic.List<object>();for(float y=patch[2];y<=patch[3];y+=.02f)for(float x=patch[0];x<=patch[1];x+=.02f){go.transform.position=new UnityEngine.Vector3(x,y);UnityEngine.Physics2D.SyncTransforms();float distance=100;foreach(var pc in probes)foreach(var c in terrain){var d=pc.Distance(c);if(d.isValid)distance=UnityEngine.Mathf.Min(distance,d.distance);}samples.Add(new {x=x,y=y,distance=distance});}checks.Add(new{bounds=patch,samples=samples});}
''' +script[end:];script=script.replace('unity-before.json','unity-local-grids.json');(p/'probe-grid.csx').write_text(script);(p/'probe-grid.json').write_text(json.dumps({'name':'exec_editor_script','arguments':{'script':script,'summary':'Resolve raster uncertainty with exact Unity 2 cm collision grids at three suspected bottlenecks'}}))

