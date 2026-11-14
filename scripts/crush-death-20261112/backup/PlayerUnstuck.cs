using System.Collections.Generic;
using Character.Lifecycle;
using UnityEngine;

// Safe poses are kept in memory only; recovery never writes campaign progress.
[DefaultExecutionOrder(200)]
public sealed class PlayerUnstuck : MonoBehaviour
{
    public float penetrationTolerance = .06f;
    public float stuckSeconds = .65f;
    public int RecoveryCount { get; private set; }
    sealed class Pose
    {
        public Vector2[] positions;
        public float[] angles;
        public Vector3 root;
        public Vector2 body;
    }
    readonly List<Pose> safe = new List<Pose>();
    Rigidbody2D[] bodies;
    Collider2D[] colliders;
    Rigidbody2D body;
    Death death;
    Transform rig;
    float embeddedTime, sampleTime, cooldown;
    Vector2 embeddedOrigin;

    void Start()
    {
        death = GetComponent<Death>();
        body = GetComponent<Rigidbody2D>();
        rig = transform.parent;
        if (death == null || body == null || rig == null) { enabled = false; return; }
        bodies = rig.GetComponentsInChildren<Rigidbody2D>();
        colliders = rig.GetComponentsInChildren<Collider2D>();
    }
    bool Own(Collider2D c) => c.transform.IsChildOf(rig);
    bool Hazard(Collider2D c)
    {
        var item = c.GetComponentInParent<CampaignInteractable>();
        return c.CompareTag("Enemy") || c.GetComponentInParent<CampaignEnemy>() != null ||
            (item != null && item.kind == CampaignInteractable.Kind.Spikes);
    }
    bool Clear(bool bodyOnly, float tolerance)
    {
        foreach (var c in colliders)
        {
            if (!c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger ||
                (bodyOnly && c.attachedRigidbody != body)) continue;
            var bounds = c.bounds;
            foreach (var other in Physics2D.OverlapBoxAll(bounds.center, bounds.size + Vector3.one * .02f, 0))
            {
                if (Own(other)) continue;
                if (Hazard(other)) return false;
                if (other.isTrigger || Physics2D.GetIgnoreLayerCollision(c.gameObject.layer, other.gameObject.layer) ||
                    Physics2D.GetIgnoreCollision(c, other)) continue;
                var distance = c.Distance(other);
                if (distance.isValid && distance.isOverlapped && distance.distance < -tolerance) return false;
            }
        }
        return true;
    }
    bool Supported()
    {
        foreach (var c in colliders)
        {
            if (!c.enabled || c.isTrigger || c.attachedRigidbody != body) continue;
            var b = c.bounds;
            foreach (var hit in Physics2D.RaycastAll(new Vector2(b.center.x, b.min.y + .02f), Vector2.down, .14f))
            {
                var ground = hit.collider;
                if (Own(ground) || ground.isTrigger || Hazard(ground) || hit.normal.y < .7f) continue;
                if (Physics2D.GetIgnoreLayerCollision(c.gameObject.layer, ground.gameObject.layer) ||
                    Physics2D.GetIgnoreCollision(c, ground)) continue;
                // Moving/falling platforms cannot be trusted as long-lived fallback landings.
                if (ground.attachedRigidbody == null || ground.attachedRigidbody.bodyType == RigidbodyType2D.Static) return true;
            }
        }
        return false;
    }
    Pose Capture()
    {
        var p = new Pose { positions = new Vector2[bodies.Length], angles = new float[bodies.Length], root = rig.position, body = body.position };
        for (int i = 0; i < bodies.Length; i++) { p.positions[i] = bodies[i].position; p.angles[i] = bodies[i].rotation; }
        return p;
    }
    void Apply(Pose p)
    {
        rig.position = p.root;
        for (int i = 0; i < bodies.Length; i++) { bodies[i].transform.SetPositionAndRotation(new Vector3(p.positions[i].x, p.positions[i].y, bodies[i].transform.position.z), Quaternion.Euler(0, 0, p.angles[i])); bodies[i].position = p.positions[i]; bodies[i].rotation = p.angles[i]; }
        Physics2D.SyncTransforms();
    }
    public static float ProgressAt(Vector2 position, Vector2[] route)
    {
        float total = 0, along = 0, nearest = float.PositiveInfinity;
        for (int i = 1; i < route.Length; i++)
        {
            Vector2 segment = route[i] - route[i - 1];
            float length = segment.magnitude;
            if (length < .001f) continue;
            float t = Mathf.Clamp01(Vector2.Dot(position - route[i - 1], segment) / segment.sqrMagnitude);
            float distance = (position - route[i - 1] - segment * t).sqrMagnitude;
            if (distance < nearest) { nearest = distance; along = total + length * t; }
            total += length;
        }
        return along;
    }
    Vector2[] Route()
    {
        var level = CampaignLevel.Current;
        if (level == null) return null;
        if (level.route != null && level.route.Length >= 2) return level.route;
        var exit = FindObjectOfType<Winning.WinThresholdDetection>();
        return exit == null ? null : new[] { level.initialSpawn, (Vector2)exit.transform.position };
    }
    public bool TryRecover()
    {
        if (death == null || death.Dead || (CampaignLevel.Current != null && CampaignLevel.Current.Won)) return false;
        var route = Route();
        if (route == null) return false;
        var original = Capture();
        float progress = ProgressAt(original.body, route);
        Pose best = null;
        float bestDistance = float.PositiveInfinity;
        try
        {
            foreach (var p in safe)
            {
                float distance = (p.body - original.body).sqrMagnitude;
                if (distance >= bestDistance || ProgressAt(p.body, route) > progress - .05f) continue;
                Apply(p);
                if (Clear(false, .025f) && Supported()) { best = p; bestDistance = distance; }
            }
        }
        finally { Apply(original); }
        if (best == null) return false; // Never guess a forward or obstructed destination.
        Apply(best);
        foreach (var rb in bodies) { rb.velocity = Vector2.zero; rb.angularVelocity = 0; }
        RecoveryCount++;
        embeddedTime = 0;
        cooldown = 1f;
        foreach (var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>()) camera.PreviousStateIsValid = false;
        return true;
    }
    void FixedUpdate()
    {
        if (death.Dead || (CampaignLevel.Current != null && CampaignLevel.Current.Won)) return;
        cooldown -= Time.fixedDeltaTime;
        sampleTime -= Time.fixedDeltaTime;
        if (sampleTime <= 0 && body.velocity.sqrMagnitude < .25f && Clear(false, .025f) && Supported())
        {
            sampleTime = .3f;
            if (safe.Count == 0 || (safe[safe.Count - 1].body - body.position).sqrMagnitude > .04f)
            {
                if (safe.Count == 512) safe.RemoveAt(1); // Retain the first verified landing.
                safe.Add(Capture());
            }
        }
        if (cooldown > 0 || Clear(true, penetrationTolerance)) { embeddedTime = 0; return; }
        if (embeddedTime == 0) embeddedOrigin = body.position;
        if ((body.position - embeddedOrigin).sqrMagnitude > .01f) { embeddedTime = 0; return; }
        embeddedTime += Time.fixedDeltaTime;
        if (embeddedTime >= stuckSeconds) { TryRecover(); embeddedTime = 0; cooldown = 1f; }
    }
}
