using System;
using System.Collections.Generic;
using Character.Control;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odyssey.Pvp
{
    [DefaultExecutionOrder(-50)]
    public sealed class PvpActor : MonoBehaviour
    {
        public const int PoseLength = 9;
        public const float InputRadius = 1.9f;
        private const float MouseUnitsPerPixel = 0.008f;
        private const float ContactSkin = 0.005f;

        private ArmsSimulation arms;
        private Rigidbody2D[] rig;
        private Collider2D[] solidColliders;
        private Vector2[] spawnOffsets;
        private float[] spawnRotations;
        private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(8);
        private SpriteRenderer aimMarker;
        private Vector2 aimPosition;
        private Vector2 sampledInput;
        private Vector2 receivedInput;
        private bool authoritative;
        private bool testBot;
        private bool local;
        private bool frozen;
        private bool inputEnabled;
        private bool cursorCaptured;

        public Rigidbody2D Body { get; private set; }
        public Collider2D BodyCollider { get; private set; }
        public Transform CameraTarget { get; private set; }
        public bool IsTestBot => testBot;
        internal int SpawnRevision { get; private set; }
        internal bool ContactActive => authoritative && !frozen && isActiveAndEnabled && Body.simulated;

        public void Setup(bool authoritative, bool local, Vector2 spawn)
        {
            if (gameObject.activeInHierarchy || rig != null)
                throw new InvalidOperationException("Initialize a PvP actor once, while its clone is inactive.");

            this.authoritative = authoritative;
            this.local = local;
            arms = GetComponentInChildren<ArmsSimulation>(true);
            var cauldron = transform.Find("Cauldron");
            Body = cauldron ? cauldron.GetComponent<Rigidbody2D>() : null;
            BodyCollider = Body ? Body.GetComponent<PolygonCollider2D>() : null;
            var pivot = arms ? arms.GetComponent<Rigidbody2D>() : null;
            var hammer = arms && arms.HammerTip
                ? arms.HammerTip.GetComponentInParent<Rigidbody2D>(true)
                : null;
            if (!Body || !BodyCollider || !pivot || !hammer)
                throw new InvalidOperationException("The PvP actor requires the Character cauldron, paddle and joint rig.");

            var followMarker = cauldron.Find("Mario");
            CameraTarget = followMarker ? followMarker : Body.transform;
            rig = new[] { Body, pivot, hammer };
            spawnOffsets = new Vector2[rig.Length];
            spawnRotations = new float[rig.Length];
            for (int i = 0; i < rig.Length; i++)
            {
                spawnOffsets[i] = rig[i].transform.position - Body.transform.position;
                spawnRotations[i] = rig[i].transform.eulerAngles.z;
            }

            var oldInput = GetComponentInChildren<TargetMovement>(true);
            aimMarker = oldInput ? oldInput.GetComponent<SpriteRenderer>() : null;
            var visual = GetComponentInChildren<PaddleCharacterVisual>(true);
            if (aimMarker && visual && visual.legacy != null)
            {
                for (int i = 0; i < visual.legacy.Length; i++)
                    if (visual.legacy[i] == aimMarker)
                        visual.legacy[i] = null;
            }
            if (aimMarker)
                aimMarker.enabled = false;

            // Campaign behaviours can run Awake even when disabled. Strip them on the inactive clone.
            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour && behaviour != this && behaviour != arms && behaviour != visual)
                    DestroyImmediate(behaviour);
            }
            foreach (var source in GetComponentsInChildren<AudioSource>(true))
                source.enabled = false;

            var colliders = new List<Collider2D>();
            foreach (var shape in GetComponentsInChildren<Collider2D>(true))
            {
                if (!shape.isTrigger && Array.IndexOf(rig, shape.GetComponentInParent<Rigidbody2D>(true)) >= 0)
                    colliders.Add(shape);
            }
            solidColliders = colliders.ToArray();

            Respawn(spawn);
            Freeze(false);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = local && enabled && !testBot;
            if (!inputEnabled)
                ReleaseCursor();
        }

        public Vector2 ReadInput() => sampledInput;

        // A collision-test dummy has no controller or gravity. Contact displacement still
        // translates its whole rig; it stays wherever the last contact left it.
        public void ConfigureTestBot()
        {
            testBot = true;
            SetInputEnabled(false);
            arms.enabled = false;
            foreach (var part in rig)
            {
                part.bodyType = RigidbodyType2D.Kinematic;
                part.velocity = Vector2.zero;
                part.angularVelocity = 0;
            }
        }

        public void HoldInput()
        {
            ApplyInput((Vector2)arms.HammerTip.position - Body.position);
        }

        public void ApplyInput(Vector2 bodyRelativeOffset)
        {
            if (!testBot && ContactActive && IsFinite(bodyRelativeOffset.x) && IsFinite(bodyRelativeOffset.y))
            {
                // Bound components first so even extreme network input cannot overflow sqrMagnitude.
                bodyRelativeOffset.x = Mathf.Clamp(bodyRelativeOffset.x, -InputRadius, InputRadius);
                bodyRelativeOffset.y = Mathf.Clamp(bodyRelativeOffset.y, -InputRadius, InputRadius);
                receivedInput = Vector2.ClampMagnitude(bodyRelativeOffset, InputRadius);
                arms.ReceiveTarget(Body.position + receivedInput);
            }
        }

        private void FixedUpdate()
        {
            if (!testBot && ContactActive)
                arms.ReceiveTarget(Body.position + receivedInput);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (!inputEnabled || frozen || !Application.isFocused || mouse == null)
            {
                ReleaseCursor();
                return;
            }

            if (cursorCaptured && Cursor.lockState == CursorLockMode.Locked)
            {
                aimPosition += mouse.delta.ReadValue() * MouseUnitsPerPixel;
            }
            else
            {
                aimPosition = arms.HammerTip.position;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorCaptured = true;
            }

            Vector2 center = Body.transform.position;
            sampledInput = Vector2.ClampMagnitude(aimPosition - center, InputRadius);
            aimPosition = center + sampledInput;
            if (aimMarker)
            {
                aimMarker.transform.position = aimPosition;
                aimMarker.enabled = true;
            }
        }

        private void OnDisable() => ReleaseCursor();

        private void ReleaseCursor()
        {
            if (!cursorCaptured)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorCaptured = false;
            if (aimMarker)
                aimMarker.enabled = false;
        }

        public float[] CapturePose()
        {
            var pose = new float[PoseLength];
            for (int i = 0; i < rig.Length; i++)
            {
                Vector2 position = rig[i].position;
                pose[i * 3] = position.x;
                pose[i * 3 + 1] = position.y;
                pose[i * 3 + 2] = rig[i].rotation;
            }
            return pose;
        }

        public void ApplyPose(float[] pose) => ApplyPose(pose, pose, 1f);

        public void ApplyPose(float[] previous, float[] next, float progress)
        {
            if (authoritative || previous == null || previous.Length != PoseLength ||
                next == null || next.Length != PoseLength || !IsFinite(progress))
                return;
            for (int i = 0; i < PoseLength; i++)
                if (!IsFinite(previous[i]) || !IsFinite(next[i]))
                    return;

            for (int i = 0; i < rig.Length; i++)
            {
                int index = i * 3;
                var position = new Vector2(
                    Mathf.Lerp(previous[index], next[index], progress),
                    Mathf.Lerp(previous[index + 1], next[index + 1], progress));
                float angle = Mathf.LerpAngle(previous[index + 2], next[index + 2], progress);
                PlaceBody(rig[i], position, angle);
            }
        }

        public void Respawn(Vector2 spawn)
        {
            for (int i = 0; i < rig.Length; i++)
            {
                var part = rig[i];
                PlaceBody(part, spawn + spawnOffsets[i], spawnRotations[i]);
                part.velocity = Vector2.zero;
                part.angularVelocity = 0f;
            }

            receivedInput = Vector2.ClampMagnitude((Vector2)arms.HammerTip.position - spawn, InputRadius);
            sampledInput = receivedInput;
            aimPosition = spawn + sampledInput;
            arms.ReceiveTarget(aimPosition);
            SpawnRevision++;
        }

        public void Freeze(bool freeze)
        {
            frozen = freeze;
            bool simulate = authoritative && !freeze;
            arms.enabled = simulate && !testBot;
            foreach (var part in rig)
                part.simulated = simulate;
            if (freeze)
                ReleaseCursor();
        }

        internal void PushHorizontally(float requestedDistance)
        {
            if (Mathf.Approximately(requestedDistance, 0f))
                return;

            Vector2 direction = requestedDistance > 0f ? Vector2.right : Vector2.left;
            float distance = Mathf.Abs(requestedDistance);
            foreach (var shape in solidColliders)
            {
                if (!shape.enabled || !shape.gameObject.activeInHierarchy)
                    continue;

                var filter = new ContactFilter2D();
                filter.SetLayerMask(Physics2D.GetLayerCollisionMask(shape.gameObject.layer));
                filter.useTriggers = false;
                shape.Cast(direction, filter, castHits, distance + ContactSkin);
                foreach (var hit in castHits)
                {
                    if (!hit.collider || hit.collider.transform.IsChildOf(transform) ||
                        hit.collider.GetComponentInParent<PvpActor>())
                        continue;

                    // Cast reports an inverse sweep normal for an initial overlap. Use the
                    // actual shape separation so touching flat ground allows a sideways push.
                    Vector2 normal = hit.normal;
                    if (hit.distance <= ContactSkin)
                    {
                        var separation = shape.Distance(hit.collider);
                        if (separation.isValid)
                            normal = separation.isOverlapped ? -separation.normal : separation.normal;
                    }
                    if (Vector2.Dot(normal, direction) < -0.01f)
                        distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - ContactSkin));
                }
            }

            if (distance <= 0f)
                return;

            // Translate the whole joint rig and keep its current velocity and motor target.
            Vector2 shift = direction * distance;
            foreach (var part in rig)
                part.position += shift;
        }

        private static void PlaceBody(Rigidbody2D part, Vector2 position, float angle)
        {
            part.position = position;
            part.rotation = angle;
            var renderPosition = new Vector3(position.x, position.y, part.transform.position.z);
            part.transform.SetPositionAndRotation(renderPosition, Quaternion.Euler(0f, 0f, angle));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
