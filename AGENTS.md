# Project art rules

- Preserve the source aspect ratio for characters, enemies, pickups, hills and decoration. Use uniform scale and repositioning/transparent padding; never resize X and Y independently to fit an old sprite rectangle.
- Use native-size repeated cells for ground, walls and variable-height pillars. Choose exposed top grass, soil interior, left/right faces and bottom corners from the Kit. Interior and bottom rows must not carry grass when covered by another ground cell.
- Keep colliders independent of visuals. Art replacement must not silently resize colliders or change traversal.
- Inspect the actual imported Kit contact sheet before mapping. Do not use guessed filenames or assign unrelated placeholder shapes as final art.
- Check real Unity Scene and Game views after refresh. Asset existence, GUID checks and compilation alone do not establish visual correctness. Inspect gaps, ends, multiple rows and all pillar heights before reporting completion.
- Native source assets are in Assets/Art/2D Platformer Kit. Adapted assets are in Assets/Art/PlatformerKit. The old replace_kit_art.py and finalize_kit_art.py are retired. Run fix_kit_proportions.py for art and Tools/Art/Repair Kit Terrain Topology after terrain layout edits.
- Preserve unrelated work (including live gifts). Current project has no Git repository; keep scoped backups and evidence hashes.

- Pillars embedded in terrain must have open bottom edges and suppress the grass directly underneath. Match topology using the native atlas layout, not numeric tile filename order. Hills spanning multiple source cells must include both sides and their center; never ship a single half-hill tile as a complete decoration.

- Pillars can be off-grid. Remove grass only beneath the opaque visible footprint, clipping each intersected ground tile at the actual world-space edges. Never infer coverage from tile centers or snap/move colliders to fix art. Validate partial-cell contacts on both sides.

- Animated replacement sprites must share a common canvas, pixels-per-unit and pivot across the entire sequence. Crop a common sequence-wide box, never fit each frame into its legacy slice rectangle. Preserve intended width changes (coin rotation) without changing height/anchor. Death transitions must be checked through cleanup in real Play, including duplicate events and shell behavior.

- Use uncompressed clamp textures and a 1-native-pixel extruded gutter for ground render sprites to prevent raster seams; preserve the 120px interior and collider grid. Keep authored death visuals when requested, and render falling corpses in a SortingGroup above world layers (HUD remains overlay).
