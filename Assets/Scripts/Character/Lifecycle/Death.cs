using System;
using Character.Control;
using UnityEngine;

namespace Character.Lifecycle
{
    public class Death : MonoBehaviour
    {
        public bool Dead { get; private set; }
        public event Action OnCharacterDeath;
        FallDetection fall;
        EnemyContactDetection contact;
        void Awake()
        {
            // Crushing is fatal; the previous automatic rewind must not rescue it.
            var recovery = GetComponent<PlayerUnstuck>();
            if (recovery != null) recovery.enabled = false;
            if (GetComponent<PlayerCrushDetection>() == null) gameObject.AddComponent<PlayerCrushDetection>();
            fall = GetComponent<FallDetection>();
            contact = GetComponent<EnemyContactDetection>();
            if (fall != null) fall.OnFallDetected += Fell;
            if (contact != null) contact.OnEnemyDeathContactDetected += Hit;
        }
        void Fell() { Begin(true, transform.position, Vector2.down); }
        void Hit() { Begin(false, contact.LastContactPoint, contact.LastContactNormal); }
        // Retained for hazards and campaign/checkpoint integrations.
        public void Kill() { Begin(false, transform.position, Vector2.up); }
        void Begin(bool falling, Vector2 point, Vector2 normal)
        {
            if (Dead) return;
            Dead = true;
            if (CampaignLevel.Current != null) CampaignLevel.Current.LoseLife();
            var root = transform.parent != null ? transform.parent : transform;
            foreach (var arms in root.GetComponentsInChildren<ArmsSimulation>()) arms.enabled = false;
            foreach (var target in FindObjectsOfType<TargetMovement>()) target.enabled = false;
            var feedback = GetComponent<DeathFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<DeathFeedback>();
            feedback.Play(root, GetComponent<Rigidbody2D>(), falling, point, normal);
            OnCharacterDeath?.Invoke();
        }
        void OnDestroy()
        {
            if (fall != null) fall.OnFallDetected -= Fell;
            if (contact != null) contact.OnEnemyDeathContactDetected -= Hit;
        }
    }
}
