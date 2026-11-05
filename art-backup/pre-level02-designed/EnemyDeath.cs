using System;
using System.Collections.Generic;
using System.Linq;
using Character.Lifecycle;
using UnityEngine;

namespace Enemy
{
    public class EnemyDeath : MonoBehaviour
    {
        [SerializeField] private GameObject aliveEntity;
        [SerializeField] private GameObject deadEntity;
        [SerializeField] private AudioClip sfx;

        public event Action<GameObject> OnEnemyDeath;

        private EnemyMovement _movement;
        private AudioSource _audioSource;
        private List<EnemyContactDetection> _characterDetectors;

        private readonly List<KoopaTroopaShellContactDetection> _shellDetectors =
            new List<KoopaTroopaShellContactDetection>();

        private List<EnemyDeath> _enemyLifecycles;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _audioSource = GetComponent<AudioSource>();

            _characterDetectors = FindObjectsOfType<EnemyContactDetection>().ToList();
            _characterDetectors.ForEach(detector => detector.OnEnemyTopContactDetected += DieIfSelf);

            _enemyLifecycles = FindObjectsOfType<EnemyDeath>().ToList();
            _enemyLifecycles.ForEach(lifecycle => lifecycle.OnEnemyDeath += SubscribeToKoopaTroopaShells);
        }

        private bool _defeated;

        private void DieIfSelf(GameObject enemy)
        {
            if (enemy != gameObject || _defeated) return;
            // Stomping a Koopa still creates a playable shell, as before.
            if (deadEntity != null && deadEntity.GetComponent<KoopaTroopaShellContactDetection>() != null)
            {
                _defeated = true;
                Unsubscribe();
                aliveEntity.SetActive(false);
                _movement.enabled = false;
                deadEntity.SetActive(true);
                PlayDeathSound();
                OnEnemyDeath?.Invoke(deadEntity);
                return;
            }
            Defeat();
        }

        private void DieFromShell(GameObject enemy)
        {
            if (enemy == gameObject) Defeat();
        }

        // Retain the authored death visual, then launch it above the world rendering layers.
        public void Defeat()
        {
            if (_defeated) return;
            _defeated = true;
            Unsubscribe();
            if (_movement != null) _movement.enabled = false;
            var visual = deadEntity != null ? deadEntity : aliveEntity;
            if (deadEntity != null)
            {
                // A defeated Koopa must not restart shell movement during its death fall.
                foreach (var shellMovement in deadEntity.GetComponentsInChildren<KoopaTroopaShellMovement>(true)) shellMovement.enabled = false;
                aliveEntity.SetActive(false);
                deadEntity.SetActive(true);
            }
            foreach (var animator in aliveEntity.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            // A group also overrides any sorting layers inherited by child renderers.
            var group = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (group == null) group = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            group.sortingLayerID = SortingLayer.layers.OrderBy(layer => layer.value).Last().id;
            group.sortingOrder = 32760;
            foreach (var collider in GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.gravityScale = 2.5f;
                body.drag = 0;
                body.velocity = new Vector2(0, 6f);
                body.WakeUp();
            }
            PlayDeathSound();
            OnEnemyDeath?.Invoke(visual);
            Destroy(gameObject, 4f);
        }

        private void PlayDeathSound()
        {
            if (_audioSource == null || sfx == null) return;
            _audioSource.PlayOneShot(sfx);
        }

        private void OnDestroy() { Unsubscribe(); }

        private void SubscribeToKoopaTroopaShells(GameObject newDeadEntity)
        {
            var shellDetector = newDeadEntity.GetComponent<KoopaTroopaShellContactDetection>();
            if (shellDetector != null)
            {
                shellDetector.OnShellContactDetected += DieFromShell;
                _shellDetectors.Add(shellDetector);
            }
        }

        private void Unsubscribe()
        {
            if (_characterDetectors != null) _characterDetectors.ForEach(detector => { if (detector != null) detector.OnEnemyTopContactDetected -= DieIfSelf; });
            _shellDetectors.ForEach(detector => { if (detector != null) detector.OnShellContactDetected -= DieFromShell; });
            if (_enemyLifecycles != null) _enemyLifecycles.ForEach(lifecycle => { if (lifecycle != null) lifecycle.OnEnemyDeath -= SubscribeToKoopaTroopaShells; });
        }
    }
}