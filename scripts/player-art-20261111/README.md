# Player art replacement — 2026-11-11

Shared Character prefab now uses six original Player PNGs from Assets/Art/11.10: torso/cauldron, paddle and two segments per arm. PaddleCharacterVisual is visual-only, ExecuteAlways, updates after physical/input movement. Two-bone IK uses separate shoulder anchors, opposite elbow bend sides and sliding shaft grip points; each sprite keeps its original aspect ratio with uniform scale. Paddle blade end follows original physical Head anchor. Legacy renderers remain for references but are disabled. No collider, rigidbody, joint or input code changed. Original serialized ten physics components preserved verbatim after engine save attempted a format-only upgrade.

PASS engine refresh: 0 errors / 0 warnings (compile-final.json).
PASS all 11 gameplay scenes inherit new rig, old art hidden; MainMenu has no player (scenes.txt). Additive scene inspection generated duplicate global-light warnings during loading, closed without saving; these are harness artifacts, not compilation errors.
PASS actual Play eight-direction poses; screenshots pose-0..7 inspected representative directions.
PASS 648 poses: 72 directions x 3 reaches x 3 synchronous body/tool scales, final maximum hand grip error 0 (range.txt). Initial failed range tests led to equal bone lengths with individually uniform segment scale to remove fully folded elbow dead zone. This is pose geometry, not full traversal.
PASS actual InputSystem mouse event -> TargetMovement -> ArmsSimulation physical paddle motion, max hand error0, alive (input.txt). Body temporarily frozen to avoid moving campaign progress; no explicit preferences writes/reset or scene saves. Native Game View movie mouse-follow.mp4; six frames extracted and inspected. Scene-final.png also inspected.
NOT RUN: full free-body hammer traversal, repeated death/restart visual regression, subjective user feel, target device or export.

Original prefab backup in backup/. Original editor restored to stopped Level01, new Character selected/framed. Scene assets and unrelated gifts unchanged. Existing untracked .skill-install/game-studios and user-supplied art preserved. Initial batch editor attempt was rejected by existing-project lock; all successful work used the existing editor bridge.
