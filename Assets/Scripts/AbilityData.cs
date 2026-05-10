using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Inventory/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    // You can add more fields like ability type, required slot, etc.
}