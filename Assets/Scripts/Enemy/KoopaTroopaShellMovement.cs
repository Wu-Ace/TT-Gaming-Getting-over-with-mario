using UnityEngine;

namespace Enemy
{
    public class KoopaTroopaShellMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 2f;

        private EnemyMovement _aliveMovement;

        private void Awake()
        {
            _aliveMovement = transform.parent.gameObject.GetComponent<EnemyMovement>();
        }

        private void Start()
        {
            _aliveMovement.Speed = speed;
            _aliveMovement.enabled = true;
        }
    }
}