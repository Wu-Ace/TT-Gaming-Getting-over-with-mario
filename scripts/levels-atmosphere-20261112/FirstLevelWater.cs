using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

[ExecuteAlways, RequireComponent(typeof(Camera)), DefaultExecutionOrder(12000)]
public sealed class FirstLevelWater : MonoBehaviour
{
    [Tooltip("Place the waterline halfway through the ground platform beneath the initial camera.")]
    public bool alignToPlatformMiddle = true;
    public float waterHeight = 3.2f;
    public float depth = 5f;
    Camera view, reflection;
    GameObject surface, capture;
    Material material;
    RenderTexture texture;
    Vector2 cameraOrigin, lastCameraPosition;
    float cameraTravel, cameraMotion;
    bool motionInitialized;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; Attach(); }
    static void Loaded(Scene s, LoadSceneMode m) { Attach(); }
    public static void Attach() { if(SceneManager.GetActiveScene().name != "Level01") return; var c=Camera.main; if(c && !c.GetComponent<FirstLevelWater>()) c.gameObject.AddComponent<FirstLevelWater>(); }
    void OnEnable() { Build(); }
    void Build() {
        if(gameObject.scene.name != "Level01") return;
        view=GetComponent<Camera>();
        cameraOrigin=lastCameraPosition=view.transform.position;
        cameraTravel=cameraMotion=0;
        motionInitialized=true;
        AlignWaterline();
        var shader=Resources.Load<Shader>("KingdomWater"); if(!shader) return;
        texture=new RenderTexture(1280,720,24) { name="Level01 water reflection", filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp, hideFlags=HideFlags.HideAndDontSave }; texture.Create();
        material=new Material(shader) { hideFlags=HideFlags.HideAndDontSave }; material.SetTexture("_Reflection",texture);
        capture=new GameObject("Water reflection camera") { hideFlags=HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(capture,gameObject.scene);
        reflection=capture.AddComponent<Camera>();
        var data=capture.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing=false; data.renderShadows=false;
        surface=GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name="Smooth river (visual only)"; surface.hideFlags=HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(surface,gameObject.scene); surface.layer=4;
        var collider=surface.GetComponent<Collider>(); collider.enabled=false; Release(collider);
        var renderer=surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.sortingOrder=250;
        Sync();
    }
    void AlignWaterline() {
        if(!alignToPlatformMiddle || !view) return;
        // Find the solid column directly below the view, then use its actual cell edges.
        // Resolve once on build so camera movement never makes the river follow vertically.
        float nearestTop=float.NegativeInfinity;
        foreach(var map in FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>()) {
            if(map.gameObject.scene!=gameObject.scene || !map.name.StartsWith("Ground")) continue;
            var cell=map.WorldToCell(view.transform.position);
            var bounds=map.cellBounds;
            for(int y=Mathf.Min(cell.y,bounds.yMax-1);y>=bounds.yMin;y--) {
                var probe=new Vector3Int(cell.x,y,0);
                if(!map.HasTile(probe)) continue;
                int top=y, bottom=y;
                while(top+1<bounds.yMax && map.HasTile(new Vector3Int(cell.x,top+1,0)))top++;
                while(bottom-1>=bounds.yMin && map.HasTile(new Vector3Int(cell.x,bottom-1,0)))bottom--;
                float upper=map.CellToWorld(new Vector3Int(cell.x,top+1,0)).y;
                float lower=map.CellToWorld(new Vector3Int(cell.x,bottom,0)).y;
                if(upper>nearestTop) { nearestTop=upper; waterHeight=(upper+lower)*.5f; }
                break;
            }
        }
    }
    void LateUpdate() { if(!surface) Build(); Sync(); }
    void Sync() {
        if(!reflection || !view || !surface) return;
        reflection.CopyFrom(view); reflection.enabled=true; reflection.depth=view.depth-10;
        reflection.targetTexture=texture; reflection.cullingMask=view.cullingMask & ~(1<<4);
        reflection.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
        Vector2 cameraPosition=view.transform.position;
        if(!motionInitialized) { cameraOrigin=lastCameraPosition=cameraPosition; motionInitialized=true; }
        Vector2 delta=cameraPosition-lastCameraPosition;
        lastCameraPosition=cameraPosition;
        // Ignore camera teleports. Integrate travel so the pattern never snaps on stopping.
        if(delta.sqrMagnitude<4f) cameraTravel+=delta.x;
        float dt=Application.isPlaying ? Time.unscaledDeltaTime : 1f/60f;
        float speed=delta.sqrMagnitude<4f ? delta.magnitude/Mathf.Max(dt,.001f) : 0f;
        cameraMotion=Mathf.Lerp(cameraMotion,Mathf.Clamp01(speed/5f),1f-Mathf.Exp(-dt*4f));
        material.SetVector("_CameraOffset",cameraPosition-cameraOrigin);
        material.SetFloat("_CameraTravel",cameraTravel);
        material.SetFloat("_CameraMotion",cameraMotion);
        float height=view.orthographicSize*2, width=height*view.aspect;
        surface.transform.position=new Vector3(view.transform.position.x,waterHeight-depth*.5f,0);
        surface.transform.localScale=new Vector3(width+2,depth,1);
        material.SetVector("_View",new Vector4(view.transform.position.x-width*.5f,view.transform.position.y-height*.5f,width,height));
        material.SetFloat("_WaterY",waterHeight); material.SetFloat("_Depth",depth);
        material.SetFloat("_Clock",Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup);
    }
    void Release(Object o) { if(!o)return; if(Application.isPlaying)Destroy(o);else DestroyImmediate(o); }
    void OnDisable() { if(capture)capture.SetActive(false); Release(surface); Release(capture); Release(material); if(texture)texture.Release(); Release(texture); }
}
