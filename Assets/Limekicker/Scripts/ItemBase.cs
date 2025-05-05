using UnityEngine;

public enum ItemType { Experience, Health, Valuable }

[CreateAssetMenu(menuName = "Limekicker/Item")]
public class ItemBase : BaseScriptableObject
{
    public ItemType itemType;

    [SerializeField] private string itemName;
    [SerializeField] private string description;

    [SerializeField] private MonoBehaviour worldPrefab;
    [SerializeField] private GameObject itemVisuals;

    public string ItemName { get { return itemName; } }
    public string Description { get { return description; } }
    public MonoBehaviour WorldPrefab { get { return worldPrefab; } }
    public GameObject ItemVisuals { get { return itemVisuals; } }
}