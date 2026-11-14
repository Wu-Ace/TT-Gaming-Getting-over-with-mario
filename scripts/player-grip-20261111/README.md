# Separate fixed paddle grips and continuous elbow motion — 2026-11-11

User reported overlapping grip points and sudden hand jumps. Previous rig projected a shared sliding grip center, separated hands by only .17m, and used a stateless shoulder-to-wrist direction that flips across the shoulder singularity. Replaced with two fixed material points at1.10m and1.70m from blade tip, .60m apart at normal scale. No slide, clamp, hand swap or lag at the wrist. Elbow direction keeps per-arm body-relative state, holds near the shoulder singularity and turns at<=540deg/s with a27deg/frame hitch cap. Forearm ends exactly at each grip, using uniform per-segment sprite scale to accommodate the unchanged physical slider range. Original aspect ratios preserved; physics, prefab, scene assets, input and gifts unchanged this turn.

PASS current editor compile0errors0warnings (compile.json).
PASS actual InputSystem mouse events through TargetMovement/ArmsSimulation, body frozen to avoid progressing: physical paddle tip travel3.2271m, max rendered wrist error1.91e-6m, min hand separation.5999983m. Recorded native Game View fixed-grips.mp4 and inspected six frames plus input-45/75 captures.
PASS repeated shoulder crossings: max single-frame upper arm turn26.99994deg, wrist error1.97e-6m, hand separation.6m (singularity.txt). This targets the prior180deg flip, not a claim of user-calibrated animation quality.
PASS648 full rotation/travel/growth combinations: max rendered wrist error1.97e-6m (range.txt). Static pose checks do not replace dynamic verification above.
PASS final actual Scene View closeup inspected. Editor restored stopped Level01; no scene saves or explicit PlayerPrefs writes/reset. No free-body full traversal, device/export, or death regression this turn.

Scoped source backup in backup/. This correction supersedes the previous sliding-grip/equal-bone approach in scripts/player-art-20261111; prior evidence retained unmodified.
