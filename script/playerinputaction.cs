using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playeranimation : MonoBehaviour
{
    [SerializeField] private Player player1;
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("IsWalk", player1.IsWalk());
    }
    void Awake()
    {
       animator = gameObject.GetComponent<Animator>();
        

    }
}
