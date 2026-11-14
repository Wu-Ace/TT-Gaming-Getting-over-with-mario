using System.Linq;
System.Collections.IEnumerator Test(){
 GameMenuUI.PreferencePrefix="level05-physical-test-20261114.";GameMenuUI.Testing=true;UnityEngine.Time.timeScale=1;
 var d=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();var a=UnityEngine.Object.FindObjectOfType<Character.Control.ArmsSimulation>();var input=UnityEngine.Object.FindObjectOfType<Character.Control.TargetMovement>();input.enabled=false;
 var so=new UnityEditor.SerializedObject(a);var center=(UnityEngine.Transform)so.FindProperty("center").objectReferenceValue;var root=d.transform.parent;
 var start=d.transform.position;float max=start.y;var lines=new System.Collections.Generic.List<string>();
 for(int i=0;i<2000&&!d.Dead;i++){
   UnityEngine.Time.timeScale=1;float phase=(i%100)/100f;
   float angle=UnityEngine.Mathf.Lerp(-165,-95,phase)*UnityEngine.Mathf.Deg2Rad;
   a.ReceiveTarget((UnityEngine.Vector2)center.position+new UnityEngine.Vector2(UnityEngine.Mathf.Cos(angle),UnityEngine.Mathf.Sin(angle))*1.6f);
   yield return new UnityEngine.WaitForFixedUpdate();max=UnityEngine.Mathf.Max(max,d.transform.position.y);
   if(i%100==0)lines.Add(i+" "+d.transform.position+" v="+d.GetComponent<UnityEngine.Rigidbody2D>().velocity);
 }
 lines.Add("start="+start+" end="+d.transform.position+" maxY="+max+" dead="+d.Dead+" no teleport or external impulse");System.IO.File.WriteAllLines("scripts/level05-physical-20261114/candidate-ascent.txt",lines);UnityEngine.Debug.Log(lines.Last());UnityEngine.Time.timeScale=0;
}return Test();
