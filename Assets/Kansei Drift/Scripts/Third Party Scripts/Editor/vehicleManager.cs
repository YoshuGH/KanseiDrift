using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class vehicleManager : EditorWindow{

    [MenuItem("Tools/VehiclaManager")]

    public static void Open(){
        GetWindow<vehicleManager>();
    }


    public GameObject VehicleObject;

    //wheelsObjects
    public GameObject[] wheelReferences ;

    //wheels 
    public Transform wheelsContainer;
    public GameObject[] wheelsObjects;
    public string wheelsContainerString = "wheelsContainer";

    //colliders
    public Transform collidersContainer;
    public Transform[] wheelTransforms;
    public WheelCollider[] wheels;
    public string collidersContainerString = "collidersContainer";

    //wheel position values
    private Vector3 wheelPosition;
    private Quaternion wheelRotation;

    //misc
    Rigidbody rigidbody;
    GameObject bodyColliders = null;
    GameObject camFocus;
    GameObject centerOfMas;
    Camera mainCam;
    cameraController camController;
    string bodyCollidersString = "bodyColliders";

    static Vector3 frontOffset , rearOffset;
    float frontradius , rearRadius , suspensionDistance;
    int wheelsAmount = 4;
    public int camIndex =0;
    bool checkedExistionPosition = false;
    bool simulateCamFlag;

    void OnGUI(){

        SerializedObject Vehicle = new SerializedObject(this);
        EditorGUILayout.PropertyField(Vehicle.FindProperty("VehicleObject"));
        Vehicle.ApplyModifiedProperties();

        SerializedObject rims = new SerializedObject(this);
        EditorGUILayout.PropertyField(rims.FindProperty("wheelReferences"));
        rims.ApplyModifiedProperties();       


        if(VehicleObject != null){ 
            checkVehicle();
            Buttons();
  
        }else{
            checkedExistionPosition = false;
            wheels = null;
            wheelTransforms = null;
            wheelsObjects = null;
            collidersContainer = wheelsContainer = null;

        }

    }

    void checkVehicle(){    
        
        foreach (Transform item in VehicleObject.transform){
            if(item.transform.name == wheelsContainerString){
                wheelsContainer = item.transform;
                if(wheelsObjects == null || wheelsObjects.Length < 2){
                    wheelsObjects = new GameObject[wheelsAmount];
                    for (int i = 0; i < wheelsContainer.transform.childCount; i++){
                        wheelsObjects[i] = wheelsContainer.GetChild(i).gameObject;
                    }
                }
            }
            if(item.transform.name == collidersContainerString){
                collidersContainer = item.transform;

                if(wheels == null || wheels.Length < 2){
                    wheelTransforms = new Transform[wheelsAmount];
                    wheels = new WheelCollider[wheelsAmount];
                    for (int i = 0; i < collidersContainer.transform.childCount; i++){
                        wheelTransforms[i] = collidersContainer.GetChild(i);
                        wheels[i] = wheelTransforms[i].GetComponent<WheelCollider>();
                    }
                }
         
            }
            if(item.transform.name == bodyCollidersString){
                bodyColliders = item.transform.gameObject;
            }
        }


        if(wheels != null &&  wheels.Length == wheelsAmount && wheelsObjects != null && wheelsObjects.Length == wheelsAmount && wheelTransforms != null && wheelTransforms.Length == wheelsAmount){
            checkExistingPosition();
            wheelsProperties();
        }

    }

    void Buttons(){

        if(collidersContainer == null && wheelReferences.Length > 0)
        if(GUILayout.Button("create colliders" ,  GUILayout.Height(40))){
            createColliders();
            
        }

        if(collidersContainer != null){
            if(GUILayout.Button("delete colliders",  GUILayout.Height(40))){
                deleteColliders();
            }
        }

        if(collidersContainer != null && wheelsContainer == null && wheelReferences[0] != null)
        if(GUILayout.Button("create wheels",  GUILayout.Height(40))){
            createWheels();
        }
        
        if(wheelsContainer != null){
        if(GUILayout.Button("delete wheels",  GUILayout.Height(40))){
            deleteWheels();
        }



        if(PrefabUtility.IsPartOfPrefabInstance(VehicleObject))
        if(GUILayout.Button("savePrefab",  GUILayout.Height(40))){
            savePrefab();
        }
        }

        
        if(GUILayout.Button("Simulate Camera position",  GUILayout.Height(40))){
            simulateCamera();
        }

        if(wheelReferences.Length > 2)
            wheelReferences = new GameObject[2];
        if(wheelReferences.Length <1)
            wheelReferences = new GameObject[1];
            
    }

    void savePrefab(){
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(VehicleObject);
        PrefabUtility.SaveAsPrefabAsset(VehicleObject ,path);
        VehicleObject = null;
        bodyColliders = null;
        centerOfMas = null;
        camFocus = null;

    }

    void createWheels(){
        checkedExistionPosition = false;
        
        wheelsContainer = new GameObject().transform;
        wheelsContainer.name = wheelsContainerString;
        wheelsContainer.transform.parent = VehicleObject.transform;
        

        wheelsObjects = new GameObject[wheelsAmount];

        if(wheelReferences.Length > 1)
        for (int i = 0; i < wheelsObjects.Length; i++){
            if(i > 1 )
            wheelsObjects[i] = Instantiate(wheelReferences[1]);
            else
            wheelsObjects[i] = Instantiate(wheelReferences[0]);
            wheelsObjects[i].transform.parent = wheelsContainer.transform ;
            wheelsObjects[i].name = "wheel" + i;
            if(i % 2 != 0)
            wheelsObjects[i].transform.GetChild(0).Rotate(new Vector3(0,180,0));
        }
        else
        for (int i = 0; i < wheelsObjects.Length; i++){
            wheelsObjects[i] = Instantiate(wheelReferences[0]);
            wheelsObjects[i].transform.parent = wheelsContainer.transform ;
            wheelsObjects[i].name = "wheel" + i;
            wheelsObjects[i].transform.position = Vector3.zero;
            if(i % 2 != 0)
            wheelsObjects[i].transform.GetChild(0).Rotate(new Vector3(0,180,0));

        }

        
    }

    void deleteWheels(){
        DestroyImmediate(wheelsContainer.gameObject);
        wheels = null;
        wheelsObjects = null;
        wheelTransforms = null;
    }

    void createColliders(){

        if(VehicleObject.GetComponent<Rigidbody>() == null){
            VehicleObject.AddComponent<Rigidbody>();
        }



        collidersContainer = new GameObject().transform;
        collidersContainer.name = collidersContainerString;
        collidersContainer.transform.parent = VehicleObject.transform;
        
        wheelTransforms = new Transform[wheelsAmount];
        wheels = new WheelCollider[wheelsAmount];

        for (int i = 0; i < wheels.Length; i++){
            wheelTransforms[i] = new GameObject().transform;
            wheelTransforms[i].transform.parent = collidersContainer.transform ;
            wheelTransforms[i].name = "collider" + i;
            wheelTransforms[i].transform.position = Vector3.zero;
            wheels[i] = wheelTransforms[i].gameObject.AddComponent<WheelCollider>();

        }

    }

    void deleteColliders(){
        DestroyImmediate(collidersContainer.gameObject);
        wheels = null;
        wheelTransforms = null;
    }

    void wheelsProperties(){
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("suspension distance" ,  GUILayout.Height(30));
        suspensionDistance = EditorGUILayout.Slider(suspensionDistance,.05f, .4f);
        EditorGUILayout.EndHorizontal();


        //front

        GUILayout.Label("Front" ,  GUILayout.Height(40));

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("front wheels position Z" ,  GUILayout.Height(30));
        frontOffset.z = EditorGUILayout.Slider(frontOffset.z,.8f, 2.2f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("front wheels position Y" ,  GUILayout.Height(30));
        frontOffset.y = EditorGUILayout.Slider(frontOffset.y,0, 1);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("front wheels position x" ,  GUILayout.Height(30));
        frontOffset.x = EditorGUILayout.Slider(frontOffset.x,0.5f, 1);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("front wheels radius" ,  GUILayout.Height(30));
        frontradius = EditorGUILayout.Slider(frontradius,0.2f, 0.5f);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < 2 ; i++){
            wheels[i].radius = frontradius;
            wheels[i].suspensionDistance = suspensionDistance;
            if(i % 2 == 0)
            wheelTransforms[i].transform.localPosition = new Vector3(-frontOffset.x,frontOffset.y,frontOffset.z);
            else
            wheelTransforms[i].transform.localPosition = new Vector3(frontOffset.x,frontOffset.y,frontOffset.z);

        }
        
        GUILayout.Label("Rear" ,  GUILayout.Height(40));

        //rear
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("rear wheels position Z" ,  GUILayout.Height(30));
        rearOffset.z = EditorGUILayout.Slider(rearOffset.z,.8f, 2.2f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("rear wheels position Y" ,  GUILayout.Height(30));
        rearOffset.y = EditorGUILayout.Slider(rearOffset.y,0, 1);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("rear wheels position x" ,  GUILayout.Height(30));
        rearOffset.x = EditorGUILayout.Slider(rearOffset.x,0.5f, 1);
        EditorGUILayout.EndHorizontal();
                
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("front wheels radius" ,  GUILayout.Height(30));
        rearRadius = EditorGUILayout.Slider(rearRadius,0.2f, 0.5f);
        EditorGUILayout.EndHorizontal();


        for (int i = 2; i < wheels.Length ; i++){
            wheels[i].suspensionDistance = suspensionDistance;
            wheels[i].radius = rearRadius;
            if(i % 2 == 0)
            wheelTransforms[i].transform.localPosition = new Vector3(-rearOffset.x,rearOffset.y,-rearOffset.z);
            else
            wheelTransforms[i].transform.localPosition = new Vector3(rearOffset.x,rearOffset.y,-rearOffset.z);
            
        }

        updateWheels();
    }

    void updateWheels(){

		for (int i = 0; i < wheels.Length; i++) {
			wheels[i].GetWorldPose(out wheelPosition, out wheelRotation);
            wheelsObjects[i].transform.rotation = wheelRotation;
            wheelsObjects[i].transform.position = wheelPosition;

        }
    }

    void checkComponents(){

        //check Scripts  ->
        if(VehicleObject.GetComponent<controller>() == null){
            VehicleObject.AddComponent<controller>();
            setUpController();
        }

        //check objecs

        foreach (Transform item in VehicleObject.transform){
            if(item.transform.name == "focus")
            camFocus = item.transform.gameObject;
            if(item.transform.name == "centerOfMas")
            centerOfMas = item.transform.gameObject;
            if(item.transform.name == bodyCollidersString)
            bodyColliders = item.gameObject;
        
        }
        if(bodyColliders == null){
            bodyColliders = new GameObject();
            bodyColliders.transform.name = bodyCollidersString;
            bodyColliders.AddComponent<BoxCollider>();
            bodyColliders.transform.parent = VehicleObject.transform;
        }

        if(camFocus == null){
            camFocus = new GameObject();
            camFocus.transform.name = "focus";
            camFocus.transform.parent = VehicleObject.transform;
        }

        if(centerOfMas == null){
            centerOfMas = new GameObject();
            centerOfMas.transform.name = "centerOfMas";
            centerOfMas.transform.parent = VehicleObject.transform;
        }

        try{
            mainCam = FindObjectOfType<Camera>();
            if(mainCam.GetComponent<cameraController>() != null){
                camController = mainCam.GetComponent<cameraController>();
                if(camController.cameraPos.Length == 0){
                    camController.cameraPos = new Vector2[2];
                }
            }
        }catch{
            mainCam = new GameObject().AddComponent<Camera>();
            mainCam.name = "MainCamera";
            if(mainCam.GetComponent<cameraController>() != null){
                camController = mainCam.GetComponent<cameraController>();
                if(camController.cameraPos.Length == 0){
                    camController.cameraPos = new Vector2[2];
                }
            }        }

        setInstances();

    }

    void checkExistingPosition(){    
        if(checkedExistionPosition)return;  

        checkComponents();

        suspensionDistance = wheels[0].GetComponent<WheelCollider>().suspensionDistance;


        for (int i = 0; i < wheels.Length; i++){
            if(i  < 2){
                frontOffset = new Vector3(Mathf.Abs(wheelTransforms[i].transform.localPosition.x),Mathf.Abs(wheelTransforms[i].transform.localPosition.y),Mathf.Abs(wheelTransforms[i].transform.localPosition.z));
                frontradius =   wheelTransforms[i].GetComponent<WheelCollider>().radius;    
            }
        }

        for (int i = 2; i < wheels.Length; i++){
                rearOffset = new Vector3(Mathf.Abs(wheelTransforms[i].transform.localPosition.x),Mathf.Abs(wheelTransforms[i].transform.localPosition.y),Mathf.Abs(wheelTransforms[i].transform.localPosition.z));
                rearRadius =  wheelTransforms[i].GetComponent<WheelCollider>().radius;
            
        }


        checkedExistionPosition = true;


    }

    void setInstances(){
        wheelsManager manager = VehicleObject.GetComponent<wheelsManager>();
        manager.wheels = wheels;
        manager.wheelObjects = wheelsObjects;
    }

    void setUpController(){
        controller c = VehicleObject.GetComponent<controller>();
        c.gears = new float[7];

        c.maxRPM = 8000;
        c.minRPM = 4000;

        c.DownForceValue = 5;
        c.dragAmount = 0.015f;
        c.EngineSmoothTime = 0.5f;
        c.finalDrive = 2.07f;

        c.gears[0] = 2.53f;
        c.gears[1] = 1.92f;
        c.gears[2] = 1.38f;
        c.gears[3] = 1.08f;
        c.gears[4] = 0.81f;
        c.gears[5] = 0.63f;
        c.gears[6] = 0.48f;



    }

    void simulateCamera(){
        
        if(camIndex >= camController.cameraPos.Length-1 || camIndex < 0) camIndex = 0;
        else camIndex ++;

        mainCam.transform.position = camFocus.transform.position - (camFocus.transform.forward * camController.cameraPos[camIndex].x )+ (camFocus.transform.up * camController.cameraPos[camIndex].y);
        mainCam.transform.LookAt(camFocus.transform);
    }

}
