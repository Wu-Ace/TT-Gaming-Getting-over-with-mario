from pathlib import Path
r=Path(r'E:\TT Gaming\TT-Gaming-Getting-over-with-mario')
p=r/'Assets/Scripts/Character/ContactSoundEffect.cs';s=p.read_text();s=s.replace('TryPlayImpact(float normalSpeed)','TryPlayImpact(float normalSpeed, bool landing = false)').replace('GameplayAudio.Play(body ? "body_hit" : "hammer_hit", gain);','GameplayAudio.Play(body ? (landing ? "body_land" : "body_hit") : "hammer_hit", gain);').replace('TryPlayImpact(speed);','TryPlayImpact(speed, landing);');p.write_text(s,encoding='utf-8')
p=r/'Assets/Scripts/GameplayAudio.cs';s=p.read_text().replace('            case "body_hit":','            case "body_land": return "KenneyImpact/footstep_grass_000";\n            case "body_hit":').replace('cue == "hammer_hit" || cue == "body_hit"','cue == "hammer_hit" || cue == "body_hit" || cue == "body_land"');p.write_text(s,encoding='utf-8')
print('Landing cue isolated from wall-bump deduplication')
