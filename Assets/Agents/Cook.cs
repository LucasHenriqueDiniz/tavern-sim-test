using UnityEngine;
using UnityEngine.AI;
using TavernSim.Core;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Cook : MonoBehaviour, ISelectable
    {
        [SerializeField] private AgentIntentDisplay intentDisplay;
        [SerializeField] private float salary = 30f;

        public NavMeshAgent Agent { get; private set; }

        public string DisplayName => "Cozinheiro";

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
