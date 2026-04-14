using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactremird : MonoBehaviour
{
    private Player player;
    private MeshRenderer mesh;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnCollisionStay(Collision other)
    {
        player = other.gameObject.GetComponent<Player>();
        mesh = GetComponent<MeshRenderer>();
        if (player!= null)
        {
            mesh.enabled = true;
           
        }
      
    }
    private void OnCollisionExit(Collision other)
    {
        mesh.enabled = false;
    }

}
