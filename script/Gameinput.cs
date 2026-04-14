using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gameinput : MonoBehaviour
{
    private EnemyAction enemyAction;
    private PlayInputActions playInputActions;
    Interact interact;
    public bool wasInteractPerformed = false;

    public void Awake()
  
    {
         playInputActions = new PlayInputActions();
        playInputActions.Player.Enable();
        enemyAction = new EnemyAction();
        enemyAction.Enemy.Enable();
        enemyAction.UI.Enable();
        playInputActions.Player.Interact.performed += Interact_perform;
        Debug.Log("Available Action Maps:");
        foreach (var map in playInputActions.asset.actionMaps)
        {
            Debug.Log(map.name);
        }
        
    }
    public  void Interact_perform(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {

        wasInteractPerformed = true;
        if (obj.performed)
        {
            if (GetComponent<Interact>() != null)
            {
                interact = GetComponent<Interact>();
            }
            Interact interactable = gameObject.GetComponent<Interact>();
            if (interactable != null)
            {
                interactable.InteractObject(); // 调用交互方法

            }
           
        }
    }
   
    // Start is called before the first frame update
    public Vector2 Getmovement()
    {
        Vector2 inputVector = playInputActions.Player.Move.ReadValue<Vector2>();
        playInputActions.Player.Move.ReadValue<Vector2>();
       
        inputVector = inputVector.normalized;
        return inputVector;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
