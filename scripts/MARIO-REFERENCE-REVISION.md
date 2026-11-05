> Historical revision: replaced by the Getting Over It inspired climbing courses. See CLIMB-COURSES.md.

# Supplied-map revision — 2026-11-04

This replaces the first generated layouts for levels 2–10. Level 1 is retained. The user supplied eight original Super Mario Bros. map strips, and asked to omit the ending staircase and adapt unreachable spans/heights to the hammer.

| Our level | Tile theme | Supplied reference |
|---|---|---|
| 2 | Jungle | 1-2 underground brick corridor, pillars and gaps |
| 3 | Beach | 1-3 treetop columns and discontinuous supports |
| 4 | Town | 3-1 ground obstacles, platforms and alternate elevations |
| 5 | Cave | 3-3 high/low column sequence |
| 6 | Grassland_Winter | 4-2 split-height underground corridors |
| 7 | Ice | 5-2 obstacle groups and elevated platforms |
| 8 | Castle | 6-1 broken ground and stepped obstacles |
| 9 | Volcano | 7-1 low/high platform groups |
| 10 | Sky | 1-3 first half plus adapted 3-3 second half |

The reference images are retained under scripts/mario-reference. prepare_mario_reference.py traces solid shapes on the 16-pixel reference grid, converts them to the project's native 0.8-unit tile grid and outputs inspectable JSON plans. This is a geometric adaptation, not an exact emulator reproduction. Source height is compressed to 65%, high faces receive staggered footholds and long gaps receive separate small supports. Original moving platforms become fixed intermediate supports; water sections/warp bonus rooms are not new gameplay systems. The final staircase and terminal wall are replaced by a short level exit apron. Objective stars use existing reference landmarks rather than adding three matching large platforms.

All new courses span about 304 world units, with the exit at x300. Vertical travel and detours vary with each layout; no measured completion-time claim is made. Each course retains one unique tile theme, three stars, body-only collection, checkpoint persistence and far-right gated exit. Native sprites are repeated without non-uniform scale. Terrain top/interior/side/bottom roles are selected against the complete occupancy grid to avoid grass inside solid terrain.

check_mario_routes.py checks a surface graph with a maximum 1.6m rise, 1.6m clear gap and 2.4m headroom. All three objectives, checkpoints and exit must belong to the start-connected graph. This is an approximation of available supports, NOT a physical hammer simulation and NOT a guarantee of unassisted completion. Real play feel and gift-assisted difficulty remain to be calibrated.

MarioReferenceBuilder.cs applies the JSON plans only to levels 2–10. Its request file is scripts/mario-reference-build.request; generation only occurs when explicitly requested, during an editor reload. Earlier scene versions are preserved under art-backup/pre-mario-layout. The first coarse manual draft is archived there and is not the final implementation.

Current evidence is under scripts/mario-evidence: build.txt, route-graph.txt, Lxx-map.png and maps-contact.png. Play trigger/death/checkpoint tests are copied there after running on the final scenes. Panorama renders and test teleports do not replace complete physics traversal. Old scripts/level-evidence layout captures and old length estimates describe the superseded revision.
