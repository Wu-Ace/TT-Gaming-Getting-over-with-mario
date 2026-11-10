# Death feedback — 2026-11-09

Project: E:/TT Gaming/TT-Gaming-Getting-over-with-mario
Engine: Tuanjie 1.10.3 / 2022.3.62t15 (06f842492117).
No Git repository; scoped originals are in backup/. Source/evidence SHA-256 values are in manifest.json.

## Delivered
- Separate fall and enemy impact presentation. No death text or vocal/cry samples.
- Impact: 60ms local rig hold (global time scale unchanged), contact-point pixel stars, 3.5m/s horizontal launch away from contact and 7m/s upward launch, spin, camera shake/3% push-in. Full rig collisions/joints/active arm control are disabled once dead.
- Fall: downward velocity/spin, short streaks, lower safe-area arrow when offscreen, slight camera downshift; no frozen corpse.
- Camera stops following the corpse. Corpse SortingGroup renders above world layers. Source character proportions unchanged.
- Synthesized nonvocal impact/fall cues respect existing SFX volume; looping music briefly ducks and recovers.
- Presentation defaults: impact 1.05s / fall .85s, then .15s blackout; async scene loading; .18s arrival fade. Actual loading time is additional. After .45s a new space/enter/click/tap skips to blackout.
- Settings provide independently saved death-shake and flash toggles. Settings cannot interrupt active death feedback.
- Existing Kill(), OnCharacterDeath, checkpoint and collected-diamond behavior retained; duplicate triggers ignored.

## Verification
Final run: see play-results.txt (UTC timestamp). Compile PASS: no C# errors or warnings in final editor.log. Isolated actual Play PASS: physical top contact vs side contact; exactly one death; dead-rig collision removal; camera/input recovery; local hold release/upward launch; real fall-threshold trigger/downward movement; lower-screen marker; two complete transitions/cleanup; diamond/checkpoint retention. Authored Koopa stomp-handler activation and actual active-shell physical side contact/death/reload/cleanup also PASS.

Actual Game screenshots captured by Unity ScreenCapture in graphical Editor Play: impact-contact/launch/fall, fall-marker/descend, respawn, settings and shell-impact. Final contact, fall marker and shell images opened and visually inspected; same production sources as the previously inspected launch/settings captures in attempt-3/.

Tests use a unique Goombas.Death.TEST.<GUID>. preference prefix and temporary physics fixtures. No formal progress clearing or authored scene saving. Test exit stops Play and closes the editor launched for this run. Existing gifts retained.

NOT RUN: full manual hammer traversal, repeated-death feel playtest, physical device/export, audio listening, input-driven early-skip and settings-toggle interaction, all aspect ratios/safe areas. Synthetic audio contains only generated tones/noise by construction. Scene layout was not edited; separate Scene-view terrain inspection not repeated.

## Earlier attempts
attempt-1/: batch Play passed impact/reload checks, then arrival-cleanup assertion ran before load+fade completed; no Game capture in batch mode.
attempt-2/: graphical Play capture succeeded; wall-clock assertion ran before sufficient game frames elapsed. Both harness checks now wait for actual scene arrival/game elapsed time.
attempt-3/: all initial checks PASS. Final run adds the shell branch; production source is unchanged from attempt-3.

## Re-run
Launch the editor on this project with -executeMethod DeathFeedbackAcceptance.Run -logFile <new-log-path>. It opens Level01, runs isolated Play checks, captures images, stops Play and exits with the result code. The harness writes its evidence into this folder; archive previous evidence before rerunning.
