// Scripts/Building/GridPlacer.cs (conceito)
using UnityEngine;
public sealed class GridPlacer : MonoBehaviour {
    [SerializeField] float cell = 1f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] GameObject ghostPrefab;
    GameObject _ghost;
    void Update() {
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100f, groundMask)) return;
        var pos = Snap(hit.point, cell);
        if (_ghost == null) _ghost = Instantiate(ghostPrefab);
        _ghost.transform.position = pos;
        if (MouseClickedConfirm()) Place(pos);
    }
    static Vector3 Snap(Vector3 p, float c) => new(Mathf.Round(p.x / c) * c, p.y, Mathf.Round(p.z / c) * c);
    void Place(Vector3 pos) { /* valida colisor, dinheiro, instancia final */ }
    bool MouseClickedConfirm() => UnityEngine.Input.GetMouseButtonDown(0);
}
