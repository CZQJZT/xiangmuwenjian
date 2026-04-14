using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    private KitchenObjectSO kitchenObjectSO;
    private IKitchenObjectParent kitchenObjectParent;
  
   public void SetKitchenObject()
    {
      
    }
    public void GetKitchenObject()
    {

    }
    public KitchenObjectSO GetKitchenObjectSO 
    {
        get { return kitchenObjectSO; }
    }
    public void SetInteract(IKitchenObjectParent kitchenObjectParent)
    {
        if (this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.CleanKitchenObject();
        }
        this.kitchenObjectParent = kitchenObjectParent;
        if (kitchenObjectParent.HasKitchenObject())
        {
            Debug.LogError("KitchenObject already has a parent");
        }
        transform.parent = kitchenObjectParent.GetKitchenObjectFollowTransform();
        transform.localPosition = Vector3.zero;
        kitchenObjectParent.SetKitchenObject(this);
    }
    public IKitchenObjectParent GetInteract(){
        return kitchenObjectParent;
    }
    
}