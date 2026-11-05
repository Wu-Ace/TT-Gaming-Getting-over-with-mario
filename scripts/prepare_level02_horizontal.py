from pathlib import Path
import json
p=json.loads(Path("scripts/level02-horizontal.json").read_text())
assert len(p["stars"])==3
print("Authored Level02 plan retained:",len(p["cells"]),"cells. Apply via Level02Designed; old procedural layout retired.")
