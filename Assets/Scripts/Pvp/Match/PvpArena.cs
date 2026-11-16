using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Character.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Odyssey.Pvp
{
    public sealed class PvpArena : MonoBehaviour
    {
        public const string SceneName = "PvpArena"; // Legacy verification scene remains loadable.
        [SerializeField] private GameObject character;
        [SerializeField] private CinemachineVirtualCamera followCamera;
        [SerializeField] private Collider2D finish;
        [SerializeField] private Vector2 spawn;
        [SerializeField] private float fallBelow;
        private bool host;
        private PvpWorldState world;
        private readonly HashSet<PvpActor> respawns = new HashSet<PvpActor>();
        internal GameObject Character => character;
        internal Vector2 Spawn => spawn;
        internal float FallBelow
        {
            get
            {
                var water = FindObjectsOfType<FirstLevelWater>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene);
                return water ? water.waterHeight - .2f : fallBelow;
            }
        }
        internal bool Valid => character && followCamera && finish;
        internal PvpWorldState World => world ?? (world = new PvpWorldState(gameObject.scene, host));

        // Called in sceneLoaded, after authored Awake but before single-player Start.
        internal static PvpArena Prepare(Scene scene, bool authoritative)
        {
            var roots = scene.GetRootGameObjects();
            var existing = roots.SelectMany(root => root.GetComponentsInChildren<PvpArena>(true)).FirstOrDefault();
            if (existing) { existing.host = authoritative; return existing; }
            var level = roots.SelectMany(root => root.GetComponentsInChildren<CampaignLevel>(true)).FirstOrDefault();
            var body = roots.SelectMany(root => root.GetComponentsInChildren<Death>(true)).FirstOrDefault();
            var goal = roots.SelectMany(root => root.GetComponentsInChildren<CampaignInteractable>(true))
                .FirstOrDefault(item => item.kind == CampaignInteractable.Kind.Door);
            var win = roots.SelectMany(root => root.GetComponentsInChildren<Winning.WinThresholdDetection>(true)).FirstOrDefault();
            var camera = roots.SelectMany(root => root.GetComponentsInChildren<CinemachineVirtualCamera>(true))
                .OrderByDescending(item => item.Priority).FirstOrDefault();
            if (!level || !body || !camera || (!goal && !win)) return null;
            var adapter = new GameObject("PvP level adapter").AddComponent<PvpArena>();
            adapter.host = authoritative;
            adapter.spawn = level.initialSpawn;
            adapter.followCamera = camera;
            adapter.finish = goal ? goal.GetComponent<Collider2D>() : win.GetComponent<Collider2D>();
            if (!adapter.finish && win) adapter.finish = win.GetComponent<Collider2D>();
            if (adapter.finish) { adapter.finish.gameObject.SetActive(true); adapter.finish.enabled = true; }
            adapter.character = body.transform.parent ? body.transform.parent.gameObject : body.gameObject;
            // sceneLoaded listeners may add enemy visual children. Ensure the same children
            // exist before hiding the reference player, whose height determines their art scale.
            foreach (var enemy in roots.SelectMany(root => root.GetComponentsInChildren<Enemy.EnemyMovement>(true)))
                if (enemy.gameObject.activeInHierarchy && !enemy.GetComponent<Enemy.ForestEnemyVisual>())
                    enemy.gameObject.AddComponent<Enemy.ForestEnemyVisual>();
            var thresholds = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name.Replace(" ", "").ToLowerInvariant().Contains("deaththreshold")).ToArray();
            adapter.fallBelow = thresholds.Length > 0 ? thresholds.Min(item => item.position.y) : level.initialSpawn.y - 30;
            adapter.character.SetActive(false);
            foreach (var root in roots)
            {
                foreach (var clock in root.GetComponentsInChildren<Winning.Clock>(true)) clock.enabled = false;
                foreach (var finishScript in root.GetComponentsInChildren<Winning.WinThresholdDetection>(true)) finishScript.enabled = false;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
#if UNITY_EDITOR
                foreach (var flight in root.GetComponentsInChildren<LevelTestFlight>(true)) flight.enabled = false;
#endif
            }
            return adapter;
        }

        internal void RegisterActor(PvpActor actor)
        {
            if (!host) return;
            foreach (var body in actor.GetComponentsInChildren<Rigidbody2D>(true))
            {
                var relay = body.gameObject.AddComponent<PvpLevelContact>();
                relay.Actor = actor;
            }
            var crush = actor.Body.gameObject.AddComponent<PlayerCrushDetection>();
            crush.enabled = false; // Query the base game's detector without its single-player death flow.
        }
        internal static void QueueRespawn(PvpActor actor)
        {
            var adapter = FindObjectsOfType<PvpArena>().FirstOrDefault(item => item.host);
            if (adapter && actor && actor.ContactActive) adapter.respawns.Add(actor);
        }
        internal bool ShouldRespawn(PvpActor actor)
        {
            bool hazard = respawns.Remove(actor);
            var crush = actor.Body.GetComponent<PlayerCrushDetection>();
            return hazard || actor.Body.position.y < FallBelow || crush && crush.IsCrushed();
        }
        internal bool HasReachedFinish(PvpActor actor) => finish.IsTouching(actor.BodyCollider);
        internal float ArrivalFraction(PvpActor actor, Vector2 previousCenter)
        {
            var body = actor.BodyCollider.bounds;
            var goal = finish.bounds;
            goal.Expand(new Vector3(body.size.x, body.size.y, 0));
            Vector2 delta = (Vector2)body.center - previousCenter;
            float x = EntryFraction(previousCenter.x, delta.x, goal.min.x, goal.max.x);
            float y = EntryFraction(previousCenter.y, delta.y, goal.min.y, goal.max.y);
            return Mathf.Clamp01(Mathf.Max(x, y));
        }
        private static float EntryFraction(float from, float delta, float min, float max) =>
            Mathf.Approximately(delta, 0) ? 0 : ((delta > 0 ? min : max) - from) / delta;
        internal void Follow(PvpActor actor)
        {
            followCamera.Follow = actor.CameraTarget;
            followCamera.PreviousStateIsValid = false;
        }
    }
}
