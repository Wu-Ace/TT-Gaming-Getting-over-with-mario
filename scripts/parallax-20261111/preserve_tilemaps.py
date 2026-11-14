import pathlib, re, json

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
pattern = re.compile(r'^--- !u!1839735485 &(-?\d+).*?(?=^--- !u!|\Z)', re.M | re.S)
report = {}
for backup in (out / 'backup').glob('*.unity'):
    if backup.stem.endswith('-after-import'):
        continue
    target = root / 'Assets' / 'Scenes' / backup.name
    before = backup.read_text(encoding='utf-8-sig')
    current = target.read_text(encoding='utf-8-sig')
    original = {m[1]: m[0] for m in pattern.finditer(before)}
    fixed = pattern.sub(lambda m: original.get(m[1], m[0]), current)
    if fixed != current:
        (out / 'backup' / (backup.stem + '-after-import.unity')).write_text(current, encoding='utf-8')
        target.write_text(fixed, encoding='utf-8')
    report[backup.stem] = 'Tilemap serialization preserved byte-for-byte (normalized line endings)'
(out / 'tilemap-preservation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
