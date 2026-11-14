using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FirstLevelMoonlightPreview
{
    static FirstLevelMoonlightPreview() { EditorApplication.delayCall+=Install; EditorApplication.update+=Tick; }
    static void Install() {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Level01") return;
        var c=Camera.main; if(!c) return;
        if(!c.GetComponent<FirstLevelMoonlight>()) {
            var effect=c.gameObject.AddComponent<FirstLevelMoonlight>();
            effect.hideFlags=HideFlags.DontSave;
        }
        SceneView.RepaintAll();
    }
    static void Tick() {
        const string request="scripts/moonlight-20261111/capture.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request)) return;
        File.Delete(request); Install();
        var c=Camera.main; if(!c||!c.GetComponent<FirstLevelMoonlight>()) return;
        c.GetComponent<FirstLevelMoonlight>().Build();
        var old=c.targetTexture; var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1280,720,24);
        Texture2D tex=null;
        try {
            c.targetTexture=rt; c.Render(); RenderTexture.active=rt;
            tex=new Texture2D(1280,720,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply();
            File.WriteAllBytes("scripts/moonlight-20261111/preview.png",tex.EncodeToPNG());
            var shafts=GameObject.Find("Layered white moonlight");
            if(shafts && shafts.transform.childCount>0){
                var beam=shafts.transform.GetChild(0);
                Vector3 before=beam.position,cameraPosition=c.transform.position;
                try{
                    c.transform.position+=new Vector3(5,2,0);
                    c.SendMessage("OnPreCull",SendMessageOptions.DontRequireReceiver);
                    if(beam.position!=before || shafts.transform.parent!=null)throw new System.Exception("Moonlight moved with camera");
                    File.WriteAllText("scripts/moonlight-20261111/world-anchor.txt","PASS: camera translated (5,2); beam world position unchanged; root has no camera parent.");
                }finally{c.transform.position=cameraPosition;}
            }
            File.WriteAllText("scripts/moonlight-20261111/result.txt","PASS: editor compiled; Level01 camera rendered with FirstLevelMoonlight. No scene save or gameplay changes.");
        } finally { c.targetTexture=old; RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt); if(tex) Object.DestroyImmediate(tex); }
    }
}
