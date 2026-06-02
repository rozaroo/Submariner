using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PeriscopeFlash3D : MonoBehaviour
{
    [SerializeField] private PeriscopeCameraAnchorSO periscopeCameraAnchorSo;
    
    private Image _flashImage;
    private Color _currentColor;

    private void Awake()
    {
        if (periscopeCameraAnchorSo == null)
        {
            Log.Warning("[PeriscopeFlash] PeriscopeCameraAnchorSO is null");
            return;
        }
        periscopeCameraAnchorSo.flashComponent = this;
        
        _flashImage = GetComponent<Image>();
        
        if (_flashImage != null)
        {
            _currentColor = _flashImage.color;
            SetOverlayAlpha(0f);
        }
    }
    
    public void SetOverlayColor(Color targetColor, float alpha)
    {
        if (_flashImage == null) return;

        alpha = Mathf.Clamp01(alpha);
        _flashImage.enabled = alpha > 0.001f;
        
        _currentColor = targetColor;
        _currentColor.a = alpha;
        _flashImage.color = _currentColor;
    }
    
    public void SetOverlayAlpha(float alpha)
    {
        if (_flashImage == null) return;

        alpha = Mathf.Clamp01(alpha);
        _flashImage.enabled = alpha > 0.001f;
        
        _currentColor = _flashImage.color;
        _currentColor.a = alpha;
        _flashImage.color = _currentColor;
    }
}