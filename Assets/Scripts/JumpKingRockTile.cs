using UnityEngine;
using UnityEngine.Tilemaps;

// A diagonal cell is still terrain when selecting the neighboring rock edges.
// Otherwise RuleTile draws exposed edge cutouts inside a continuous slope.
[CreateAssetMenu(menuName="2D/Tiles/Jump King Rock")]
public sealed class JumpKingRockTile : RuleTile {
 public TileBase[] slopes;
 public override bool RuleMatch(int neighbor, TileBase other) {
  if(slopes!=null)foreach(var slope in slopes)if(other==slope){other=this;break;}
  return base.RuleMatch(neighbor,other);
 }
}
