using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking, isHandbraking, updateCenterOfMass = false, reverse = false;
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
    private int colIterator, checkpoints;
    private bool disableControls = false;

    private Rigidbody rb = new Rigidbody();
    private PlayerInput playerInput;
    private InputAction moveAction, brakeAction, handbrakeAction, upShift, downShift;
    private WheelFrictionCurve[] sidewaysFriction = new WheelFrictionCurve[4];

    private enum DriveSetup { FWD, RWD, AWD }
    private enum GearBox { automatic, manual }

    [Header("Car Setup"),Space]
    [SerializeField] private DriveSetup driveSetup;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float motorTorque;
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] GearBox gearbox;
    [SerializeField] private float maxRPM = 5500, minRPM = 2500;
    [SerializeField] private float[] gearsRatio;
    [SerializeField] private float finalDriveAxleRatio = 3.74f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float brakeForce;
    [SerializeField] private float maxSteeringAngle;
    [SerializeField] private float sidewaysFrictionStiffness = 0.775f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float frontAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float rearAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("Is for relocate the center of mass relative to the original")] 
    private Vector3 centerOfMassOffset;
    [SerializeField] private float downForceValue = 50f;

    [Header("Wheel Colliders")]
    [SerializeField, Space, Space] public WheelCollider frontLeftCollider;
    [SerializeField] public WheelCollider frontRightCollider;
    [SerializeField] public WheelCollider rearLeftCollider;
    [SerializeField] public WheelCollider rearRightCollider;

    [Header("Wheel Transform Models")]
    [SerializeField, Space, Space] public Transform frontLeftTransform;
    [SerializeField] public Transform frontRightTransform;
    [SerializeField] public Transform rearLeftTransform;
    [SerializeField] public Transform rearRightTransform;

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

    private void Awake()
    {
        playerInput = transform.GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        brakeAction = playerInput.actions["Brake"];
        handbrakeAction = playerInput.actions["Handbrake"];
        upShift = playerInput.actions["UpShift"];
        downShift = playerInput.actions["DownShift"];
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
        frontRightCollider.GetWorldPose(out pos, out quat);
        FRWheel = pos;
        rearRightCollider.GetWorldPose(out pos, out quat);
        RRWheel = pos;
        frontLeftCollider.GetWorldPose(out pos, out quat);
        FLWheel = pos;

        //Calculate wheelbase y wheeltrack
        wheelBase = Vector3.Distance(FRWheel, RRWheel);
        wheelTrack = Vector3.Distance(FRWheel, FLWheel);

        //Obtain the wheelFrictionCurve
        sidewaysFriction[0] = frontLeftCollider.sidewaysFriction;
        sidewaysFriction[1] = frontRightCollider.sidewaysFriction;
        sidewaysFriction[2] = rearLeftCollider.sidewaysFriction;
        sidewaysFriction[3] = rearRightCollider.sidewaysFriction;

        //Add the sidewaysFrictionStiffness to the wheels given the current drive setup
        switch (driveSetup)
        {
            case 0:
                for(int i = 0; i < 2; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                }
                frontLeftCollider.sidewaysFriction = sidewaysFriction[0];
                frontRightCollider.sidewaysFriction = sidewaysFriction[1];
                break;
            case (DriveSetup)1:
                for (int i = 2; i < 4; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                }
                rearLeftCollider.sidewaysFriction = sidewaysFriction[2];
                rearRightCollider.sidewaysFriction = sidewaysFriction[3];
                break;
            case (DriveSetup)2:
                for (int i = 0; i < 4; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                }
                frontLeftCollider.sidewaysFriction = sidewaysFriction[0];
                frontRightCollider.sidewaysFriction = sidewaysFriction[1];
                rearLeftCollider.sidewaysFriction = sidewaysFriction[2];
                rearRightCollider.sidewaysFriction = sidewaysFriction[3];
                break;
            default:
                for (int i = 0; i < 2; i++)
                {
                    sidewaysFriction[i].stiffness = sidewaysFrictionStiffness;
                }
                frontLeftCollider.sidewaysFriction = sidewaysFriction[0];
                frontRightCollider.sidewaysFriction = sidewaysFriction[1];
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
        frontRightCollider.GetWorldPose(out pos, out quat);
        FRWheel = pos;
        rearRightCollider.GetWorldPose(out pos, out quat);
        RRWheel = pos;
        frontLeftCollider.GetWorldPose(out pos, out quat);
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
        AntirollBarCalculation(frontLeftCollider, frontRightCollider, frontAntirollStiffness);
        AntirollBarCalculation(rearLeftCollider, rearRightCollider, rearAntirollStiffness);
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
                frontLeftCollider.motorTorque = motorForceEachWheel * 1.25f;
                frontRightCollider.motorTorque = motorForceEachWheel * 1.25f;
                break;
            case (DriveSetup)1:
                motorForceEachWheel = motorTorque / 2;
                rearLeftCollider.motorTorque = motorForceEachWheel * 1.25f;
                rearRightCollider.motorTorque = motorForceEachWheel * 1.25f;
                break;
            case (DriveSetup)2:
                motorForceEachWheel = motorTorque / 4;
                frontLeftCollider.motorTorque = motorForceEachWheel * 1.25f;
                frontRightCollider.motorTorque = motorForceEachWheel * 1.25f;
                rearLeftCollider.motorTorque = motorForceEachWheel * 1.25f;
                rearRightCollider.motorTorque = motorForceEachWheel * 1.25f;
                break;
            default:
                frontLeftCollider.motorTorque = motorForceEachWheel;
                frontRightCollider.motorTorque = motorForceEachWheel;
                break;
        }

        currentBrakeForce = isBraking || isHandbraking ? brakeForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        
        if (isHandbraking)
        {
            rearLeftCollider.brakeTorque = currentBrakeForce * 2.5f;
            rearRightCollider.brakeTorque = currentBrakeForce * 2.5f;
        }
        else
        {
            frontLeftCollider.brakeTorque = currentBrakeForce;
            frontRightCollider.brakeTorque = currentBrakeForce;
            rearLeftCollider.brakeTorque = currentBrakeForce;
            rearRightCollider.brakeTorque = currentBrakeForce;
        }
    }

    private void HandleSteering()
    {
        //Ackerman Steering formula
        //steer angle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (wheelRadius + (wheelTrack / 2))) * InputDirection
        if (horizontalInput > 0 || horizontalInput < 0)
        {
            currentSteerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (frontLeftCollider.radius + (wheelTrack / 2))) * horizontalInput;
            frontLeftCollider.steerAngle = Mathf.Clamp(currentSteerAngle, -maxSteeringAngle, maxSteeringAngle);
            currentSteerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (frontRightCollider.radius + (wheelTrack / 2))) * horizontalInput;
            frontRightCollider.steerAngle = Mathf.Clamp(currentSteerAngle, -maxSteeringAngle, maxSteeringAngle);
        }
        else
        {
            frontLeftCollider.steerAngle = 0;
            frontRightCollider.steerAngle = 0;
        }
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftCollider, frontLeftTransform);
        UpdateSingleWheel(frontRightCollider, frontRightTransform);
        UpdateSingleWheel(rearLeftCollider, rearLeftTransform);
        UpdateSingleWheel(rearRightCollider, rearRightTransform);
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
        switch(gearbox)
        {
            case 0:
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
            case (GearBox)1:
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

        sum += frontLeftCollider.rpm;
        sum += frontRightCollider.rpm;
        sum += rearLeftCollider.rpm;
        sum += rearRightCollider.rpm;

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
        if(frontLeftCollider.isGrounded && frontRightCollider.isGrounded && rearLeftCollider.isGrounded && rearRightCollider)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
