using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Enemy
{
    // New art keeps a sequence-wide canvas and bottom anchor. Physics stays stable per state.
    [DefaultExecutionOrder(-100)]
    public sealed class ForestEnemyVisual : MonoBehaviour
    {
        SpriteRenderer visual;
        Sprite[] walk, death, attack;
        float patrolSpeed;
        public bool IsCharging { get; private set; }
        EnemyMovement movement;
        bool pig, dying;
        float clock;
        Rigidbody2D body;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Attach();
        }
        static void Loaded(Scene scene, LoadSceneMode mode) { Attach(); }
        static void Attach()
        {
            foreach (var enemy in FindObjectsOfType<EnemyMovement>())
                if (!enemy.GetComponent<ForestEnemyVisual>() && enemy.gameObject.activeInHierarchy)
                    enemy.gameObject.AddComponent<ForestEnemyVisual>();
        }
        static readonly System.Collections.Generic.Dictionary<string,Sprite[]> frameCache = new System.Collections.Generic.Dictionary<string,Sprite[]>();
        Sprite[] Frames(string prefix, int count, float ppu)
        {
            string key=prefix+":"+ppu.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            if(frameCache.TryGetValue(key,out var cached) && cached.All(frame=>frame!=null))return cached;
            var frames=Enumerable.Range(1, count).Select(i => {
                var texture = Resources.Load<Texture2D>("ForestEnemies/" + prefix + i);
                return texture == null ? null : Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, 0), ppu);
            }).ToArray();
            frameCache[key]=frames;
            return frames;
        }
        void Awake()
        {
            pig = GetComponentInChildren<KoopaTroopaShellContactDetection>(true) != null;
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<EnemyMovement>();
            patrolSpeed = movement.Speed;
            var collider = GetComponentsInChildren<BoxCollider2D>().FirstOrDefault(c => c.enabled && !c.isTrigger && c.attachedRigidbody == body);
            if (!collider) { enabled = false; return; }
            var old = collider.bounds;
            // Use the player's body as a common world-size reference, never legacy enemy scale.
            var player = FindObjectOfType<Character.Lifecycle.Death>();
            var playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
            float reference = playerCollider != null ? playerCollider.bounds.size.y : 1.2f;
            float height = Mathf.Clamp(reference, .8f, 2f) * (pig ? 2.1f : 1.5f);
            float ppu = (pig ? 300f : 222f) / height;
            walk = Frames(pig ? "pig_move_" : "sheep_move_", pig ? 4 : 2, ppu);
            death = pig ? walk : Frames("sheep_die_", 2, ppu);
            if (pig) attack = Frames("pig_attack_", 2, ppu);
            if (walk.Any(s => s == null)) { enabled = false; return; }
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var original = GetComponentsInChildren<SpriteRenderer>().FirstOrDefault();
            var child = new GameObject("Forest enemy visual");
            child.transform.SetParent(transform, false);
            child.transform.position = new Vector3(old.center.x, old.min.y, transform.position.z);
            child.transform.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1);
            visual = child.AddComponent<SpriteRenderer>();
            if (original) { visual.sortingLayerID = original.sortingLayerID; visual.sortingOrder = original.sortingOrder; }
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) if (r != visual) r.enabled = false;
            // Torso only: legs/horns and outstretched arms are excluded.
            float width = height * (pig ? .64f : .95f);
            var localSize = collider.transform.InverseTransformVector(new Vector3(width, height * .78f, 0));
            collider.size = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
            // Round only a tiny part of the lower corners to avoid catching individual tile edges.
            collider.edgeRadius = Mathf.Min(.025f / Mathf.Abs(collider.transform.lossyScale.x),
                Mathf.Min(collider.size.x, collider.size.y) * .08f);
            collider.offset = collider.transform.InverseTransformPoint(new Vector3(old.center.x, old.min.y + height * .39f, transform.position.z));
            visual.sprite = walk[0];
        }
        public void BeginDeath() { dying = true; clock = 0; movement.Speed = patrolSpeed; }
        public bool ReceiveAttack()
        {
            if (!pig || dying || !enabled) return false;
            // First hit starts a persistent shell-like charge. Further hits do nothing.
            if (IsCharging) return true;
            GameplayAudio.Play("enemy_hit", .65f);
            GameplayAudio.Play("pig_charge", .75f);
            IsCharging = true;
            clock = 0;
            movement.Speed = Mathf.Max(.8f, Mathf.Abs(patrolSpeed)) * 2.5f;
            return true;
        }
        public void HitEnemiesAhead(Collider2D ownCollider)
        {
            if (!IsCharging || dying || !enabled || !ownCollider || !body) return;
            var bounds = ownCollider.bounds;
            float reach = Mathf.Abs(movement.Speed) * Time.fixedDeltaTime + .04f;
            foreach (var other in Physics2D.OverlapBoxAll(bounds.center,
                new Vector2(bounds.size.x + reach * 2f, bounds.size.y * .9f), 0f))
            {
                if (!other || other.isTrigger || other.attachedRigidbody == body) continue;
                var target = other.GetComponentInParent<EnemyDeath>();
                if (target && target.gameObject != gameObject && target.isActiveAndEnabled)
                    target.Defeat(true);
            }
        }
        void LateUpdate()
        {
            if (!visual) return;
            if (dying || (body && Mathf.Abs(body.velocity.x) > .03f)) clock += Time.deltaTime;
            else clock = 0;
            var frames = dying ? death : IsCharging && attack != null && attack.All(s => s != null) ? attack : walk;
            int index = dying ? Mathf.Min((int)(clock * 6), frames.Length - 1) : (int)(clock * (pig ? 8 : 6)) % frames.Length;
            visual.sprite = frames[index];
            if (!dying && body && Mathf.Abs(body.velocity.x) > .01f) visual.flipX = body.velocity.x < 0;
        }
        // Cached sprites are shared by enemies and must survive individual enemy cleanup.
    }
}
