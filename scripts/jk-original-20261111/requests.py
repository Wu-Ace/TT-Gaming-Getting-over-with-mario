import json
from pathlib import Path
p=Path('scripts/jk-original-20261111');(p/'apply.json').write_text(json.dumps(dict(name='exec_editor_script',arguments=dict(script='InstallJumpKingFifth.Apply();',summary='Replace union terrain with exact original-only Jump King collision tiles and source-derived slopes'))))
# Reuse existing full-floor render check with the new evidence directory.
v=json.loads(Path('scripts/jk-tilemap-20261111/verify.json').read_text());v['arguments']['script']=v['arguments']['script'].replace('scripts/jk-tilemap-20261111/','scripts/jk-original-20261111/');(p/'verify.json').write_text(json.dumps(v))
