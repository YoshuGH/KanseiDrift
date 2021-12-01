using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{

    [SerializeField]public float fieldOfView = 68f;

    private GameObject atachedVehicle;
    private int locationIndicator = 1 ;
    private controller controllerRef;
    private Camera camera;

    private float bias ;
    private float smoothTime = .5f;
    private float smoothTimemin = .5f , max = .9f;
    private Vector3 newPos;
    private Transform target;
    public float bandEffect = 300;

    [HideInInspector]public Vector2[] cameraPos;
    
    void Start(){

        cameraPos = new Vector2[3];
        cameraPos[0] = new Vector2(2,0);
        cameraPos[1] = new Vector2(7.5f,1f);
        cameraPos[2] = new Vector2(8.4f,1.6f);

        camera = gameObject.GetComponent<Camera>();
        atachedVehicle = GameObject.FindGameObjectWithTag("Player");
        target = atachedVehicle.transform.Find("focus").gameObject.transform;

        controllerRef = atachedVehicle.GetComponent<controller>();

        camera.usePhysicalProperties = true;
        camera.fieldOfView = fieldOfView;

    }

    void FixedUpdate(){

        updateCam();


    }

    public void cycleCamera(){
        if(locationIndicator >= cameraPos.Length-1 || locationIndicator < 0) locationIndicator = 0;
            else locationIndicator ++;
    }

    public void updateCam(){
        if(Input.GetKeyDown(KeyCode.Tab)){
            cycleCamera();
        }    

		newPos = target.position - (target.forward * cameraPos[locationIndicator].x) + (target.up * cameraPos[locationIndicator].y) ;
		transform.position = newPos * (1 - smoothTime) + transform.position * smoothTime;
        transform.LookAt(target.transform);
        transform.localPosition += transform.forward * controllerRef.KPH / bandEffect ;

        bias = max - controllerRef.KPH  / 400  ;
        smoothTime =( bias >=smoothTimemin)? bias :smoothTimemin;

    }

}
