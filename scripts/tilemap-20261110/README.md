# Tilemap migration 2026-11-10
Enabled Tutorial and Level01-05 static terrain visuals migrated to Unity Tilemap; original sprite resources, cell transforms/colors/sorting and original collider objects retained. Moving platforms/interactions remain GameObjects. Backups are pre-migration dirty working-tree content, not Git HEAD.

4377 sprites migrated. Non-grid geometry retained through separate Tilemaps (Cave 101 maps); this is a compatibility migration, not a grid-snapped level redesign. Original collider objects remain authoritative: painting new tiles does not automatically change collisions. No Rule Tile topology or Tile Palette asset was authored. Migrated tile assets are in Assets/Tiles/MigratedTerrain.

Validation: editor compilation 0 errors/0 warnings; serialized collider blocks identical all six scenes; source-to-cell sprite/position/matrix/color/sorting comparison in verification.txt. Cave Scene view captured and inspected. Game View, physical Play, full traversal, device/export NOT RUN. Mirror matrix comparison must use direct matrix rather than rotation/lossyScale decomposition.
