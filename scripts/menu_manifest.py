from pathlib import Path
import hashlib,json
root=Path.cwd();paths=[root/'Assets/Scripts/GameMenuUI.cs',root/'Assets/Scripts/CampaignLevel.cs',root/'Assets/Scripts/Winning/Clock.cs',root/'ProjectSettings/EditorBuildSettings.asset']+list((root/'Assets/Resources/MenuUI').glob('*'))+list((root/'scripts/ui-evidence').glob('*'))
(root/'scripts/ui-evidence/manifest.json').write_text(json.dumps({str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths if p.is_file()},indent=2),encoding='utf8')
