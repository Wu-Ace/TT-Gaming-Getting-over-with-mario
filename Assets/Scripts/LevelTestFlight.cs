#if UNITY_EDITOR
using Character.Lifecycle;
using UnityEngine;
using UnityEngine.InputSystem;

// Editor-only level navigation. Never serialized into scenes or included in players.
[DefaultExecutionOrder(10000)]
public sealed class LevelTestFlight : MonoBehaviour
{
    Transform rig;
    Rigidbody2D[] bodies;
    bool[] simulated;
    bool flying;

    void Awake() { rig = transform.parent != null ? transform.parent : transform; }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f11Key.wasPressedThisFrame) SetFlying(!flying);
        if (!flying || Time.timeScale == 0) return;
        var direction = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
            (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        float speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 24f : 8f;
        rig.position += (Vector3)(Vector2.ClampMagnitude(direction, 1) * speed * Time.deltaTime);
        Physics2D.SyncTransforms();
    }
    void SetFlying(bool value)
    {
        if (value == flying) return;
        if (value)
        {
            bodies = rig.GetComponentsInChildren<Rigidbody2D>(true);
            simulated = new bool[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                simulated[i] = bodies[i].simulated;
                bodies[i].velocity = Vector2.zero;
                bodies[i].angularVelocity = 0;
                bodies[i].simulated = false;
            }
        }
        else if (bodies != null)
            for (int i = 0; i < bodies.Length; i++)
                if (bodies[i])
                {
                    bodies[i].velocity = Vector2.zero;
                    bodies[i].angularVelocity = 0;
                    bodies[i].simulated = simulated[i];
                }
        flying = value;
    }
    void OnDisable() { SetFlying(false); }
    void OnGUI()
    {
        GUI.Label(new Rect(12, 90, 650, 25), flying
            ? "TEST FLIGHT ON | WASD move | Shift boost | F11 resume physics"
            : "F11: level test flight (WASD / Shift boost)");
    }
    [UnityEditor.InitializeOnLoadMethod]
    static void Register()
    {
        UnityEditor.EditorApplication.update -= Attach;
        UnityEditor.EditorApplication.update += Attach;
    }
    static void Attach()
    {
        if (!Application.isPlaying) return;
        foreach (var player in FindObjectsOfType<Death>())
            if (!player.GetComponent<LevelTestFlight>()) player.gameObject.AddComponent<LevelTestFlight>();
    }
}
#endif

