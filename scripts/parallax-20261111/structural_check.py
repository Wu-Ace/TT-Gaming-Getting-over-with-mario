import pathlib, re, json
root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
def docs(path):
    text = path.read_text(encoding='utf-8-sig')
    return {m[2]: (m[1], m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+).*?(?=^--- !u!|\Z)', text, re.M | re.S)}
results = {}
for backup in (out/'backup').glob('*.unity'):
    if backup.stem.endswith('-after-import'):
        continue
    old, new = docs(backup), docs(root/'Assets/Scenes'/backup.name)
    added = set(new) - set(old)
    assert len(added) == 1 and all(new[k][0] == '114' for k in added)
    assert set(old) <= set(new)
    changed = []
    for key, (kind, text) in old.items():
        current = new[key][1]
        if current != text:
            changed.append(key)
            assert kind == '1', (backup.name, key, kind)
            for extra in added:
                current = re.sub(r'^  - component: \{fileID: ' + extra + r'\}\n', '', current, flags=re.M)
            assert current == text, (backup.name, key)
    assert len(changed) == 1
    results[backup.stem] = {'pass': True, 'only_change': 'one camera background component added', 'all_existing_non_camera_nodes_colliders_tilemaps_transforms': 'identical to scoped pre-task backup'}
(out/'structural-check.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results))
