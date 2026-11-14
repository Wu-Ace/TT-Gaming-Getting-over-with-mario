# Paddle arm reach limit — 2026-11-11

Added on user request: preserve fixed tail grips and original motor controls, but constrain paddle motion to both arms' reach.

- ArmsSimulation projects requested targets into the intersection of both shoulder/grip reach regions and existing slider travel. Two runtime max-distance joints enforce the actual physical reach. Motor prediction prevents driving rotation across the reach boundary; extension uses the feasible interval at the current angle.
- Natural arm span stays .98 times character growth; physical reach uses .94 times growth, leaving solver margin. Tail grip source pixels remain (24,12) and (24,40). Paddle art, original mechanical limits and colliders are unchanged.
- TargetMovement applies the same projection to its persistent mouse target. PaddleCharacterVisual no longer increases nominal arm length with grip distance.

Verification:
- compile-final.json: editor refresh completed, 0 errors / 0 warnings.
- scaled-result.json: actual Character prefab in an isolated Physics2D scene, campaign behaviours removed before activation. 24 oversized directions at each of .65 / 1 / 1.5 scale, 72 requests total. Maximum actual shoulder-to-fixed-grip distance normalized by growth: .9400011, below natural .98. Log's final natural arm=1.47 is the last scale (1.5), whereas maximum is normalized.
- support-result.json / support.txt: gravity support test raised body .4061203 units; maximum shoulder-to-grip distance .554402, below .98.
- Editor confirmed stopped on MainMenu, not compiling/updating, no unread errors. Frame restoration request found no character on MainMenu and failed harmlessly; no scene changes were saved.
- Earlier failed solver experiments retained for traceability; only final scaled/support results describe final implementation.

Not verified: full level traversal, live mouse feel, growth transitions, death lifecycle, extreme external impulses, target device or exported build. No formal saves or PlayerPrefs reset. New reach limits intentionally reduce available paddle positions/directions.
