using UnityEngine;
using UnityEngine.AI;

namespace TavernSim.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Cook : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        void Awake() => Agent = GetComponent<NavMeshAgent>();
    }
}
