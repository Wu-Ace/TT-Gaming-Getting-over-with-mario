using UnityEngine;

namespace Odyssey.Pvp
{
    // Only environmental enemies can request a respawn. Another PvP actor never does.
    public sealed class PvpLevelContact : MonoBehaviour
    {
        internal PvpActor Actor;
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var session = PvpSession.Current;
            if (!session || !session.IsHost || session.Phase != PvpPhase.Racing || !Actor || !Actor.ContactActive) return;
            if (collision.collider.GetComponentInParent<PvpActor>()) return;
            var enemy = collision.collider.GetComponentInParent<Enemy.EnemyDeath>();
            if (!enemy) return;
            bool top = false;
            foreach (var contact in collision.contacts)
                if (Vector2.Angle(contact.normal, Vector2.up) < 20) { top = true; break; }
            if (top) enemy.Defeat();
            else if (GetComponent<Rigidbody2D>() == Actor.Body) PvpArena.QueueRespawn(Actor);
        }
    }
}
