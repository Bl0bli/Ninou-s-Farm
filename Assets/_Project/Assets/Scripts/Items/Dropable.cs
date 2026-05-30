using UnityEngine;

public class Dropable : MonoBehaviour
{
    [SerializeField] private ItemData _itemDropped;

    protected virtual void DropItem(bool destroyGameObject = false)
    {
        GameObject droppedItem = Instantiate(Resources.Load("Prefabs/ItemPickable") as GameObject, transform.position, Quaternion.identity);
        ItemPickable pickable = droppedItem.GetComponent<ItemPickable>();
        pickable.Init(_itemDropped);
        
    }
}
