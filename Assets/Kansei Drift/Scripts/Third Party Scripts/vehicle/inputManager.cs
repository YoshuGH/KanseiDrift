
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class inputManager : MonoBehaviour{


   
    public float vertical;
    public float horizontal;
    public bool handbrake;
    public bool boosting;


    void Update()
    {
        keyboard();
    }

    public void keyboard () {
        vertical = Input.GetAxis ("Vertical");
        horizontal = Input.GetAxis ("Horizontal");
        handbrake = (Input.GetAxis ("Jump") != 0) ? true : false;
        if (Input.GetKey (KeyCode.LeftShift)) boosting = true;
        else boosting = false;

    }



}
