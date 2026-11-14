from pathlib import Path
import json
s=Path('scripts/dec-20261111/free-layout.cs').read_text()
s=s.replace('.before-free-layout','.before-larger-denser').replace('rng.NextDouble()<.42','rng.NextDouble()<.12').replace('5f+(float)rng.NextDouble()*7f','2.8f+(float)rng.NextDouble()*3.5f').replace('rng.NextDouble()<.4?1:rng.Next(2,5)','rng.NextDouble()<.15?1:rng.Next(3,6)').replace('rng.NextDouble()<.52','rng.NextDouble()<.75')
s=s.replace('var sprite=sprites[id];float scale=h/sprite.bounds.size.y,w=sprite.bounds.size.x*scale;','h*=id<3?1.9f:1.45f;var sprite=sprites[id];float scale=h/sprite.bounds.size.y,w=sprite.bounds.size.x*scale;')
s=s.replace('if(w>length-.3f)continue;','if(w>length-.3f){id=rng.Next(3);sprite=sprites[id];h=.65f+(float)rng.NextDouble()*.55f;scale=h/sprite.bounds.size.y;w=sprite.bounds.size.x*scale;if(w>length-.2f)continue;}')
s=s.replace('direction*(.2f+(float)rng.NextDouble()*1.1f)','direction*(.4f+(float)rng.NextDouble()*2.0f)').replace('(w+r.bounds.size.x)*.6f','(w+r.bounds.size.x)*.32f')
s=s.replace('scripts/dec-20261111/free-layout.txt','scripts/dec-20261111/larger-denser.txt')
Path('scripts/dec-20261111/larger-denser.cs').write_text(s)
json.dump(dict(name='exec_editor_script',arguments=dict(script=s,summary='Increase decoration scale and density with irregular clusters')),open('scripts/dec-20261111/larger-denser.json','w'))
