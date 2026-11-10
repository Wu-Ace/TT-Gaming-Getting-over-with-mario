# Fall death presentation refinement — 2026-11-09

Project: E:/TT Gaming/TT-Gaming-Getting-over-with-mario
Engine: Tuanjie 1.10.3 / 2022.3.62t15. Git HEAD: 705a802068da9ed8521c87203cf55c6d2b188501. Repository exists with pre-existing dirty/untracked work (see git-status.txt); earlier no-Git handoff text is superseded by this live observation. Scoped originals: backup/.

## Change
Only fall presentation is redesigned. Impact timing, burst, shake, launch and audio branch stay as before.

- Replace the single offscreen arrow with a snapshot of the actual character/cauldron/hammer pose. Two delayed pale echoes follow a colored lead pose as it turns and accelerates through the lower screen. This is an overlay memory; physics is not teleported. Visible source sprites retain source rect ratio, shared relative pose and pivot. Only offscreen falls receive the overlay pose; onscreen falls keep the real corpse.
- Sparse upward-moving side wind strokes, expanding elliptical departure rings and a blue-black depth vignette make the downward rush visible across the screen.
- Slight camera dip/pullback, a generated descending wind/tone (no voice), 1.05s fall presentation and .24s aperture closure centered on departure, then normal async reload/.18s arrival fade. Early skip still available after .45s.
- Aperture and all fall graphics hide under full blackout before arrival fade. No residual black mask after reload. No death text, no vocal samples. Global time scale and formal save data untouched.
- Snapshot uses CanvasScaler-compatible first-frame coordinates, including high-DPI screens. No source-art/collider/terrain asset edits.

## Validation
Final play-results.txt: 26 PASS checks, resultCode 0 (final-check-result.json). Final compile.json: 0 errors; 2 existing unused-field warnings in unrelated ContactSoundEffect.cs, which this change does not edit.

Actual Play: physical top-vs-side contacts, duplicate-death suppression, impact launch and recovery, real threshold fall, actual pose snapshot creation/source aspect ratios, wind/vignette/rings, open-then-closing aperture, fall reload/cleanup, checkpoint/diamond retention, authored Koopa shell activation and actual shell side-collision death/reload. Game ScreenCapture images include fall-marker (legacy filename now shows pose), fall-descend and fall-iris. Final fall-descend and fall-iris opened and visually inspected; initial run images also inspected before first-frame scale correction. attempt-1/ retains prior passing run.

Existing open editor reused via Codely bridge. It was in Level01 Play initially. Test uses unique Goombas.Death.TEST.<GUID>. prefs; no formal prefs cleared, no scene saved. Final edit-state check: Level01 dirty=False. Test runner now has RunInEditor() so it preserves the user's editor; ordinary Play restored afterward (restore-play-result.json).

NOT RUN: full manual traversal/repeated-death feel test, on-device/export, audio listening, actual input early-skip/toggle clicks, visual checks across all resolutions, onscreen-fall visual capture. The normal onscreen-fall path is implemented but the captured threshold fixture intentionally starts offscreen. Scene layout unchanged; separate Scene-view terrain inspection not repeated.

## Re-run
From Edit Mode call DeathFeedbackAcceptance.RunInEditor() using Codely exec_editor_script. It runs isolated Play checks and returns to Edit Mode, keeping editor open. Run() is the opt-in batch entry that exits the editor. Evidence path is scripts/fall-polish-20261109; archive existing evidence before rerunning.
