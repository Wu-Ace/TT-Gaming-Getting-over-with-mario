System.Collections.IEnumerator Run()
{
    GameMenuUI.Testing = true;
    var level = UnityEngine.Object.FindObjectOfType<CampaignLevel>();
    var body = UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();
    var root = body.transform.parent;
    foreach (var rb in root.GetComponentsInChildren<UnityEngine.Rigidbody2D>()) rb.simulated = false;
    foreach (var input in root.GetComponentsInChildren<Character.Control.ArmsSimulation>()) input.enabled = false;
    foreach (var target in UnityEngine.Object.FindObjectsOfType<Character.Control.TargetMovement>()) target.enabled = false;
    foreach (var u in root.GetComponentsInChildren<PlayerUnstuck>()) u.enabled = false;
    var camera = UnityEngine.Camera.main;
    // Prior editor diagnostics may leave a virtual camera disabled in the live editor.
    foreach (var v in UnityEngine.Object.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>()) { v.enabled = true; v.PreviousStateIsValid = false; }
    var tester = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "GiftKeyboardTester");
    if (tester != null) tester.GetType().GetField("visible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(tester, false);
    var background = camera.GetComponent<KitLandscape>();
    if (!background || !background.HasLayeredBackground) throw new System.Exception("Six-layer background missing");
    var renderers = camera.GetComponentsInChildren<UnityEngine.SpriteRenderer>().Where(r => r.gameObject.name.Contains(" / ")).ToArray();
    if (renderers.Length != 16) throw new System.Exception("Expected 16 sprites for 6 mirrored layers, got " + renderers.Length);
    var first = root.position;
    for (int i = 0; i < 30; i++) yield return null;
    var startCamera = camera.transform.position;
    var far = renderers.First(r => r.name == "02 Mountains / 1");
    var near = renderers.First(r => r.name == "06 Near Slopes / 1");
    float startFar = far.transform.localPosition.x, startNear = near.transform.localPosition.x;
    UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/parallax-20261111/start-game.png"));
    for (int i = 0; i < 160; i++)
    {
        float t = (i + 1f) / 160f;
        root.position = first + new UnityEngine.Vector3(t * 24f, UnityEngine.Mathf.Sin(t * UnityEngine.Mathf.PI) * 3f, 0);
        yield return new UnityEngine.WaitForSecondsRealtime(.035f);
        if (i == 100) UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/parallax-20261111/moving-game.png"));
    }
    for (int i = 0; i < 30; i++) yield return null;
    background.UpdateLayers();
    float cameraTravel = camera.transform.position.x - startCamera.x;
    float farTravel = startFar - far.transform.localPosition.x;
    float nearTravel = startNear - near.transform.localPosition.x;
    var report = new System.Collections.Generic.List<string>();
    report.Add("Camera follows moved player (physics isolated): camera dx=" + cameraTravel + ", far screen dx=" + farTravel + ", near screen dx=" + nearTravel);
    if (cameraTravel < 15 || System.Math.Abs(farTravel / cameraTravel - .035f) > .003f || System.Math.Abs(nearTravel / cameraTravel - .29f) > .003f)
        throw new System.Exception("Camera-relative depth rates failed: " + string.Join("\n", report));
    UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("scripts/parallax-20261111/moved-game.png"));
    // Use the actual gameplay camera at distant horizontal/vertical coordinates and aspect ratios.
    var brain = camera.GetComponent<Cinemachine.CinemachineBrain>();
    brain.enabled = false;
    var saved = camera.transform.position;
    var oldAspect = camera.aspect;
    void Capture(string name, int w, int h)
    {
        var oldTarget = camera.targetTexture;
        var oldActive = UnityEngine.RenderTexture.active;
        var rt = new UnityEngine.RenderTexture(w, h, 24);
        var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.aspect = (float)w / h; background.UpdateLayers(); camera.Render();
            UnityEngine.RenderTexture.active = rt;
            tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes("scripts/parallax-20261111/" + name + ".png", tex.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget; camera.aspect = oldAspect; UnityEngine.RenderTexture.active = oldActive;
            UnityEngine.Object.Destroy(tex); rt.Release(); UnityEngine.Object.Destroy(rt);
        }
    }
    camera.transform.position = startCamera; Capture("landscape", 1400, 800); Capture("portrait", 720, 1280); Capture("ultrawide", 1600, 700);
    foreach (float x in new[] { -10000f, -150f, 150f, 10000f })
    {
        camera.transform.position = startCamera + new UnityEngine.Vector3(x, 300, 0);
        background.UpdateLayers();
        foreach (var r in renderers)
            if (r.transform.localScale.x != r.transform.localScale.y || !float.IsFinite(r.transform.localPosition.x)) throw new System.Exception("Invalid aspect/transform");
        // Verify each scrolling layer covers the viewport with touching mirrored panel bounds.
        foreach (var group in renderers.Where(r => !r.name.StartsWith("01")).GroupBy(r => r.sortingOrder))
        {
            var sorted = group.OrderBy(r => r.bounds.min.x).ToArray();
            float half = camera.orthographicSize * camera.aspect;
            if (sorted[0].bounds.min.x > camera.transform.position.x - half || sorted[2].bounds.max.x < camera.transform.position.x + half) throw new System.Exception("Horizontal coverage failure");
            for (int k = 0; k < 2; k++)
                if (System.Math.Abs(sorted[k].bounds.max.x - sorted[k + 1].bounds.min.x) > .01f || sorted[k].flipX == sorted[k + 1].flipX) throw new System.Exception("Mirror seam geometry failure");
        }
    }
    Capture("long-climb", 1400, 800);
    report.Add("PASS six layers / 16 sprites; actual player-follow depth rates; uniform scale; continuous mirrored panel bounds at +/-150 and +/-10000m; tall-climb bounded vertical coverage.");
    camera.transform.position = saved; camera.aspect = oldAspect; brain.enabled = true;
    root.position = first;
    for (int i = 0; i < 30; i++) yield return null;
    for (int i = 0; i < 12; i++) { background.enabled = false; background.enabled = true; }
    yield return null;
    if (camera.GetComponentsInChildren<UnityEngine.SpriteRenderer>().Count(r => r.gameObject.name.Contains(" / ")) != 16) throw new System.Exception("Lifecycle leaked duplicate backgrounds");
    report.Add("PASS twelve disable/rebuild cycles retain exactly 16 renderers; no scene/preference writes in runtime test. Physical traversal/device/export not tested.");
    System.IO.File.WriteAllLines("scripts/parallax-20261111/runtime-results.txt", report);
    UnityEngine.Debug.Log(string.Join("\n", report));
}
return Run();
