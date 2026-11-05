# First two levels / Level 02 authored revision

Scope: OneOneScene keeps its post-diamond geometry unchanged. Level02_Jungle is now the authored horizontal course. Levels 03–10 match the pre-objective-diamonds backups byte-for-byte. Existing Level.prefab also matches the scoped pre-layout backup after terrain topology repair.

PASS: actual editor compilation; six objective sprite renders; actual L02 Scene and Game view inspection; initial mouse-controlled hammer sample; simplified surface graph to all three diamonds; two-scene Play body-only collection, duplicate rejection, locked/unlocked door, death/reload retention, checkpoint spawn and fresh restart. Play progress tests use GameMenuUI.Testing and a unique TEST preference prefix.

PASS interaction checks: Character-layer physics probe activates coin block once; 3x body force/torque restores after 10s; 1.5x growth restores; Koopa shell movement, final defeat and cleanup. Separate falling physics probe confirms top-contact creates shell and moving shell defeats Goomba. Probe placement failures and a build collider lookup failure are retained as attempts; latest successful results are interaction-study/results.txt and shell-contact.txt.

Fixed gameplay defect: EnemyDeath now separates shelled from finally defeated, permitting final defeat after shell activation without allowing duplicate stomp transitions.

NOT RUN: full unassisted mouse traversal, comprehensive per-direction block/hammer contacts, complete power-up feel comparison, gift balance, device and export checks. Teleported collection tests and contact probes do not prove full traversal.

Sources: scripts/level02-horizontal.json; Assets/Editor/Level02Designed.cs. Backup: art-backup/pre-level02-designed. Editor left stopped on Level02_Jungle. Design: design/levels/level02-jungle.md.
