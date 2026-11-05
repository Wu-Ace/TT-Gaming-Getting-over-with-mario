from pathlib import Path
import json
exec(Path('scripts/prepare_climb_courses.py').read_text().split('folder=Path')[0]);log=[]
for n in range(2,11):
 p=json.loads(Path(f'scripts/climb-plans/L{n:02}.json').read_text());nodes,seen=reach({(a['x'],a['y']) for a in p['cells']});goals=[(round(v['x']/.8-.5),round((v['y']+3.6-1.05)/.8)) for v in p['stars']]+[(round(v['x']/.8-.5),round((v['y']+3.6)/.8)) for v in p['checkpoints']]+[(round(p['exit']['x']/.8),round((p['exit']['y']+3.6)/.8))];ok=[any(abs(x-gx)<=1 and y==gy for x,y in seen) for gx,gy in goals];log.append(f'{"PASS" if all(ok) else "FAIL"} L{n} objectives/checkpoints/exit={ok}');print(log[-1])
Path('scripts/climb-evidence/route-graph.txt').write_text('\n'.join(log)+'\nApproximate surface graph only; not physical hammer traversal.')
