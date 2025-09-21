using UnityEngine;
using UnityEngine.AI;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Cook : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.angularSpeed = 600f;
            Agent.acceleration = 20f;
            Agent.stoppingDistance = 0.05f;
        }
    }
}
