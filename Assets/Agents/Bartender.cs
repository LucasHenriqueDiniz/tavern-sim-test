using UnityEngine;
using UnityEngine.AI;
using TavernSim.Core;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Bartender : MonoBehaviour, ISelectable
    {
        [SerializeField] private AgentIntentDisplay intentDisplay;
        [SerializeField] private float salary = 28f;

        public NavMeshAgent Agent { get; private set; }

        public string DisplayName => "Bartender";

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
    }
}
