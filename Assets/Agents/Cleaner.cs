using UnityEngine;
using UnityEngine.AI;
using TavernSim.Core;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Cleaner : MonoBehaviour, ISelectable
    {
        [SerializeField] private AgentIntentDisplay intentDisplay;
        [SerializeField] private float salary = 20f;

        public NavMeshAgent Agent { get; private set; }

        public string DisplayName => "Faxineiro";

        public Transform Transform => transform;

        public float Salary => salary;

        public float MovementSpeed => Agent != null ? Agent.speed : 0f;

        public string Status => intentDisplay != null ? intentDisplay.CurrentIntent : string.Empty;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            if (intentDisplay == null)
            {
                TryGetComponent(out intentDisplay);
            }
        }

        public void SetDestination(Vector3 pos)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.SetDestination(pos);
            }
        }

        public bool HasReached(float thresholdSqr)
        {
            if (Agent == null || !Agent.isOnNavMesh)
            {
                return false;
            }

            if (Agent.pathPending)
            {
                return false;
            }

            if (float.IsInfinity(Agent.remainingDistance))
            {
                return false;
            }

            float eps = Mathf.Max(Mathf.Sqrt(thresholdSqr), Agent.stoppingDistance);
            if (Agent.remainingDistance > eps)
            {
                return false;
            }

            return !Agent.hasPath || Agent.velocity.sqrMagnitude <= thresholdSqr;
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

