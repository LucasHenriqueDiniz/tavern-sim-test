using UnityEngine;
using UnityEngine.AI; // Requires installing the AI Navigation package from the Package Manager.

namespace TavernSim.Agents
{
    /// <summary>
    /// Simple NavMesh driven agent used by the AgentSystem to move customers through the tavern.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private NavMeshAgent _agent;

        public NavMeshAgent Agent => _agent;
        public Animator Animator => animator;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 1.5f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 6f;
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

            var threshold = Mathf.Max(thresholdSqr, 0f);
            var epsilon = Mathf.Max(Mathf.Sqrt(threshold), _agent.stoppingDistance);
            if (_agent.remainingDistance > epsilon)
            {
                return false;
            }

            return !_agent.hasPath || _agent.velocity.sqrMagnitude <= threshold;
        }

        public void SitAt(Transform anchor)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.Warp(anchor.position);
                var previous = _agent.updateRotation;
                _agent.updateRotation = false;
                transform.rotation = anchor.rotation;
                _agent.updateRotation = previous;
            }
            else
            {
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            }
        }

        public void StandUp()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }
    }
}
