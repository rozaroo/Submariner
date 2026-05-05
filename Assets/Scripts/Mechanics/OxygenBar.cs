using UnityEngine;

// Attach en el RectTransform de la barra de relleno (no el fondo).
// El ancho se achica proporcionalmente al oxígeno restante.
public class OxygenBar : MonoBehaviour
{
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private RectTransform fillRect;

    private float _fullWidth;

    private void Awake()
    {
        _fullWidth = fillRect.sizeDelta.x;
    }

    private void Update()
    {
        float ratio = oxygenSystem.currentOxygen / oxygenSystem.maxOxygen;
        fillRect.sizeDelta = new Vector2(_fullWidth * ratio, fillRect.sizeDelta.y);
    }
}
