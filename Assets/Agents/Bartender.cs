using UnityEngine;
using UnityEngine.AI;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Bartender : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.angularSpeed = 720f;
            Agent.acceleration = 24f;
            Agent.stoppingDistance = 0.05f;
        }
    }
}
