using UnityEngine;
using Unity.AI.Navigation; // Requires installing the AI Navigation package from the Package Manager.

namespace TavernSim.Building
{
    /// <summary>
    /// Helper utilities for creating NavMesh surfaces and obstacles in the graybox scene.
    /// </summary>
    public static class NavMeshSetup
    {
        public static NavMeshSurface EnsureSurface(GameObject ground)
        {
            var surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = ground.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.overrideTileSize = true;
                surface.tileSize = 64;
            }

            return surface;
        }

        public static void MarkObstacle(GameObject go, bool carve = true)
        {
            var obstacle = go.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            }

            obstacle.carving = carve;
            obstacle.size = go.transform.localScale;
        }
    }
}
