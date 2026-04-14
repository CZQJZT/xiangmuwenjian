using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanCounter : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO; 
    [SerializeField] private Transform TableTop;
    
    private KitchenObject kitchenObject;

    public void Interact(Player player)
    {
        Debug.Log("CleanCounter Interact");
        
        if (kitchenObject == null)
        {
            if (player.HasKitchenObject())
            {
                KitchenObject playerKitchenObject = player.GetKitchenObject();
                playerKitchenObject.SetInteract(this);
            }
        }
        else
        {
            if (!player.HasKitchenObject())
            {
                kitchenObject.SetInteract(player);
                CleanKitchenObject();
            }
        }
    }

    public Transform GetKitchenObjectFollowTransform()
    {
        return TableTop;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
    }

    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    public void CleanKitchenObject()
    {
        kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return kitchenObject != null;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}