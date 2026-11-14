import json,subprocess,time
from pathlib import Path
out=Path('scripts/rock-sides-20261111')
def call(name,args,label):
 p=out/'capture-request.json';p.write_text(json.dumps(dict(name=name,arguments=args)))
 r=subprocess.run(['python','scripts/rock-all-levels-20261111/cli.py',str(p)],capture_output=True,text=True,encoding='utf-8',timeout=200)
 (out/(label+'.json')).write_text(r.stdout,encoding='utf-8')
 if r.returncode or '"success": false' in r.stdout or 'Script compilation failed' in r.stdout:raise RuntimeError(label+r.stdout+r.stderr)
def capture(n):
 code='''System.Collections.IEnumerator Capture(){
 foreach(var c in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())c.enabled=false;
 foreach(var rb in UnityEngine.Object.FindObjectsOfType<UnityEngine.Rigidbody2D>())if(rb.bodyType!=UnityEngine.RigidbodyType2D.Static)rb.simulated=false;
 var cam=UnityEngine.Camera.main;var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();var start=level?level.initialSpawn:UnityEngine.Vector2.zero;cam.orthographicSize=5;
 for(int i=0;i<2;i++){
  var p=i==0?start+new UnityEngine.Vector2(6,1):start+new UnityEngine.Vector2(24,1);
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Level05_Cave"&&i==1)p=new UnityEngine.Vector2(12,110);
  cam.transform.position=new UnityEngine.Vector3(p.x,p.y,-10);
  yield return null;yield return new UnityEngine.WaitForEndOfFrame();yield return new UnityEngine.WaitForSecondsRealtime(.15f);
  UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/rock-sides-20261111/"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"-game-"+i+".png"));
  yield return null;yield return new UnityEngine.WaitForEndOfFrame();yield return new UnityEngine.WaitForSecondsRealtime(.25f);
 }
 }return Capture();'''
 call('exec_runtime_script',dict(script=code,summary='Capture actual '+n+' top/down variation without progression',timeoutSeconds=60),n+'-capture')
scenes=sorted(p.stem for p in Path('Assets/Scenes').glob('*.unity') if p.stem!='MainMenu')
try:
 for n in ['Level01','Level05_Cave','Level10_Sky']:
  call('exec_editor_script',dict(script='UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/'+n+'.unity");',summary='Open '+n+' for actual visual check',timeoutSeconds=60),n+'-open')
  call('unity_editor',dict(action='play'),n+'-play')
  capture(n)
  call('unity_editor',dict(action='stop'),n+'-stop')
  print('Captured '+n,flush=True)
finally:
 call('unity_editor',dict(action='stop'),'final-stop')
 call('exec_editor_script',dict(script='UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");var v=UnityEditor.SceneView.lastActiveSceneView;v.in2DMode=true;v.LookAt(new UnityEngine.Vector3(15,-2,0),UnityEngine.Quaternion.identity,6,true,true);v.Focus();v.Repaint();',summary='Restore stopped editor on first level'),'restore')
