using UnityEngine;

namespace Character.Control
{
    // Visual-only two-bone rig. Physics, joints and input remain on the original objects.
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class PaddleCharacterVisual : MonoBehaviour
    {
        public Transform body, tool, tip;
        public SpriteRenderer torso, paddle, leftUpper, leftLower, rightUpper, rightLower;
        public SpriteRenderer[] legacy;
        // Retained serialized references for the archived frame-baking tool; never sampled.
        [HideInInspector] public Sprite[] leftPoses, rightPoses;
        const float Pixel = 1.8f / 311f;
        public float HandError { get; private set; }
        public float ArmReach { get; private set; }
        Sprite originalLeft, originalRight, leftForearm, rightForearm, leftPalm, rightPalm;
        SpriteRenderer leftHandRenderer, rightHandRenderer, leftForeRenderer, rightForeRenderer,leftUpperDraw,rightUpperDraw;
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,MeshRenderer> limbRenderers=new System.Collections.Generic.Dictionary<SpriteRenderer,MeshRenderer>();
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,Mesh> limbMeshes=new System.Collections.Generic.Dictionary<SpriteRenderer,Mesh>();
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,Vector2[]> baseVertices=new System.Collections.Generic.Dictionary<SpriteRenderer,Vector2[]>();
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,Vector3> mappedAnchor=new System.Collections.Generic.Dictionary<SpriteRenderer,Vector3>();
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,Vector2> sourceAnchor=new System.Collections.Generic.Dictionary<SpriteRenderer,Vector2>();
        public Vector3 LeftHand { get; private set; }
        public Vector3 RightHand { get; private set; }
        public Vector3 LeftElbow { get; private set; }
        public Vector3 RightElbow { get; private set; }
        void OnDisable() { ReleaseHands(); }
        void ReleaseHands()
        {
            if(originalLeft && leftLower) leftLower.enabled=true;
            if(originalRight && rightLower) rightLower.enabled=true;
            if(leftUpperDraw){DestroyVisual(leftUpperDraw.sprite);DestroyVisual(leftUpperDraw.gameObject);}
            if(rightUpperDraw){DestroyVisual(rightUpperDraw.sprite);DestroyVisual(rightUpperDraw.gameObject);}
            if(leftUpper)leftUpper.enabled=true;if(rightUpper)rightUpper.enabled=true;
            foreach(var mesh in limbMeshes.Values)if(mesh)DestroyVisual(mesh);limbMeshes.Clear();limbRenderers.Clear();
            baseVertices.Clear();mappedAnchor.Clear();sourceAnchor.Clear();
            if(leftHandRenderer) DestroyVisual(leftHandRenderer.gameObject);
            if(rightHandRenderer) DestroyVisual(rightHandRenderer.gameObject);
            if(leftForeRenderer) DestroyVisual(leftForeRenderer.gameObject);
            if(rightForeRenderer) DestroyVisual(rightForeRenderer.gameObject);
            foreach(var sprite in new[]{leftForearm,rightForearm,leftPalm,rightPalm}) if(sprite) DestroyVisual(sprite);
            leftHandRenderer=rightHandRenderer=null; originalLeft=originalRight=null;
        }
        static void DestroyVisual(Object value) { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void PrepareHands()
        {
            if(leftHandRenderer || !leftLower || !rightLower || !leftLower.sprite || !rightLower.sprite) return;
            leftUpper.enabled=rightUpper.enabled=true;
            leftUpper.sortingOrder=3;rightUpper.sortingOrder=4;
            originalLeft=leftLower.sprite;originalRight=rightLower.sprite;
            leftUpperDraw=MakeUpper(leftUpper);rightUpperDraw=MakeUpper(rightUpper);
            SplitHand(leftLower,originalLeft,82,new Vector2(94,16),out leftForearm,out leftPalm,out leftHandRenderer,out leftForeRenderer);
            SplitHand(rightLower,originalRight,88,new Vector2(99,15),out rightForearm,out rightPalm,out rightHandRenderer,out rightForeRenderer);
        }
        SpriteRenderer MakeUpper(SpriteRenderer source){
            var go=new GameObject("Connected upper "+source.name);go.hideFlags=HideFlags.DontSave;go.transform.SetParent(transform,false);
            var r=go.AddComponent<SpriteRenderer>();var sp=source.sprite;r.sprite=Sprite.Create(sp.texture,sp.rect,sp.pivot/sp.rect.size,sp.pixelsPerUnit,0,SpriteMeshType.Tight);r.sprite.hideFlags=HideFlags.DontSave;
            r.sharedMaterial=source.sharedMaterial;r.sortingLayerID=source.sortingLayerID;r.sortingOrder=source.sortingOrder;source.enabled=false;return r;
        }
        void SplitHand(SpriteRenderer forearm,Sprite source,int cut,Vector2 palmAnchor,out Sprite foreSprite,out Sprite palmSprite,out SpriteRenderer hand,out SpriteRenderer segment)
        {
            var rect=source.rect;
            foreSprite=Sprite.Create(source.texture,new Rect(rect.x,rect.y,cut,rect.height),new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.Tight);
            palmSprite=Sprite.Create(source.texture,new Rect(rect.x+cut,rect.y,rect.width-cut,rect.height),new Vector2((palmAnchor.x-cut)/(rect.width-cut),(rect.height-palmAnchor.y)/rect.height),source.pixelsPerUnit,0,SpriteMeshType.Tight);
            foreSprite.hideFlags=palmSprite.hideFlags=HideFlags.DontSave;forearm.enabled=false;
            var go=new GameObject("Grip palm " + forearm.name);go.hideFlags=HideFlags.DontSave;go.transform.SetParent(transform,false);
            hand=go.AddComponent<SpriteRenderer>();hand.sprite=palmSprite;hand.sharedMaterial=forearm.sharedMaterial;
            hand.sortingLayerID=forearm.sortingLayerID;hand.sortingOrder=8;
            var armGo=new GameObject("Forearm segment " + forearm.name);armGo.hideFlags=HideFlags.DontSave;armGo.transform.SetParent(transform,false);
            segment=armGo.AddComponent<SpriteRenderer>();segment.sprite=foreSprite;segment.sharedMaterial=forearm.sharedMaterial;
            segment.sortingLayerID=forearm.sortingLayerID;segment.sortingOrder=forearm.sortingOrder;
        }
        void LateUpdate() { Pose(); }
        public void Pose()
        {
            if (!body || !tool || !tip || !torso) return;
            foreach (var r in legacy) if (r) r.enabled = r.GetComponent<TargetMovement>() != null;
            PrepareHands();
            leftUpper.enabled=rightUpper.enabled=false;
            float growth = Mathf.Abs(body.lossyScale.x) / 7.5f;
            float px = Pixel * growth;
            Vector3 up = body.up, right = body.right;
            Vector3 origin = body.position + up * (.3f * growth);
            Place(torso, origin, Vector2.zero, Vector2.up, up, px);
            Vector3 blade = tip.position;
            Vector3 axis = -tool.right;
            // Bottom of blade stays on the existing physical contact point.
            Place(paddle, blade, new Vector2(24, 240), Vector2.up, axis, 2.15f / 230f * growth * 0.63474137f);
            Vector3 shoulderL = body.position + up * (.66f * growth) - right * (.19f * growth);
            Vector3 shoulderR = body.position + up * (.66f * growth) + right * (.19f * growth);
            // Authored material points on the paddle, independent of mouse/shoulder/reach.
            // Left/right ordering is retained; neither hand can slide or exchange grips.
            LeftHand = PixelPosition(paddle,new Vector2(24,12));
            RightHand = PixelPosition(paddle,new Vector2(24,40));
            // Visual grips slide together along the shaft; they never constrain physics.
            Vector3 shaft=tool.right;
            float shift=Vector3.Dot((shoulderL+shoulderR-LeftHand-RightHand)*.5f,shaft);
            float maxSlide=1.48f*growth;
            shift=Mathf.Clamp(shift,0,maxSlide);
            LeftHand+=shaft*shift;RightHand+=shaft*shift;
            // Shoulder protraction/retraction follows physical reach, not just tool angle.
            Vector3 gripCenter=(LeftHand+RightHand)*.5f;
            Vector3 chest=body.position+up*(.66f*growth);
            Vector3 reachDirection=gripCenter-chest;
            float extension=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f*growth,.95f*growth,reachDirection.magnitude));
            Vector3 shoulderTravel=Vector3.ClampMagnitude(reachDirection,.09f*growth)*extension;
            shoulderL+=shoulderTravel*.35f;shoulderR+=shoulderTravel*.35f;
            ArmReach = .98f * growth;
            float a = Arm(leftUpperDraw, leftForeRenderer, shoulderL, LeftHand, -1, px, 0,
                new Vector2(16,16), new Vector2(68,70), new Vector2(13,17), new Vector2(81,16));
            float b = Arm(rightUpperDraw, rightForeRenderer, shoulderR, RightHand, 1, px, 1,
                new Vector2(66,16), new Vector2(12,74), new Vector2(12,28), new Vector2(87,15));
            // Palms rotate with the shaft, independently of the forearm's approach angle.
            Place(leftHandRenderer,LeftHand,Vector2.zero,Vector2.right,tool.up,px*1.1f);
            Place(rightHandRenderer,RightHand,Vector2.zero,Vector2.right,-tool.up,px*1.1f);
            HandError = Mathf.Max(a,b);
        }
        float Arm(SpriteRenderer upper, SpriteRenderer lower, Vector3 shoulder, Vector3 hand, float bend,
            float px, int index, Vector2 s, Vector2 e, Vector2 elbowPixel, Vector2 wristPixel)
        {
            // Natural .49m segments; only the minimum shared correction for out-of-arc physics poses.
            float length = ArmReach * .5f;
            Vector3 delta = hand - shoulder;
            float distance = delta.magnitude;
            Vector3 direction = distance > .00001f ? delta/distance : body.right;
            float along = Mathf.Min(distance*.5f,length);
            float height = Mathf.Sqrt(Mathf.Max(0,length*length-along*along));
            // A torso-relative, forward-facing pole gives the elbow a continuous bend circle.
            // Positive depth avoids the planar branch flip as the shaft passes the chest.
            float extension=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f*px/Pixel,.95f*px/Pixel,distance));
            // Retracted elbows stay down/out; extension visibly carries the upper arm
            // forward rather than hiding its movement in camera-depth rotation.
            Vector3 pole = body.right * (index == 0 ? -.32f : .32f) - body.up * Mathf.Lerp(.60f,.18f,extension) + Vector3.forward * .35f;
            Vector3 bendDirection = (pole - direction * Vector3.Dot(pole,direction)).normalized;
            Vector3 elbow3 = shoulder + direction*along + bendDirection*height;
            Vector3 elbow = new Vector3(elbow3.x,elbow3.y,shoulder.z);
            // Rotate the actual sprite plane toward the camera instead of shrinking the
            // whole bitmap. Its width and native aspect ratio remain unchanged.
            float fixedLength = .49f * px / Pixel;
            PlaceLimb(upper,shoulder,s,ImageDirection(e-s),elbow3-shoulder,fixedLength / Vector2.Distance(s,e));
            // Anchor at the wrist: grip never drifts while the elbow is partly occluded.
            PlaceLimb(lower,hand,wristPixel,ImageDirection(elbowPixel-wristPixel),elbow3-hand,fixedLength / Vector2.Distance(elbowPixel,wristPixel));
            if (index == 0) LeftElbow = elbow; else RightElbow = elbow;
            return Vector3.Distance(PixelPosition(lower,wristPixel),hand);
        }
        void PlaceLimb(SpriteRenderer r,Vector3 anchor,Vector2 pixel,Vector2 sourceAxis,Vector3 direction,float scale)
        {
            Vector3 tangent=direction.sqrMagnitude>.0000001f?direction.normalized:Vector3.right;
            Vector3 width=new Vector3(-tangent.y,tangent.x,0);
            if(width.sqrMagnitude<.000001f)width=Vector3.up;
            width.Normalize();
            Vector3 normal=Vector3.Cross(tangent,width).normalized;
            Quaternion rotation=Quaternion.LookRotation(normal,width)*Quaternion.Euler(0,0,-Mathf.Atan2(sourceAxis.y,sourceAxis.x)*Mathf.Rad2Deg);
            // Continuous bone deformation: extend along the limb axis, retain native cross-section.
            // Both segments terminate at the same elbow; no independent endpoint gap.
            if(!baseVertices.ContainsKey(r))baseVertices[r]=r.sprite.vertices;
            var axis=sourceAxis.normalized;Vector2 anchorPixel=new Vector2(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y)/r.sprite.pixelsPerUnit;
            float naturalLength=(r==leftUpperDraw||r==rightUpperDraw)?.49f:.49f;
            float growth=Mathf.Abs(body.lossyScale.x)/7.5f;
            float ratio=direction.magnitude/(naturalLength*growth);
            var vertices=new Vector2[baseVertices[r].Length];
            for(int i=0;i<vertices.Length;i++){var v=baseVertices[r][i]-anchorPixel;vertices[i]=anchorPixel+v+axis*Vector2.Dot(v,axis)*(ratio-1);}
            if(!limbMeshes.ContainsKey(r)){
                var mesh=new Mesh();mesh.hideFlags=HideFlags.DontSave;limbMeshes[r]=mesh;
                var skin=new GameObject("Connected skin");skin.hideFlags=HideFlags.DontSave;skin.transform.SetParent(r.transform,false);skin.AddComponent<MeshFilter>().sharedMesh=mesh;
                var mr=skin.AddComponent<MeshRenderer>();limbRenderers[r]=mr;mr.sharedMaterial=r.sharedMaterial;mr.sortingLayerID=r.sortingLayerID;mr.sortingOrder=r.sortingOrder;
                var props=new MaterialPropertyBlock();props.SetTexture("_MainTex",r.sprite.texture);props.SetColor("_Color",r.color);mr.SetPropertyBlock(props);r.forceRenderingOff=true;
            }
            var geometry=limbMeshes[r];var points=new Vector3[vertices.Length];var colors=new Color[vertices.Length];
            for(int i=0;i<points.Length;i++){points[i]=vertices[i];colors[i]=Color.white;}
            geometry.vertices=points;geometry.uv=r.sprite.uv;geometry.triangles=System.Array.ConvertAll(r.sprite.triangles,x=>(int)x);geometry.colors=colors;geometry.RecalculateBounds();geometry.RecalculateNormals();
            limbRenderers[r].enabled=r.enabled;
            mappedAnchor[r]=anchor;sourceAnchor[r]=pixel;
            Vector3 local=new Vector3(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y,0);
            r.transform.SetPositionAndRotation(anchor-rotation*(local*scale),rotation);
            r.transform.localScale=Vector3.one*(scale*r.sprite.pixelsPerUnit/r.transform.parent.lossyScale.x);
        }
        public static Vector3 PixelPosition(SpriteRenderer r, Vector2 pixel)
        {
            var local = new Vector3(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y,0) / r.sprite.pixelsPerUnit;
            return r.transform.TransformPoint(local);
        }
        static Vector2 ImageDirection(Vector2 v) { return new Vector2(v.x,-v.y); }
        static void Place(SpriteRenderer r, Vector3 anchor, Vector2 pixel, Vector2 sourceAxis, Vector3 direction, float scale)
        {
            if (!r || !r.sprite) return;
            float angle = Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-Mathf.Atan2(sourceAxis.y,sourceAxis.x)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(0,0,angle);
            // A zero pixel denotes the center anchor used by the torso.
            Vector2 local = pixel == Vector2.zero ? Vector2.zero : new Vector2(pixel.x-r.sprite.pivot.x,r.sprite.rect.height-pixel.y-r.sprite.pivot.y);
            r.transform.SetPositionAndRotation(anchor-rotation*(Vector3)(local*scale),rotation);
            float parentScale = r.transform.parent.lossyScale.x;
            r.transform.localScale=Vector3.one*(scale*r.sprite.pixelsPerUnit/parentScale);
        }
    }
}



