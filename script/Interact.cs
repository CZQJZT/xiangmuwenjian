using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    [SerializeField] private Transform TableTop;
    [SerializeField] private Interact secondInteract;
    [SerializeField] private bool test;
    [SerializeField] private Player player;
    [SerializeField] private Transform Holds;
    // Start is called before the first frame update
    private KitchenObject kitchenObject;
    void Start()
    {
        test = true;
    }
    private void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.T) && test == true)
        {
            if (kitchenObject != null)
            {
                kitchenObject.SetInteract(secondInteract);
                Debug.Log(secondInteract);
            }
        }

    }


    // Update is called once per frame
    public void SetInteract()
    {

    }
    public void GetInteract()
    {

    }
     public void InteractObject(Player player = null)
    {
        Debug.Log("sir");
        if (kitchenObject == null)
        {
            Transform kitchenObjectTrasform = Instantiate(kitchenObjectSO.prefab, TableTop);
            kitchenObjectTrasform.localPosition = Vector3.zero;
            kitchenObject = kitchenObjectTrasform.GetComponent<KitchenObject>();
            kitchenObject.SetInteract(this);
        }
        else if (kitchenObject != null && player != null)
        {
            if (player.HasKitchenObject())
            {
                KitchenObject playerKitchenObject = player.GetKitchenObject();
                playerKitchenObject.SetInteract(this);
            }
            else
            {
                kitchenObject.SetInteract(player);
                CleanKitchenObject();
            }
        }
    }

    public void Interacting()
    {

    }
    public Transform GetKitchenObjectFollowTransform()
    {
        return TableTop;
    }
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
    }
    public  KitchenObject GetKitchenObject()
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
}
