# Rock terrain replacement — 2026-11-11

Applied to Level01, Level02_Jungle, Level03_Beach, Level04_Town only. 4,715 grid cells (1272/1527/1579/337) now use Assets/Tiles/RockTerrain/Rock.asset. 65 independent Level03 platform renderers use rock variants, with uniform visual scale and original collider ancestors retained. Rock_tex excluded. Background/decor/enemies/player untouched by this installer. Original Rock PNGs and metadata untouched.

16 cardinal-neighbor cases support corners, exposed edges, interior, narrow columns, isolated cells and single-row platforms. Native composition is uniformly reduced from 128 to 120px, then surrounded with a 1px extruded gutter; PPU150 preserves the .8m grid. The 128x121 bottom source is padded without stretching. Middle wall contours and thin ledges are composed from the inspected Rock edges. Fill black is matched to edge pieces to prevent color bands. Paintable Rock Palette.prefab supplied.

PASS: real editor refresh/compilation, zero errors/warnings; real Level01 Scene view inspection; Game captures from all four levels inspected for continuous terrain, gaps, steps, ends, columns and thin surfaces. Captures relocate the camera and freeze runtime rigidbodies; they are visual checks, not traversal. Level03-game-0 was not saved; other regions were inspected. PASS all 67 serialized scene collider components and their ancestor position/rotation/scale unchanged against scoped backups. Prefab collider geometry untouched.

NOT RUN: exhaustive every pillar-height view, complete physical hammer traversal, passenger carrying/falling-platform behavioral regression, device/export. Live editor left stopped on Level01. Existing and concurrent player/font/finish-flag work retained; changes elsewhere in current Git status belong to other work. HEAD observed at final audit: b96b2d04280c510e3f583bc45a4f379f1fb67fab; hashes identify actual files.

Backups: backup/Level01.unity through Level04_Town.unity pre-replacement. pre-final-editor-save.unity preserves disk state before saving the current editor scene. Failed batch launch logs are historical: OS process inventory was incomplete; existing editor was subsequently reused via host bridge.
