import json
from pathlib import Path
p=Path('scripts/jk-tilemap-20261111')
s='''var m=UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>().First(m=>m.name.StartsWith("Jump King"));var c=m.GetComponent<UnityEngine.CompositeCollider2D>();var list=new System.Collections.Generic.List<string>();foreach(var pos in m.cellBounds.allPositionsWithin)if(m.HasTile(pos)&&!c.OverlapPoint(m.GetCellCenterWorld(pos)))list.Add(pos+" "+m.GetTile(pos).name);System.IO.File.WriteAllLines("scripts/jk-tilemap-20261111/misses.txt",list);'''
(p/'misses.json').write_text(json.dumps(dict(name='exec_editor_script',arguments=dict(script=s,summary='Identify collider samples that fall on exact slope boundaries'))))
