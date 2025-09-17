using UnityEngine;
using UnityEngine.AI; // Requires installing the AI Navigation package from the Package Manager.

namespace TavernSim.Agents
{
    /// <summary>
    /// NavMesh driven waiter controlled entirely by the AgentSystem.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Waiter : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private NavMeshAgent _agent;

        public NavMeshAgent Agent => _agent;
        public Animator Animator => animator;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 2.5f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 8f;
        }

        public void SetDestination(Vector3 position)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(position);
            }
        }

        public bool HasReached(float thresholdSqr)
        {
            if (!_agent.isOnNavMesh)
            {
                return true;
            }

            if (_agent.pathPending)
            {
                return false;
            }

            if (_agent.remainingDistance > Mathf.Sqrt(thresholdSqr))
            {
                return false;
            }

            return !_agent.hasPath || _agent.velocity.sqrMagnitude <= thresholdSqr;
        }
    }
}
