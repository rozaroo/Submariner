using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configures the visual content shown by a TutorialSketchbook while it is open.
/// The referenced Canvas should be world-space and placed as a child of the book.
/// </summary>
[DisallowMultipleComponent]
public class TutorialSketchbookContent : MonoBehaviour
{
    public enum ContentMode
    {
        Text,
        Image,
        TextAndImage
    }

    [Header("Content")]
    [SerializeField] private ContentMode contentMode = ContentMode.Text;
    [SerializeField, TextArea(3, 12)] private string textContent;
    [SerializeField] private Sprite imageContent;

    [Header("World-space view")]
    [Tooltip("Root object of the world-space Canvas. It is shown only while the sketchbook is open.")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TMP_Text textView;
    [SerializeField] private Image imageView;

    private void Awake()
    {
        AttachViewToSketchbook();
        ApplyContent();
        Hide();
    }

    public void Show()
    {
        ApplyContent();

        if (contentRoot != null)
        {
            contentRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (contentRoot != null)
        {
            contentRoot.SetActive(false);
        }
    }

    private void ApplyContent()
    {
        bool showText = contentMode is ContentMode.Text or ContentMode.TextAndImage;
        bool showImage = contentMode is ContentMode.Image or ContentMode.TextAndImage;

        if (textView != null)
        {
            textView.text = textContent;
            textView.gameObject.SetActive(showText);
        }

        if (imageView != null)
        {
            imageView.sprite = imageContent;
            imageView.gameObject.SetActive(showImage && imageContent != null);
        }
    }

    private void AttachViewToSketchbook()
    {
        if (contentRoot == null || contentRoot.transform.IsChildOf(transform))
        {
            return;
        }

        // Keep its current placement on the physical book, then make it follow
        // every movement and rotation applied by TutorialSketchbook.
        contentRoot.transform.SetParent(transform, worldPositionStays: true);
    }
}
