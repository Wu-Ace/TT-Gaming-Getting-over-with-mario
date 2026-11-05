# Climbing revision — 2026-11-04

This supersedes the Mario-reference layouts for levels 2–10. The first level, menu work, live-gift bindings and native Kit source artwork are preserved.

The new direction studies Getting Over It's use of irregular support surfaces, changes of climbing direction, exposed returns and drops back onto earlier terrain. It is an independent tile-based adaptation, not a copy of the original mountain mesh. Reference study: [Jackeloko's map of Bennett Foddy's mountain](https://steamcommunity.com/sharedfiles/filedetails/?id=1235672527), credited to Jackeloko; [the original game's creator page](https://foddy.net/blog/2017/09/getting-over-it/). The downloaded study images under scripts/climb-reference are analysis references only and are not imported or shipped as game artwork.

Each new course is a compact, rising cliff rather than a full-width horizontal Mario course. Three stars are distributed at different heights and side positions. The exit is at the upper-right summit. Core rules are unchanged: three stars open the door, the body must enter it, ordinary deaths retain stars and checkpoint state, and manual restart clears that level's state.

| Level | Tile theme | Authored centre-route length |
|---|---|---|
| 2 | Jungle | 311.8 |
| 3 | Beach | 311.8 |
| 4 | Town | 312.8 |
| 5 | Cave | 313.0 |
| 6 | Grassland_Winter | 312.2 |
| 7 | Ice | 312.5 |
| 8 | Castle | 312.5 |
| 9 | Volcano | 312.0 |
| 10 | Sky | 311.6 |

Lengths measure the authored landmark polyline in world units, not actual body/hammer trajectories. The new courses trade horizontal width for elevation. Added holds, retries and detours can increase real travel and completion time. No claim of equal completion time to level 1 is made.

Terrain uses native 0.8m cells. The complete occupied grid selects exposed tops, interior, sides and bottoms, so buried surfaces do not carry grass. Cliff bodies vary in thickness; small footholds are staggered around exposed rises. Long drops can return the player to a lower part of the same course. Stable checkpoint bays remain at roughly 40% of the authored route; levels 8–10 retain a second later checkpoint. These remain more forgiving than the original game's full-reset mountain.

`scripts/prepare_climb_courses.py` owns the authored landmarks and outputs the JSON plans under `scripts/climb-plans`. `Assets/Editor/ClimbCourseBuilder.cs` applies only the nine LevelXX scenes, backs up their prior files under `art-backup/pre-climb-layout`, and renders whole-course reviews. It leaves Build Settings and the menu scene untouched. To regenerate, create scripts/climb-build.request and refresh scripts outside Play. Do not run the old campaign or Mario builders to edit these new courses.

`scripts/check_climb_routes.py` checks a simplified surface graph with a maximum 1.6m upward step, near-edge spacing and at least 2.4m column headroom. All objective approaches, checkpoint approaches and exits pass that approximation. The graph does not simulate hammer impulses, momentum, swept body collision or friction; it cannot establish a complete physical playthrough.

Current evidence is under scripts/climb-evidence. Build and panorama checks describe the actual saved scenes. Play checks use a frozen, teleported rig to test triggers, gated completion, death/reload, checkpoint spawning and manual restart. Full unassisted hammer traversal, gift balance, target-device performance and export validation remain separate work. Prior Mario maps, panoramas and route reports are historical references only.

## Acceptance isolation correction

The newly added menu persists completion stars. The older campaign test runner initially did not isolate this new save path, and its scripted completions may have written three-star menu records to the normal prefix. Prior values were not captured and have not been cleared or guessed. The runner now sets GameMenuUI.Testing and a unique Goombas.Campaign.TEST prefix before test completions. Subsequent checks use that isolated prefix. This limitation must be disclosed alongside the passing trigger checks.
