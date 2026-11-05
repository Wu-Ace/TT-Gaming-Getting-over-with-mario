## Current layout

The active levels 2–10 now use the climbing revision in [CLIMB-COURSES.md](CLIMB-COURSES.md). Mario-reference descriptions and older layout estimates below are superseded.

# Layout revision notice

Levels 2–10 below were replaced using the user-supplied original Mario map strips. See [MARIO-REFERENCE-REVISION.md](MARIO-REFERENCE-REVISION.md) for current layouts, scope and validation. Earlier route estimates and layout descriptions below are historical. Core three-star/session rules still apply.

# Ten-level star campaign

All levels use three body-collected objective stars. The far-right door unlocks at 3/3; entering the door completes the level. Hammer contact cannot collect stars. Ordinary deaths reload the scene while preserving stars, checkpoint and local shortcut states in this play session. Restart level clears that level's state. Exiting the application clears session progress; no PlayerPrefs are modified.

The old first-level win threshold is disabled. The new exit is x=310, near the right edge of the existing map. Its existing geometry, art, enemies, pickups and live-gift configuration remain. Campaign cameras follow vertically as well as horizontally.

| Level | Scene | Single tile theme | Estimated route including star detours |
|---|---|---|---|
| 1 | OneOneScene | Grassland | Existing course; new exit extends the old finish slightly |
| 2 | Level02_Jungle | Jungle | 326 units |
| 3 | Level03_Beach | Beach | 313 units |
| 4 | Level04_Town | Town | 325 units |
| 5 | Level05_Cave | Cave | 319 units |
| 6 | Level06_Grassland_Winter | Grassland_Winter | 326 units |
| 7 | Level07_Ice | Ice | 323 units |
| 8 | Level08_Castle | Castle | 334 units |
| 9 | Level09_Volcano | Volcano | 319 units |
| 10 | Level10_Sky | Sky | 336 units |

These lengths are authored route estimates, not measured player trajectories. Existing spawn-to-old-finish horizontal distance is about 314 units. Actual travel and completion time depend on hammer use and retries.

Jungle uses rolling slopes; Beach uses separated islands; Town uses roof platforms and a keyed alley; Cave uses low ceilings and recessed sections; Winter uses broad ridges; Ice uses reduced-friction terrain and braking valleys; Castle uses high plateaus, ledges and overhangs; Volcano uses descending and rising valleys; Sky uses discontinuous cloud platforms. Objective platforms have intermediate footholds. Each new level has one middle checkpoint; levels 8–10 also have a late checkpoint. The Volcano middle checkpoint is separated from the nearby spike bed.

Switches open local barriers permanently for the current attempt/session. The Town bronze key opens a local shortcut, not the final exit. Bees and ghosts patrol predictable paths; plants emerge on a fixed cycle. Body contact with enemies/spikes kills. A hammer collision defeats the new enemies; spikes do not kill the hammer. Enemy frames retain uniform proportions. Native Kit source files are unchanged; theme tile derivatives use native cell crops and edge gutters under Assets/Art/Campaign.

## Use and verification

Open Assets/Scenes/OneOneScene.unity and Play. Complete a level and use Next level. All ten scenes are enabled in Build Settings. Individual levels can also be opened directly in the editor. The existing gift test panel can be toggled with F12.

- `level-evidence/build-results.txt`: authored layout summary.
- `level-evidence/structure-results.txt`: per-scene objective count and theme checks.
- `level-evidence/play-results.txt`: actual Play trigger, death/reload, checkpoint, restart and spike checks. Tests freeze input and teleport the rig; they do not establish full hammer traversal or difficulty balance.
- `level-evidence/Lxx-layout-y.png`: editor render at each objective location.
- `level-evidence/Lxx-star.png`, `Lxx-exit.png`: actual Game captures during smoke testing.
- `level-evidence/ui-results.txt`: actual Next level button smoke check, when run.

Full unassisted hammer playthroughs, gift-assisted balance, target device performance and an exported build remain separate validation work. No export or publication was performed.

Original disk scene, loaded scene snapshot and Build Settings are under art-backup/pre-campaign. Source snapshots there reconstruct only the scoped pre-campaign edits. The editor-only CampaignTemplate scene preserves the loaded baseline used for generation and is excluded from Build Settings. Do not run the builder to edit a single level: it regenerates the campaign. Edit scene terrain directly instead. Existing Grassland topology repair is specific to the original first-level art and must not be applied to other themes.
