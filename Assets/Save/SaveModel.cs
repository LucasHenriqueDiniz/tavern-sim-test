using System;
using UnityEngine;

namespace TavernSim.Save
{
    /// <summary>
    /// Serializable save model containing the deterministic state required to resume a session.
    /// </summary>
    [Serializable]
    public sealed class SaveModel
    {
        public int version = 1;
        public float cash = 500f;
        public PlacedProp[] placedProps = Array.Empty<PlacedProp>();
        public int rngSeed = 12345;
    }

    [Serializable]
    public sealed class PlacedProp
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
    }
}
