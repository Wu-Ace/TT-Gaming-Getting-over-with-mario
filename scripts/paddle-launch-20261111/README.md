# Paddle downward stroke response

User: quickly pushing paddle down against terrain should launch the character, as with the earlier Mario version.

Source comparison: archived ArmsSimulation used a contact-dependent body force. Current motor rewrite removed that branch, capped extension speed at2, and applied only half MaxBodyForce. Target projection also discarded stroke speed after clipping to the hand-reach region. This explains weakened response but does not prove every reported terrain case: the baseline isolated fixture already launched.

Changes: TargetMovement forwards valid world-space mouse displacement and frame duration before position projection. ArmsSimulation uses bounded radial mouse velocity (maximum8) as extension motor feed-forward, decays it over .06 seconds and retains existing position servo. Motor force now uses full existing MaxBodyForce (400 in actual prefab). No direct upward force, jump button, collider edit, grip sliding or arm stretching. Physical reach constraints remain .94*growth, natural span .98*growth.

Verification on existing editor, isolated actual Character prefab with campaign behaviours removed before activation and local Physics2D floor:
- compile.json:0 errors/0 warnings.
- Same fast downward input, 5x60px over 5 physics steps: baseline upward speed2.183461, peak rise1.277551; after speed3.993566, peak rise1.215966. Launch impulse/speed improved, maximum height did not improve in this persistent-target fixture. Max observed arm reach after .9283231<.98. Input timing follows actual editor frame duration; this is not a deterministic user-device mouse trace.
- reach-result.json:72 oversized targets over three sizes; normalized max shoulder-to-grip .9404781<.98.
- Tests clean up their temporary scene and restore Edit Mode; no formal saves reset.

Not verified: screenshot's exact real terrain/contact, actual mouse play, full traversal, device/export, death and pause lifecycle. These tests establish stronger isolated physical stroke response, not complete live gameplay acceptance.
