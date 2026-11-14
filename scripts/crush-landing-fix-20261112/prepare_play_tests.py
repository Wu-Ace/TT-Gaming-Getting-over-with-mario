from pathlib import Path
import json
p=Path('scripts/crush-landing-fix-20261112');old=Path('scripts/crush-death-20261112')
for name,tool in [('prepare-play','exec_editor_script'),('test-play','exec_runtime_script')]:
 s=(old/(name+'.csx')).read_text(encoding='utf-8-sig').replace('scripts/crush-death-20261112/','scripts/crush-landing-fix-20261112/')
 (p/(name+'.csx')).write_text(s);(p/(name+'.json')).write_text(json.dumps({'name':tool,'arguments':{'script':s,'summary':'Verify isolated actual Play crush death remains functional after landing false-positive fix','timeoutSeconds':180}}))
