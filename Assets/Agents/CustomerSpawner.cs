using System.Collections.Generic;
using UnityEngine;
using TavernSim.Core.Simulation;
using TavernSim.Simulation.Systems;

namespace TavernSim.Agents
{
    /// <summary>
    /// Deterministic spawner that uses an object pool to recycle customer instances.
    /// </summary>
    public sealed class CustomerSpawner : MonoBehaviour, ISimSystem
    {
        [SerializeField] private int maxCustomers = 8;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private int prewarmCount = 8;
        [SerializeField] private Customer customerPrefab;

        private readonly List<Customer> _pool = new List<Customer>(16);
        private readonly List<Customer> _active = new List<Customer>(16);
        private AgentSystem _agentSystem;
        private float _timer;
        private int _createdCount;

        public void Configure(AgentSystem agentSystem)
        {
            if (_agentSystem != null)
            {
                _agentSystem.CustomerReleased -= Release;
            }

            _agentSystem = agentSystem;
            if (_agentSystem != null)
            {
                _agentSystem.CustomerReleased += Release;
            }
        }

        public void Initialize(Simulation simulation)
        {
            _timer = spawnInterval;
            Prewarm();
        }

        public void Tick(float deltaTime)
        {
            if (_agentSystem == null)
            {
                return;
            }

            _timer += deltaTime;
            CleanupInactive();

            if (_active.Count >= maxCustomers)
            {
                return;
            }

            if (_timer >= spawnInterval)
            {
                _timer = 0f;
                var customer = GetOrCreate();
                _active.Add(customer);
                _agentSystem.SpawnCustomer(customer);
            }
        }

        public void LateTick(float deltaTime)
        {
        }

        public void Dispose()
        {
            if (_agentSystem != null)
            {
                _agentSystem.CustomerReleased -= Release;
            }

            _pool.Clear();
            _active.Clear();
        }

        private Customer GetOrCreate()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }

            var created = CreateInstance();
            _pool.Add(created);
            return created;
        }

        private void CleanupInactive()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].gameObject.activeSelf)
                {
                    _active.RemoveAt(i);
                }
            }
        }

        private void Prewarm()
        {
            var target = Mathf.Clamp(prewarmCount, 0, maxCustomers);
            for (int i = _pool.Count; i < target; i++)
            {
                var customer = CreateInstance();
                customer.gameObject.SetActive(false);
                _pool.Add(customer);
            }
        }

        private Customer CreateInstance()
        {
            Customer instance;
            if (customerPrefab != null)
            {
                instance = Instantiate(customerPrefab, transform);
                instance.name = $"Customer_{_createdCount++}";
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Customer_{_createdCount++}";
                go.transform.SetParent(transform, false);
                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent == null)
                {
                    agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
                }

                agent.radius = 0.3f;
                agent.height = 1.8f;
                instance = go.AddComponent<Customer>();
            }

            instance.transform.SetParent(transform, false);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void Release(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            customer.gameObject.SetActive(false);
            _active.Remove(customer);
            if (!_pool.Contains(customer))
            {
                _pool.Add(customer);
            }
        }
    }
}
