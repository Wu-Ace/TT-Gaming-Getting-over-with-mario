# Upward paddle raise fix

Cause: old slider maximum blade radius was ~1.597, while upward fixed-tail grip geometry requires at least ~1.797 at baseline size. Additionally the rotation guard waited for a feasible radius while extension alignment could suppress the movement needed to reach it.

Change: add .65*growth clearance to the slider's extension travel, retaining both .94*growth physical arm-distance constraints. Coordinate extension with a five-degree look-ahead rotation interval and retain at least 25% extension drive during turns. Tail grips, arm span and collider geometry unchanged.

Validation: editor refresh 0 errors/warnings. Isolated actual-prefab physical left/up/right sequence reaches upward axis (0,1), angle error 0. Continuous upward mouse deltas through TargetMovement reach axis (-.07,1), error -.20 degrees. These are isolated physics/input tests, not a visual or full traversal acceptance.

Reach regression initial run reported failure solely because 7 formerly oversized targets became legal for the enlarged character after slider clearance increased; actual normalized maximum .9405025 remained below .98. Final regression uses target radius 3*growth to maintain oversized input at every size. Raw results retained.
