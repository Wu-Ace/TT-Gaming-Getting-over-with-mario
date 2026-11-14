import json
from pathlib import Path
out=Path('scripts/rock-terrain-20261111')
script='''System.Collections.IEnumerator Capture(){
+foreach(var c in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())c.enabled=false;
+foreach(var rb in UnityEngine.Object.FindObjectsOfType<UnityEngine.Rigidbody2D>())if(rb.bodyType!=UnityEngine.RigidbodyType2D.Static)rb.simulated=false;
+var map=UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>().First(m=>m.name.StartsWith("Ground"));var list=new System.Collections.Generic.List<UnityEngine.Vector3Int>();foreach(var c in map.cellBounds.allPositionsWithin)if(map.HasTile(c))list.Add(c);var cells=list.ToArray();var cam=UnityEngine.Camera.main;cam.orthographicSize=6;
+var picks=new[]{cells.OrderBy(c=>c.x).ElementAt(cells.Length/8),cells.OrderByDescending(c=>c.y).First(),cells.OrderBy(c=>c.x).ElementAt(cells.Length*3/4)};
+for(int i=0;i<picks.Length;i++){var p=map.GetCellCenterWorld(picks[i]);cam.transform.position=new UnityEngine.Vector3(p.x,p.y+2,-10);yield return null;yield return new UnityEngine.WaitForEndOfFrame();UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/rock-terrain-20261111/"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"-game-"+i+".png"));yield return null;yield return new UnityEngine.WaitForEndOfFrame();}
+}return Capture();'''.replace('\n+','\n')
(out/'capture.json').write_text(json.dumps(dict(name='exec_runtime_script',arguments=dict(script=script,summary='Capture actual Play terrain regions without progression or save resets'))))
for n in ['Level01','Level02_Jungle','Level03_Beach','Level04_Town']:
 code='UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/'+n+'.unity");var v=UnityEditor.SceneView.lastActiveSceneView;v.in2DMode=true;v.LookAt(new UnityEngine.Vector3(12,-1,0),UnityEngine.Quaternion.identity,8,true,true);v.Repaint();'
 (out/(n+'-open.json')).write_text(json.dumps(dict(name='exec_editor_script',arguments=dict(script=code,summary='Open '+n+' for actual Scene and Game review'))))
(out/'stop.json').write_text(json.dumps(dict(name='unity_editor',arguments=dict(action='stop'))))

