using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationLampFeedback : MonoBehaviour
{
    [Header("Motor")]
    [SerializeField] private EngineSystem engineSystem;

    [Header("Lampara")]
    [SerializeField] private Renderer lampRenderer;
    [SerializeField] private Light lampLight;
    //[SerializeField] private Image lampImage;

    //Texto 3D
    private Vector3 textLocalPosition = new Vector3(0f, 0.15f, 0f);
    private Vector3 textLocalRotation = new Vector3(0f, 0f, 0f);
    private float textFontSize = 0.5f;
    private Color textColor = Color.white;
    private TMP_Text generatedText;

    [Header("Configuracion")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float blinkInterval = 0.25f;
    [SerializeField] private float warningDuration = 3f;
    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;
    private bool debugModeActive;
    private bool debugEngineRunning;

    private Coroutine warningCoroutine;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        CreateStatusText();
        RefreshIndicator();
    }

    private void Update()
    {
        if (enableDebugKeys && UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                debugModeActive = true;
                debugEngineRunning = false;

                if (warningCoroutine != null)
                {
                    StopCoroutine(warningCoroutine);
                    warningCoroutine = null;
                }
                SetLamp(Color.black);
                SetText("MOTOR APAGADO");
                Debug.Log("[DEBUG] Motor apagado.");
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                debugModeActive = true;
                debugEngineRunning = true;

                if (warningCoroutine != null)
                {
                    StopCoroutine(warningCoroutine);
                    warningCoroutine = null;
                }
                SetLamp(activeColor);
                SetText("MOTOR ACTIVO");

                Debug.Log("[DEBUG] Motor activo.");
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                debugModeActive = true;
                debugEngineRunning = false;

                ShowMotorWarning();

                Debug.Log("[DEBUG] Intento de movimiento con motor apagado.");
            }
        }

        RefreshIndicator();
    }
    public void ShowMotorWarning()
    {
        bool isEngineRunning = debugModeActive ? debugEngineRunning : engineSystem != null && engineSystem.IsRunning();
        if (isEngineRunning)
        {
            RefreshIndicator();
            return;
        }
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(BlinkWarning());
    }

    private IEnumerator BlinkWarning()
    {
        float elapsed = 0f;
        bool visible = true;

        SetText("MOTOR APAGADO");

        while (elapsed < warningDuration)
        {
            bool isEngineRunning = debugModeActive ? debugEngineRunning : engineSystem != null && engineSystem.IsRunning();
            if (isEngineRunning)
            {
                warningCoroutine = null;
                RefreshIndicator();
                yield break;
            }
            SetLamp(visible ? warningColor : Color.black);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
            visible = !visible;
        }
        warningCoroutine = null;
        RefreshIndicator();
    }


    private void RefreshIndicator()
    {
        bool isEngineRunning;
        if (debugModeActive) isEngineRunning = debugEngineRunning;
        else
        {
            if (engineSystem == null) return;
            isEngineRunning = engineSystem.IsRunning();
        }

        if (isEngineRunning)
        {
            if (warningCoroutine != null)
            {
                StopCoroutine(warningCoroutine);
                warningCoroutine = null;
            }
            SetLamp(activeColor);
            SetText("MOTOR ACTIVO");
        }
        else if (warningCoroutine == null)
        {
            SetLamp(Color.black);
            SetText("MOTOR APAGADO");
        }
    }
    private void SetLamp(Color color)
    {
        if (lampRenderer != null)
        {
            lampRenderer.GetPropertyBlock(propertyBlock);
            Material material = lampRenderer.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty(BaseColorID)) propertyBlock.SetColor(BaseColorID, color);
                if (material.HasProperty(ColorID)) propertyBlock.SetColor(ColorID, color);
                if (material.HasProperty(EmissionColorID)) propertyBlock.SetColor(EmissionColorID,color * 2f);
            }
            lampRenderer.SetPropertyBlock(propertyBlock);
        }
        if (lampLight != null)
        {
            lampLight.color = color;
            lampLight.enabled = color != Color.black;
        }
        //if (lampImage != null) lampImage.color = color;
    }

    private void SetText(string message)
    {
        if (generatedText != null) generatedText.text = message;
    }

    private void OnDisable()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
    }

    private void CreateStatusText()
    {
        if (generatedText != null)
            return;

        GameObject textObject = new GameObject("MotorStatusText");

        // El texto será hijo de la lámpara.
        textObject.transform.SetParent(transform, false);

        textObject.transform.localPosition = textLocalPosition;
        textObject.transform.localRotation = Quaternion.Euler(textLocalRotation);
        textObject.transform.localScale = Vector3.one;
        generatedText = textObject.AddComponent<TextMeshPro>();
        generatedText.text = "MOTOR APAGADO";
        generatedText.fontSize = textFontSize;
        generatedText.color = textColor;
        generatedText.alignment = TextAlignmentOptions.Center;
        generatedText.enableWordWrapping = false;
    }

}
