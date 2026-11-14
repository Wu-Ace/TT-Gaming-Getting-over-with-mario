using UnityEngine;
using UnityEngine.Tilemaps;

// Stable proper six-colouring: cardinal neighbours draw from disjoint groups.
// No scanning previous cells, recursion, runtime textures, or reload-time randomness.
[CreateAssetMenu(menuName="2D/Tiles/Rock Non-Repeating Variations")]
public sealed class RockVariationTile : RuleTile {
 public Sprite[] surfaces;
 public TileBase[] slopes;
 public int seed;
 public static int Variant(Vector3Int p,int seed,int salt) {
  unchecked {
   uint h=(uint)p.x*0x9e3779b9u ^ (uint)p.y*0x85ebca6bu ^ (uint)seed ^ (uint)salt;
   h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;h^=h>>16;
   return (int)(h%3)+(((p.x+p.y)&1)*3);
  }
 }
 public bool Solid(TileBase t) {
  if(t==this)return true;
  if(slopes!=null)foreach(var s in slopes)if(t==s)return true;
  return false;
 }
 public int Mask(Vector3Int p,ITilemap m) =>
  (Solid(m.GetTile(p+Vector3Int.up))?1:0)|(Solid(m.GetTile(p+Vector3Int.down))?2:0)|
  (Solid(m.GetTile(p+Vector3Int.left))?4:0)|(Solid(m.GetTile(p+Vector3Int.right))?8:0);
 public Sprite Surface(int mask,Vector3Int p) => surfaces[mask*36+Variant(p,seed,13)*6+Variant(p,seed,7919)];
 public override void GetTileData(Vector3Int p,ITilemap map,ref TileData data) {
  data.sprite=Surface(Mask(p,map),p);data.color=Color.white;data.transform=Matrix4x4.identity;
  data.flags=TileFlags.LockTransform;data.colliderType=m_DefaultColliderType;
 }
 public override void RefreshTile(Vector3Int p,ITilemap map) {
  map.RefreshTile(p);map.RefreshTile(p+Vector3Int.left);map.RefreshTile(p+Vector3Int.right);
  map.RefreshTile(p+Vector3Int.up);map.RefreshTile(p+Vector3Int.down);
 }
}
