using UnityEngine;
using UnityEngine.AI;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Bartender : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        private void Awake() => Agent = GetComponent<NavMeshAgent>();
    }
}
