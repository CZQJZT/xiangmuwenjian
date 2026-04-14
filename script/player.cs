using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent
{
    Interact interact;
    private bool isWalk;
    [SerializeField]float moveSpeed = 5f;
    [SerializeField] private Gameinput gameinput;
    private float roatespeed = 10f;
   public  Transform Hold;
    private KitchenObject kitchenObject;
    [SerializeField] private Interact secondInteract;
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    private KitchenObject ikitchenObjectParent;

    // Start is called before the first frame update
    void Start()
    {
        gameinput = GetComponent<Gameinput>();
        if (gameinput == null)
        {
            gameinput = GetComponent<Gameinput>(); // 尝试从当前游戏对象获取 Gameinput 组件
            if (gameinput == null)
            {
                Debug.LogError("Gameinput component not found on this game object.");
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
       
        Vector2 inputVector = gameinput.Getmovement();
        inputVector = inputVector.normalized;
        Vector3 movement = new Vector3(inputVector.x, 0, inputVector.y);
        transform.position += movement * Time.deltaTime * moveSpeed;
        transform.forward = Vector3.Slerp(transform.forward,movement,Time.deltaTime * roatespeed);
        isWalk = movement != Vector3.zero;
    }
    
    private void OnCollisionStay(Collision other)
    {
        gameinput = GetComponent<Gameinput>();
        
        Interact interact = other.gameObject.GetComponent<Interact>();
        CleanCounter cleanCounter = other.gameObject.GetComponent<CleanCounter>();
        
        if (gameinput.wasInteractPerformed)
        {
            if (interact != null)
            {
                interact.InteractObject(this);
                gameinput.wasInteractPerformed = false;
            }
            else if (cleanCounter != null)
            {
                cleanCounter.Interact(this);
                gameinput.wasInteractPerformed = false;
            }
        }
        
        if (interact != null)
        {
            interact.Interacting();
        }
    }
   
    public bool IsWalk()
    {
        return isWalk;
    }
    public Transform GetKitchenObjectFollowTransform()
    {
        return Hold;
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
}
