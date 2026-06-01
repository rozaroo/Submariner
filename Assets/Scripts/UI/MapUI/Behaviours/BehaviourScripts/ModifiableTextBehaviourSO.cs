using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/ModifiableText")]
public class ModifiableTextBehaviourComponentSo : IconBehaviourSO
{
    [Header("Text Config")]
    public string defaultText = "";
    public float fontSize = 2f;
    public TextAlignmentOptions alignment = TextAlignmentOptions.Center;

    public override void ApplyComponent(GameObject go)
    {
        var comp      = go.AddComponent<ModifiableTextBehaviour>();
        comp.Setup(defaultText,fontSize,alignment);
    }
}