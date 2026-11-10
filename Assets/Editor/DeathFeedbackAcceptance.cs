using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Character.Lifecycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in isolated Play regression. Never saves authored scenes or clears formal preferences.
[InitializeOnLoad]
public static class DeathFeedbackAcceptance
{
    const string Out = "scripts/fall-polish-20261109/";
    static int stage, deaths, tops, reloads;
    static double due, deadline, loadedAt;
    static Death body;
    static GameObject enemy, shell;
    static Vector2 checkpoint;
    static float deathTime;
    static string log;
    static DeathFeedbackAcceptance() { EditorApplication.playModeStateChanged += Changed; }
    public static void Run() { SessionState.SetBool("DeathFeedbackAcceptance.CloseEditor", true); RunCore(); }
    public static void RunInEditor() { SessionState.SetBool("DeathFeedbackAcceptance.CloseEditor", false); RunCore(); }
    static void RunCore()
    {
        Directory.CreateDirectory(Out);
        SessionState.SetBool("DeathFeedbackAcceptance", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        gameView.Show();
        gameView.Focus();
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DeathFeedbackAcceptance", false))
        {
            SessionState.SetBool("DeathFeedbackAcceptance", false);
            Application.runInBackground = true;
            GameMenuUI.Testing = true;
            GameMenuUI.PreferencePrefix = "Goombas.Death.TEST." + Guid.NewGuid() + ".";
            log = "UTC " + DateTime.UtcNow.ToString("O") + "\nIsolated physical contacts and forced fall; not full traversal/device/audio listening.\n";
            stage = deaths = tops = reloads = 0;
            due = EditorApplication.timeSinceStartup + 1;
            deadline = due + 55;
            SceneManager.sceneLoaded += Loaded;
            EditorApplication.update += Tick;
        }
        if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("DeathFeedbackAcceptance.Exit", false))
        {
            SessionState.SetBool("DeathFeedbackAcceptance.Exit", false);
            if (SessionState.GetBool("DeathFeedbackAcceptance.CloseEditor", false)) EditorApplication.Exit(SessionState.GetInt("DeathFeedbackAcceptance.Code", 1));
        }
    }
    static void Loaded(Scene scene, LoadSceneMode mode) { reloads++; loadedAt = EditorApplication.timeSinceStartup; }
    static void Check(bool ok, string message)
    {
        log += (ok ? "PASS " : "FAIL ") + message + "\n";
        File.WriteAllText(Out + "play-results.txt", log);
        if (!ok) throw new Exception(message);
    }
    static void Prepare()
    {
        body = UnityEngine.Object.FindObjectOfType<Death>();
        foreach (var tester in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().Where(x => x.GetType().Name == "GiftKeyboardTester"))
            tester.GetType().GetField("visible", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(tester, false);
        foreach (var arm in UnityEngine.Object.FindObjectsOfType<Character.Control.ArmsSimulation>()) arm.enabled = false;
        foreach (var joint in body.transform.parent.GetComponentsInChildren<Joint2D>()) joint.enabled = false;
        foreach (var rb in body.transform.parent.GetComponentsInChildren<Rigidbody2D>())
        { rb.gravityScale = 0; rb.velocity = Vector2.zero; rb.angularVelocity = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation; }
        foreach (var collider in body.transform.parent.GetComponentsInChildren<Collider2D>())
            if (collider.attachedRigidbody != body.GetComponent<Rigidbody2D>()) collider.enabled = false;
    }
    static void Move(Vector2 position)
    {
        var delta = position - (Vector2)body.transform.position;
        foreach (var rb in body.transform.parent.GetComponentsInChildren<Rigidbody2D>()) rb.position += delta;
        Physics2D.SyncTransforms();
    }
    static void Fixture(bool top)
    {
        var hit = body.GetComponentsInChildren<Collider2D>().First(c => c.enabled && c.attachedRigidbody == body.GetComponent<Rigidbody2D>());
        var b = hit.bounds;
        enemy = new GameObject("Death test enemy side contact", typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer));
        enemy.tag = "Enemy";
        var col = enemy.GetComponent<BoxCollider2D>(); col.size = new Vector2(.65f, .65f);
        var rb = enemy.GetComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        enemy.transform.position = top ? new Vector3(b.center.x, b.min.y - .5f, 0) : new Vector3(b.max.x + .5f, b.center.y, 0);
        var source = UnityEngine.Object.FindObjectsOfType<Enemy.EnemyDeath>().SelectMany(e => e.GetComponentsInChildren<SpriteRenderer>()).FirstOrDefault(r => r.sprite != null);
        if (source != null) { enemy.GetComponent<SpriteRenderer>().sprite = source.sprite; enemy.transform.localScale = Vector3.one * (.65f / source.sprite.bounds.size.x); col.size = Vector2.one * .65f / enemy.transform.localScale.x; }
        rb.velocity = top ? Vector2.up * 2 : Vector2.left * 2;
        Physics2D.SyncTransforms();
    }
    static void Capture(string name) { ScreenCapture.CaptureScreenshot(Out + name + ".png"); }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out at stage " + stage);
            if (EditorApplication.timeSinceStartup < due) return;
            switch (stage)
            {
                case 0:
                    Prepare(); Move((Vector2)body.transform.position + Vector2.up * 3);
                    body.GetComponent<EnemyContactDetection>().OnEnemyTopContactDetected += _ => tops++;
                    body.OnCharacterDeath += () => deaths++;
                    Fixture(true); stage = 1; due += .4; break;
                case 1:
                    Check(tops > 0 && !body.Dead, "Physical top contact remains a stomp, no death feedback");
                    UnityEngine.Object.Destroy(enemy);
                    body.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                    stage = 2; due += .15; break;
                case 2:
                    Fixture(false); stage = 3; due += .02; break;
                case 3:
                    if (!body.Dead) { due = EditorApplication.timeSinceStartup + .01; break; }
                    deathTime = Time.unscaledTime;
                    Check(deaths == 1 && !body.GetComponent<DeathFeedback>().Falling, "Physical side contact starts impact death exactly once");
                    Check(body.transform.parent.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Whole dead rig no longer collides");
                    body.Kill(); body.Kill();
                    Check(deaths == 1, "Duplicate death calls are suppressed");
                    Check(!Camera.main.GetComponent<Cinemachine.CinemachineBrain>().enabled, "Death camera stops following corpse");
                    Check(UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>().All(t => !t.gameObject.name.Contains("Death")), "Death overlay contains no text");
                    GameMenuUI.Instance.OpenSettings(); Check(!GameMenuUI.IsPaused, "Death cannot be paused by settings");
                    Capture("impact-contact"); stage = 4; due = EditorApplication.timeSinceStartup + .12; break;
                case 4:
                    if (body != null && body.GetComponent<DeathFeedback>().Elapsed < .12f) { due = EditorApplication.timeSinceStartup + .01; break; }
                    Check(body.GetComponent<Rigidbody2D>().simulated && body.GetComponent<Rigidbody2D>().velocity.y > 0, "Corpse releases after local hit stop and launches upwards (simulated=" + body.GetComponent<Rigidbody2D>().simulated + ", vy=" + body.GetComponent<Rigidbody2D>().velocity.y + ", elapsed=" + body.GetComponent<DeathFeedback>().Elapsed + ")");
                    Capture("impact-launch");
                    stage = 5; due += .28; break;
                case 5:
                    if (body != null && body.GetComponent<DeathFeedback>().Elapsed < .4f) { due = EditorApplication.timeSinceStartup + .01; break; }
                    Capture("impact-fall"); stage = 6; due += 1.1; break;
                case 6:
                    if (reloads < 1 || EditorApplication.timeSinceStartup - loadedAt < .35) { due = EditorApplication.timeSinceStartup + .05; break; }
                    Check(reloads == 1 && !UnityEngine.Object.FindObjectOfType<Death>().Dead, "Impact death reloads and revives after short presentation");
                    Check(!DeathFeedback.Active && Camera.main.GetComponent<Cinemachine.CinemachineBrain>().enabled, "Camera and feedback state restored on spawn");
                    Check(UnityEngine.Object.FindObjectOfType<Character.Control.TargetMovement>().enabled, "Input target restored on spawn");
                    Check(UnityEngine.Object.FindObjectOfType<DeathArrivalFade>() == null, "Arrival blackout cleaned up");
                    Prepare(); checkpoint = CampaignLevel.Current.SpawnPosition;
                    CampaignLevel.Current.Collect(0); CampaignLevel.Current.Checkpoint(checkpoint);
                    var threshold = (Transform)typeof(FallDetection).GetField("deathThreshold", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(body.GetComponent<FallDetection>());
                    Move(new Vector2(body.transform.position.x, threshold.position.y - 1));
                    stage = 7; due += .03; break;
                case 7:
                    Check(body.Dead && body.GetComponent<DeathFeedback>().Falling, "Crossing actual fall threshold triggers separate fall feedback");
                    var presentation = UnityEngine.Object.FindObjectOfType<FallDeathPresentation>();
                    Check(presentation != null && presentation.Offscreen && presentation.SnapshotCount > 0, "Offscreen fall has actual character-pose echoes");
                    var echoImages = presentation.GetComponentsInChildren<UnityEngine.UI.Image>().Where(i => i.gameObject.name == "Fall silhouette").ToArray();
                    Check(echoImages.All(i => Mathf.Abs(i.rectTransform.sizeDelta.x / i.rectTransform.sizeDelta.y - i.sprite.rect.width / i.sprite.rect.height) < .001f), "Fall pose images preserve source sprite aspect ratios");
                    Check(UnityEngine.Object.FindObjectOfType<FallIrisGraphic>().Closure == 0, "Iris is open during the fall rather than hiding the pose");
                    Check(GameObject.Find("Fall wind streak") != null && GameObject.Find("Fall depth vignette") != null && GameObject.Find("Fall air ring") != null, "Fall has screen wind, depth vignette and departure rings");
                    body.Kill(); Check(body.GetComponent<DeathFeedback>().Falling, "Duplicate hazard during fall does not replace presentation");
                    Capture("fall-marker"); stage = 8; due += .18; break;
                case 8:
                    Check(body.GetComponent<Rigidbody2D>().velocity.y < 0 && body.GetComponent<Rigidbody2D>().constraints != RigidbodyConstraints2D.FreezeAll, "Fall corpse continues down instead of freezing");
                    Capture("fall-descend"); stage = 15; due = EditorApplication.timeSinceStartup + .05; break;
                case 15:
                    if (body.GetComponent<DeathFeedback>().Elapsed < 1.2f) { due = EditorApplication.timeSinceStartup + .01; break; }
                    Check(UnityEngine.Object.FindObjectOfType<FallIrisGraphic>().Closure > 0, "Fall ends with closing aperture rather than plain fade");
                    Capture("fall-iris"); stage = 9; due = EditorApplication.timeSinceStartup + .8; break;
                case 9:
                    if (reloads < 2 || EditorApplication.timeSinceStartup - loadedAt < .35) { due = EditorApplication.timeSinceStartup + .05; break; }
                    Check(reloads == 2 && !UnityEngine.Object.FindObjectOfType<Death>().Dead, "Fall death reloads and revives");
                    Check(CampaignLevel.Current.HasStar(0) && Vector2.Distance(CampaignLevel.Current.SpawnPosition, checkpoint) < .01f, "Death preserves collected diamond and checkpoint");
                    Check(UnityEngine.Object.FindObjectOfType<DeathArrivalFade>() == null && !DeathFeedback.Active, "Second death fully cleans transition state");
                    Capture("respawn"); stage = 10; due = EditorApplication.timeSinceStartup + .2; break;
                case 10:
                    GameMenuUI.Instance.OpenSettings(); Capture("settings"); stage = 11; due += .2; break;
                case 11:
                    GameMenuUI.Instance.CloseSettings(); Prepare();
                    Move((Vector2)body.transform.position + Vector2.up * 3);
                    deaths = 0; body.OnCharacterDeath += () => deaths++;
                    enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/KoopaTroopa.prefab"));
                    var koopa = enemy.GetComponentInChildren<Enemy.EnemyDeath>();
                    enemy.transform.position += body.transform.position + Vector3.right * 4 - koopa.transform.position;
                    koopa.OnEnemyDeath += visual => shell = visual;
                    koopa.SendMessage("DieIfSelf", koopa.gameObject);
                    stage = 12; due = EditorApplication.timeSinceStartup + .2; break;
                case 12:
                    Check(shell != null && shell.activeInHierarchy, "Authored Koopa stomp handler still activates its shell");
                    foreach (var movement in enemy.GetComponentsInChildren<Enemy.EnemyMovement>()) movement.enabled = false;
                    var shellBody = shell.GetComponentInParent<Rigidbody2D>();
                    shellBody.bodyType = RigidbodyType2D.Kinematic; shellBody.gravityScale = 0;
                    var shellBounds = shell.GetComponentInChildren<Collider2D>().bounds;
                    var bodyBounds = body.GetComponentsInChildren<Collider2D>().First(c => c.enabled && c.attachedRigidbody == body.GetComponent<Rigidbody2D>()).bounds;
                    enemy.transform.position += new Vector3(bodyBounds.max.x + .12f - shellBounds.min.x, bodyBounds.center.y - shellBounds.center.y, 0);
                    shellBody.velocity = Vector2.left * 2; Physics2D.SyncTransforms();
                    stage = 13; due = EditorApplication.timeSinceStartup + .02; break;
                case 13:
                    if (!body.Dead) { due = EditorApplication.timeSinceStartup + .02; break; }
                    Check(deaths == 1 && !body.GetComponent<DeathFeedback>().Falling, "Actual active shell side collision triggers exactly one impact death");
                    Capture("shell-impact"); stage = 14; due = EditorApplication.timeSinceStartup + 1.5; break;
                case 14:
                    if (reloads < 3 || EditorApplication.timeSinceStartup - loadedAt < .35) { due = EditorApplication.timeSinceStartup + .05; break; }
                    Check(!UnityEngine.Object.FindObjectOfType<Death>().Dead && !DeathFeedback.Active && UnityEngine.Object.FindObjectOfType<DeathArrivalFade>() == null, "Shell collision death completes reload and presentation cleanup");
                    Finish(0); break;
            }
        }
        catch (Exception e) { log += "FAIL " + e + "\n"; File.WriteAllText(Out + "play-results.txt", log); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SceneManager.sceneLoaded -= Loaded;
        SessionState.SetInt("DeathFeedbackAcceptance.Code", code);
        SessionState.SetBool("DeathFeedbackAcceptance.Exit", true);
        EditorApplication.ExitPlaymode();
    }
}
