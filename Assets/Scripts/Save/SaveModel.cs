// Scripts/Save/SaveModel.cs
[System.Serializable]
public sealed class SaveModel {
    public int version = 1;
    public float cash;
    public List<PlacedProp> props = new();
}
[System.Serializable]
public sealed class PlacedProp { public string prefabId; public float x,y,z; public float ry; }
