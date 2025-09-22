using UnityEngine;
using UnityEngine.AI;
using TavernSim.Core;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Waiter : MonoBehaviour, ISelectable
    {
        [SerializeField] private AgentIntentDisplay intentDisplay;
        [SerializeField] private float salary = 25f;

        private NavMeshAgent _agent;

        public string DisplayName => "Garçom";

        public string Status => intentDisplay != null ? intentDisplay.CurrentIntent : string.Empty;

        public float Salary => salary;

        public float MovementSpeed => _agent != null ? _agent.speed : 0f;

        public Transform Transform => transform;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.angularSpeed = 720f;
            _agent.acceleration = 36f;
            _agent.stoppingDistance = 0.05f;

            if (intentDisplay == null)
            {
                TryGetComponent(out intentDisplay);
            }
        }

        public void SetDestination(Vector3 pos)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(pos);
            }
        }

        public bool HasReached(float thresholdSqr)
        {
            if (!_agent.isOnNavMesh)
            {
                return false;
            }

            if (_agent.pathPending)
            {
                return false;
            }

            if (float.IsInfinity(_agent.remainingDistance))
            {
                return false;
            }

            float eps = Mathf.Max(Mathf.Sqrt(thresholdSqr), _agent.stoppingDistance);
            if (_agent.remainingDistance > eps)
            {
                return false;
            }

            return !_agent.hasPath || _agent.velocity.sqrMagnitude <= thresholdSqr;
        }

        public void SetIntent(string text)
        {
            if (intentDisplay == null)
            {
                TryGetComponent(out intentDisplay);
            }

            intentDisplay?.SetIntent(text);
        }
    }
}
