from pathlib import Path
import re
from PIL import Image
s=Path('scripts/kit-terrain-repair-result.txt').read_text();pattern=r'^CONTACT (.*?) cellX=([\d.-]+)\.\.([\d.-]+) maskColumns=(\d+)/(\d+) pillars=(.*?) asset=(.*)$';count=0
soil=Image.open('Assets/Art/PlatformerKit/Terrain/grass-31.png').convert('RGBA')
for m in re.finditer(pattern,s,re.M):
 name,x0,x1,total,width,spans,path=m.groups();x0=float(x0);x1=float(x1);width=int(width);ranges=[tuple(map(float,v.split('..'))) for v in spans.split(';')];im=Image.open(path).convert('RGBA');mask=[]
 for x in range(width):
  world=x0+(x+.5)/width*(x1-x0);expected=any(a<=world<b for a,b in ranges);actual=all(im.getpixel((x,y))==soil.getpixel((x,y)) for y in range(im.height));
  if expected!=actual:assert min(abs(world-v) for r in ranges for v in r)<.0003,(name,x,world)
  mask.append(actual)
 assert sum(mask)==int(total),(name,sum(mask),total)
 count+=1
assert count==68,count
p=Path('scripts/art-evidence/contact-checks.txt');p.write_text(f'PASS: {count} pillar/ground contact records across Level prefab and scene. Pixel grass masks match world-space visible pillar footprints; 13 unique footprints. No whole-cell center approximation.\n')
print(p.read_text())
p=Path('scripts/validate_kit_art.py');t=p.read_text().replace('SUMMARY total=776 grass=365 soil=411 edgeCells=71','SUMMARY total=776 grass=390 soil=386 edgeCells=71').replace('365 grass / 411 soil','390 surface cells (including partial contact masks) / 386 fully covered soil');p.write_text(t)
p=Path('AGENTS.md');p.write_text(p.read_text()+'\n- Pillars can be off-grid. Remove grass only beneath the opaque visible footprint, clipping each intersected ground tile at the actual world-space edges. Never infer coverage from tile centers or snap/move colliders to fix art. Validate partial-cell contacts on both sides.\n')
