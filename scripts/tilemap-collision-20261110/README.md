# Tilemap collision configuration — 2026-11-10
Tutorial 50 cells/1 old box; Level01 736/736; Jungle1527/184; Beach1579/1579; Town337/337. Total4229 cells,2837 replaced BoxCollider2D components. 91 solid maps,90 Composite+Static Rigidbody2D and one Town map using unmerged TilemapCollider2D after Composite center-coverage failure. Retained all slopes, Cave authored polygons, and unmatched non-grid geometry. Disabled future build scenes untouched.

Solid tiles are Assets/Tiles/SolidTerrain. Paint these on configured maps; collider updates automatically. Assets/Tiles/MigratedTerrain still contains non-collision visuals. Each normalized texture preserves original image bytes/settings but uses adjusted pixels-per-unit; per-cell scale becomes identity, preventing Grid collision inheriting 5x visual scaling. No Rule Tile or Palette authored.

PASS compile0errors0warnings; all4229 solid cell centers covered after scene reload including Town fallback. PASS actual Tutorial Play falling-body test (Character layer7, body settles y=-1.845) and automatic painted/erased collision update. Tutorial Scene screenshot inspected. Full player/hammer traversal, all resolutions, target device/export NOT RUN. Source backup scenes in backup/ reflect live pre-change working tree, not Git HEAD.

Initial cloned/local-physics harness failed because cloned composites had zero generated paths; not accepted as final evidence. Real saved Tutorial Play test passed. First Town composite coverage attempts failed before save; one map was switched to unmerged collider and saved only after coverage passed. Verification code must explicitly handle Unity fake-null rather than C# null-coalescing when falling back from Composite to TilemapCollider.

Original stopped editor scene restored. No PlayerPrefs reset or explicit preference writes, gameplay source/gifts unchanged. Actual Tutorial ran briefly; complete progression/save side effects were not audited.
