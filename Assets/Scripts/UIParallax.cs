using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIParallax : MonoBehaviour
{
    [Header("Configuración de Parallax")]
    [Tooltip("Fuerza del movimiento en X e Y. Valores más altos generan mayor movimiento.")]
    public Vector2 fuerzaParallax = new Vector2(15f, 15f);

    [Tooltip("Velocidad de suavizado del movimiento.")]
    public float velocidadSuavizado = 10f;

    [Tooltip("Invertir la dirección del movimiento (para que se mueva en dirección contraria al ratón).")]
    public bool invertirDireccion = true;

    private RectTransform rectTransform;
    private Vector2 posicionInicial;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Guardamos la posición original del elemento en el Canvas
        posicionInicial = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // 1. Obtenemos la posición del ratón en la pantalla
        Vector2 posicionRaton = Input.mousePosition;

        // 2. Normalizamos la posición del ratón para que el centro de la pantalla sea (0,0)
        // Rango de -1 a 1 en ambos ejes
        float posXNormalizada = (posicionRaton.x / Screen.width) * 2f - 1f;
        float posYNormalizada = (posicionRaton.y / Screen.height) * 2f - 1f;

        // 3. Calculamos el desplazamiento aplicando la fuerza personalizada
        Vector2 desplazamiento = new Vector2(posXNormalizada * fuerzaParallax.x, posYNormalizada * fuerzaParallax.y);

        if (invertirDireccion)
        {
            desplazamiento = -desplazamiento;
        }

        // 4. Calculamos la posición final objetivo
        Vector2 posicionObjetivo = posicionInicial + desplazamiento;

        // 5. Interpolamos suavemente (Lerp) desde la posición actual a la posición objetivo
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, posicionObjetivo, Time.deltaTime * velocidadSuavizado);
    }
}