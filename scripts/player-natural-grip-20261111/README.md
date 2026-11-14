# Natural paddle grip and body-relative elbows — 2026-11-11

User clarified the reference is Getting Over It, not PUBG, and asked to inspect GitHub remakes before correcting unnatural wrist/elbow motion. Inspected rossning92/getting-0ver-it Assets/Hand.cs, Head.cs and PlayerControl.cs via raw GitHub. Hand.cs uses whole-arm authored sprites, handle-relative direction and handedness-dependent flip. It is not a two-bone IK implementation and is not original game source. Caliburn9/GettingOverItClone only exposes basic Body/Rod physics scripts. poshih/over-the-edge search result described relevant body-relative hints but live GitHub/API returned404; its source was not available and no claim of reviewing that source is made.

Implementation is original adaptation to current six-sprite kit, with no third-party code dependency:
- Forearm and palm are separate Sprite.Create subrects of unchanged source PNGs. Temporary DontSave renderers avoid replacing persistent sprite references. Palms orient with the shaft, separately from forearm approach, two separated grips and left/right order retained.
- Body-relative 3D elbow pole points down/out and toward camera; elbow circle projected into2D produces foreshortening. Replaces nearest-planar-branch choice that could keep an elbow lifted behind the head. Forearm/upper segment uniform scale follows projected length (no aspect distortion).
- Existing adaptive grip/reach correction preserved; no physical paddle travel, input, collider, prefab, scene asset or unrelated terrain changes this turn.

PASS current editor compile0errors0warnings.
PASS actual mouse events drive original paddle physics: tip travel3.205225m, max rendered forearm wrist error1.91e-6m, min hand gap.2001352m. Native Game movie natural-grip.mp4, frame montage and input-45/75 screenshots inspected. Rotational diagnostic from old harness sees173deg when a foreshortened upper sprite passes near zero projected length; this is not an elbow-position discontinuity. Spatial sweep below measures the relevant geometry.
PASS6489 half-degree full-circle angle/reach/growth poses: wrist error<2e-6m,min gap.2001341m,max elbow displacement.03053m per.5deg normalized,no projected limb stretching beyond3D segment length.
PASS12 disable/rebuild cycles: exactly4 temporary forearm/palm renderers, cleanup removes all,original sprite asset references retained.
Actual Scene closeup inspected;editor stopped on original Level01 with clean scene. Physics test temporarily froze body,no explicit preferences writes/reset or scene saves. Full free-body traversal, subjective user acceptance, enemy/death regression, device/export NOT RUN.

Reference https://github.com/rossning92/getting-0ver-it/blob/master/Assets/Hand.cs . Source backup PaddleCharacterVisual.cs.bak. Earlier source/view attempts and all final evidence retained under this directory.
