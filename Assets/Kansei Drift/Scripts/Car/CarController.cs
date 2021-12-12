using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    #region Private Variables

    private float horizontalInput;
    private float verticalInput;
    private float currentBrakeForce;
    private float currentSteerAngle;
    private float wheelBase = 2.55f;
    private float wheelTrack = 1.5f;
    private float kmph;
    private int currentGear = 0;
    private float engineRPM;
    private float gForce = 0;
    private float currentVelocity;
    private float lastFrameVelocity;
    private float motorTorque;
    private float ackermanRadius;
    private int colIterator, checkpoints;
    private bool disableControls = false;
    private bool isBraking, isHandbraking, updateCenterOfMass = false, reverse = false;

    private Rigidbody rb = new Rigidbody();
    private PlayerInput playerInput;
    private InputAction moveAction, brakeAction, handbrakeAction, upShift, downShift;
    private WheelFrictionCurve[] sidewaysFriction;

    private enum DriveSetup { FWD, RWD, AWD }
    private enum GearBox { automatic, manual }

    #endregion

    #region Inpector Variables

    [Header("Car Setup"),Space]
    [SerializeField] private DriveSetup driveSetup;
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField, Tooltip("The Value is express in N/m")]
    private float brakeForce;
    [SerializeField] GearBox gearboxType;
    [SerializeField, Tooltip("This only applys to automatic gearbox")] 
    private float maxRPM = 5500, minRPM = 2500;
    [SerializeField] private float[] gearsRatio;
    [SerializeField] private float finalDriveAxleRatio = 3.73f;
    [SerializeField] private float reverseGearRatio = 2.76f;
    [SerializeField] private float maxSteeringAngle;
    [SerializeField] private float sidewaysFrictionStiffness = 0.775f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float frontAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float rearAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("Is for relocate the center of mass relative to the original")] 
    private Vector3 centerOfMassOffset;
    [SerializeField] private float downForceValue = 50f;
    [Header("Miscellaneous")]
    [SerializeField, Space, Space] private float steerAngleSmoothTime;

    [Header("Wheel Colliders")]
    [SerializeField, Space, Space, Tooltip("The order of the wheel colliders must be equal to the wheel transforms")] 
    private WheelCollider[] wheelColliders;

    [Header("Wheel Transform Models")]
    [SerializeField, Space, Space, Tooltip("The order of the wheel transforms must be equal to the wheel colliders")] 
    private Transform[] wheelTransforms;

    #endregion

    #region Accesors
    public float KMPH
    {
        get { return kmph; }
    }

    public bool DisableControls
    {
        set { disableControls = value; }
    }

    public float EngineRPM
    {
        get { return engineRPM; }
    }

    public float MaxRPM
    {
        get { return maxRPM; }
    }

    public int Gear
    {
        get { return currentGear; }
    }

    public float GForce
    {
        get { return gForce; }
    }

    public bool IsReverse
    {
        get { return reverse; }
    }

    public float VerticalInput
    {
        get { return verticalInput; }
    }

    public int Checkpoints
    {
        get { return checkpoints; }
    }

    public WheelCollider[] WheelColliders
    {
        get { return wheelColliders; }
    }

    public Transform[] WheelTransforms
    {
        get { return wheelTransforms; }
    }

    #endregion

    private void Awake()
    {
        playerInput = transform.GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        brakeAction = playerInput.actions["Brake"];
        handbrakeAction = playerInput.actions["Handbrake"];
        upShift = playerInput.actions["UpShift"];
        downShift = playerInput.actions["DownShift"];
        sidewaysFriction = new WheelFrictionCurve[wheelColliders.Length];
    }

    private void Start()
    {
        Vector3 pos; Quaternion quat;

        //Get the rigidbody of the player y change de center of mass depending of the offset setted before
        //Also allow the Gizmos of the center of mass to see in game
        rb = transform.GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;
        updateCenterOfMass = true;

        //Obtain the position of need wheels to calculate wheelbase and wheeltrack
        Vector3 FRWheel, RRWheel, FLWheel;
        wheelColliders[0].GetWorldPose(out pos, out quat);
        FRWheel = pos;
        wheelColliders[2].GetWorldPose(out pos, out quat);
        RRWheel = pos;
        wheelColliders[1].GetWorldPose(out pos, out quat);
        FLWheel = pos;

        //Calculate wheelbase y wheeltrack
        wheelBase = Vector3.Distance(FRWheel, RRWheel);
        wheelTrack = Vector3.Distance(FRWheel, FLWheel);

        //Obtain the wheelFrictionCurve
        for(int i = 0; i<wheelColliders.Length; i++)
        {
            sidewaysFriction[i] = wheelColliders[0].sidewaysFriction;
        }

        //Calculate the ackerman radius given a max angle
        float rad = maxSteeringAngle * Mathf.PI / 180;
        ackermanRadius = (wheelBase / (Mathf.Tan(rad))) + (wheelTrack / 2);

        //Add the sidewaysFrictionStiffness to the wheels given the current drive setup
        switch (driveSetup)
        {
            case DriveSetup.FWD:
                for(int i = 0; i < wheelColliders.Length; i++)
                {
                    if(i < 2)
                    {
                        sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                        wheelColliders[i].sidewaysFriction = sidewaysFriction[i];
                    }
                }
                break;
            case (DriveSetup)1:
                for (int i = 2; i < wheelColliders.Length; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                    wheelColliders[i].sidewaysFriction = sidewaysFriction[i];
                }
                break;
            case DriveSetup.AWD:
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                    wheelColliders[i].sidewaysFriction = sidewaysFriction[i];
                }
                break;
            default:
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    if (i < 2)
                    {
                        sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                        wheelColliders[i].sidewaysFriction = sidewaysFriction[i];
                    }
                }
                break;
        }
    }

    void OnDrawGizmos()
    {
        Rigidbody _rb = this.GetComponent<Rigidbody>();
        Vector3 centerOfMassPos;
        Vector3 pos; Quaternion quat;

        if (!updateCenterOfMass)
        {
            centerOfMassPos = new Vector3(0.0f, 0.9f, 0.0f) + centerOfMassOffset;
        }
        else
        {
            centerOfMassPos = _rb.worldCenterOfMass;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(centerOfMassPos, 0.1f);

        Vector3 FRWheel, RRWheel, FLWheel;
        wheelColliders[1].GetWorldPose(out pos, out quat);
        FRWheel = pos;
        wheelColliders[3].GetWorldPose(out pos, out quat);
        RRWheel = pos;
        wheelColliders[0].GetWorldPose(out pos, out quat);
        FLWheel = pos;

        //Wheelbase
        Gizmos.color = Color.red;
        Gizmos.DrawLine(FRWheel, RRWheel);

        //Wheel track
        Gizmos.color = Color.red;
        Gizmos.DrawLine(FRWheel, FLWheel);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("VictoryColliders"))
        {
            colIterator++;
            if(colIterator == 5)
            {
                checkpoints++;
            }
        }
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {

        Gearbox();

        //Calculate Speed in KMPH
        kmph = rb.velocity.magnitude * 3.6f;

        //Calculates de G Force
        currentVelocity = rb.velocity.magnitude;
        gForce = (currentVelocity - lastFrameVelocity) / (Time.deltaTime * Physics.gravity.magnitude);
        lastFrameVelocity = currentVelocity;


        //Add the downforce to the car
        rb.AddForce(-transform.up * downForceValue * rb.velocity.magnitude);

        CalculateEnginePower();
        HandleMotor();
        HandleSteering();
        UpdateWheels();

        //Calculate the Antirollforce for the antiroll bars in both axles (Front and Rear)
        AntirollBarCalculation(wheelColliders[0], wheelColliders[1], frontAntirollStiffness);
        AntirollBarCalculation(wheelColliders[2], wheelColliders[3], rearAntirollStiffness);
    }

    private void GetInput()
    {
        if(!disableControls)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            horizontalInput = move.x;
            verticalInput = move.y;

            isBraking = brakeAction.ReadValue<float>() == 1 ? true : false;
            isHandbraking = handbrakeAction.ReadValue<float>() == 1 ? true : false;

            if (upShift.triggered)
            {
                currentGear++;
            }

            if (downShift.triggered)
            {
                currentGear--;
            }
        }
    }

    private void HandleMotor()
    {
        float motorForceEachWheel = 0;
        switch(driveSetup)
        {
            case 0:
                motorForceEachWheel = motorTorque / 2;
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    if (i < 2)
                    {
                        wheelColliders[i].motorTorque = motorForceEachWheel;
                    }
                }
                break;
            case DriveSetup.RWD:
                motorForceEachWheel = motorTorque / 2;
                for (int i = 2; i < wheelColliders.Length; i++)
                {
                    wheelColliders[i].motorTorque = motorForceEachWheel;
                }
                break;
            case (DriveSetup)2:
                motorForceEachWheel = motorTorque / 4;
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    wheelColliders[i].motorTorque = motorForceEachWheel;
                }
                break;
            default:
                motorForceEachWheel = motorTorque / 2;
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    if (i < 2)
                    {
                        wheelColliders[i].motorTorque = motorForceEachWheel;
                    }
                }
                break;
        }
        currentBrakeForce = isBraking || isHandbraking ? brakeForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        
        if (isHandbraking)
        {
            for (int i = 2; i < wheelColliders.Length; i++)
            {
                wheelColliders[i].brakeTorque = currentBrakeForce * 3;
            }
        }
        else
        {
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                wheelColliders[i].brakeTorque = currentBrakeForce;
            }
        }
    }

    private void HandleSteering()
    {
        float ackermanAngle = 0, clampAckermanangle = 0, velocity = 0;
        //Ackerman Steering formula
        //steer angle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (wheelRadius + (wheelTrack / 2))) * InputDirection
        if (horizontalInput > 0)
        {
            //Left wheel
            currentSteerAngle = wheelColliders[0].steerAngle;
            ackermanAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (ackermanRadius + (wheelTrack / 2))) * horizontalInput;
            clampAckermanangle = Mathf.Clamp(ackermanAngle, -maxSteeringAngle, maxSteeringAngle);
            wheelColliders[0].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, clampAckermanangle, ref velocity, steerAngleSmoothTime);
            //print("Left: " + wheelColliders[0].steerAngle);

            //Right Wheel
            currentSteerAngle = wheelColliders[1].steerAngle;
            ackermanAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (ackermanRadius - (wheelTrack / 2))) * horizontalInput;
            clampAckermanangle = Mathf.Clamp(ackermanAngle, -maxSteeringAngle, maxSteeringAngle);
            wheelColliders[1].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, clampAckermanangle, ref velocity, steerAngleSmoothTime);
            //print("Right: " + wheelColliders[1].steerAngle);
        }
        else if (horizontalInput < 0)
        {
            //Left wheel
            currentSteerAngle = wheelColliders[0].steerAngle;
            ackermanAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (ackermanRadius - (wheelTrack / 2))) * horizontalInput;
            clampAckermanangle = Mathf.Clamp(ackermanAngle, -maxSteeringAngle, maxSteeringAngle);
            wheelColliders[0].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, clampAckermanangle, ref velocity, steerAngleSmoothTime);
            //print("Left: " + wheelColliders[0].steerAngle);

            //Right Wheel
            currentSteerAngle = wheelColliders[1].steerAngle;
            ackermanAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (ackermanRadius + (wheelTrack / 2))) * horizontalInput;
            clampAckermanangle = Mathf.Clamp(ackermanAngle, -maxSteeringAngle, maxSteeringAngle);
            wheelColliders[1].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, clampAckermanangle, ref velocity, steerAngleSmoothTime);
            //print("Right: " + wheelColliders[1].steerAngle);
        }
        else
        {
            currentSteerAngle = wheelColliders[0].steerAngle;
            wheelColliders[0].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, 0, ref velocity, steerAngleSmoothTime);
            currentSteerAngle = wheelColliders[1].steerAngle;
            wheelColliders[1].steerAngle = Mathf.SmoothDampAngle(currentSteerAngle, 0, ref velocity, steerAngleSmoothTime);
            currentSteerAngle = 0;
        }
    }

    private void UpdateWheels()
    {
        for(int i = 0; i < wheelColliders.Length; i++)
        {
            UpdateSingleWheel(wheelColliders[i], wheelTransforms[i]);
        }
    }

    private void CalculateEnginePower()
    {
        float wheelRPM = GetWheelRPM();

        motorTorque = torqueCurve.Evaluate(engineRPM) * (gearsRatio[currentGear]) * verticalInput;
        float velocity = 0;
        engineRPM = Mathf.SmoothDamp(engineRPM, 800 + (Mathf.Abs(wheelRPM)) * finalDriveAxleRatio * (gearsRatio[currentGear]),ref velocity, 0.1f);
    }

    private void Gearbox()
    {
        switch(gearboxType)
        {
            case GearBox.automatic:
                if (!isGrounded()) return;
                if (engineRPM > maxRPM && currentGear < gearsRatio.Length - 1 && !reverse)
                {
                    currentGear++;
                }
                if (engineRPM < minRPM && currentGear > 0)
                {
                    currentGear--;
                }
                break;
            case GearBox.manual:
                if (reverse)
                {
                    reverse = false;
                }
                else if (currentGear > gearsRatio.Length - 1)
                {
                    currentGear--;
                }

                if (currentGear < 0)
                {
                    reverse = true;
                    currentGear++;
                }

                break;
        }
    }

    private float GetWheelRPM()
    {
        float sum = 0;

        for(int i = 0; i < wheelColliders.Length; i++)
        {
            sum += wheelColliders[i].rpm;
        }

        if (sum < 0 && !reverse)
        {
            reverse = true;
            currentGear = 0;
        }
        else if (sum > 0 && reverse)
        {
            reverse = false;
        }
        return sum / 4;
    }

    private void UpdateSingleWheel(WheelCollider _wheelCollider, Transform _wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        _wheelCollider.GetWorldPose(out pos, out rot);
        _wheelTransform.position = pos;
        _wheelTransform.rotation = rot;
    }

    private void AntirollBarCalculation(WheelCollider _leftWheel, WheelCollider _rightWheel, float _stiffness)
    {
        WheelHit hit;
        //Suspension travel depends on the compression of the spring can be between 0 (Fully Compressed) and 1 (Fully Extended)
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = _leftWheel.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-_leftWheel.transform.InverseTransformPoint(hit.point).y - _leftWheel.radius) / _leftWheel.suspensionDistance;

        bool groundedR = _rightWheel.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-_rightWheel.transform.InverseTransformPoint(hit.point).y - _rightWheel.radius) / _rightWheel.suspensionDistance;

        float antiRollForce = (travelL - travelR) * _stiffness;

        if (groundedL)
            rb.AddForceAtPosition(_leftWheel.transform.up * -antiRollForce, _leftWheel.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(_rightWheel.transform.up * antiRollForce, _rightWheel.transform.position);
    }

    public bool isGrounded()
    {
        if(wheelColliders[0].isGrounded && wheelColliders[1].isGrounded && wheelColliders[2].isGrounded && wheelColliders[3].isGrounded)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
