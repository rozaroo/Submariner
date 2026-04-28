using UnityEngine;

// El agua sube mientras haya al menos una grieta activa.
// A mayor cantidad de grietas, más rápido sube.
// Al superar el 50% del recorrido total se loguea la derrota.
public class FloodSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CrackManager crackManager;
    [SerializeField] private Transform waterMesh; // mesh sin collider que representa el agua

    [Header("Configuración")]
    [SerializeField] private float baseRiseSpeed = 0.1f;
    [SerializeField] private float startHeight = 0f;
    [SerializeField] private float maxHeight = 10f;

    private float _currentHeight;
    private bool _halfwayLogged;

    private void Start()
    {
        _currentHeight = startHeight;
        SetWaterHeight(_currentHeight);
    }

    private void Update()
    {
        if (crackManager.ActiveCrackCount <= 0) return;

        // Velocidad proporcional a la cantidad de grietas abiertas
        float speed = baseRiseSpeed * crackManager.ActiveCrackCount;
        _currentHeight = Mathf.Clamp(_currentHeight + speed * Time.deltaTime, startHeight, maxHeight);
        SetWaterHeight(_currentHeight);

        float progress = (_currentHeight - startHeight) / (maxHeight - startHeight);
        if (!_halfwayLogged && progress >= 0.7f)
        {
            Debug.Log("[FloodSystem] Submarino se hundió.");
            _halfwayLogged = true;
        }
    }

    private void SetWaterHeight(float y)
    {
        Vector3 pos = waterMesh.position;
        pos.y = y;
        waterMesh.position = pos;
    }
}
