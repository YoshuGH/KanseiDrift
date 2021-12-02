using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking, isHandbraking, updateCenterOfMass = false;
    private float currentBrakeForce;
    private float currentSteerAngle;
    private float velocity = 0;
    private float wheelBase = 2.55f;
    private float wheelTrack = 1.5f;
    private float kmph;

    private Rigidbody rb = new Rigidbody();

    public Text motor;
    public Text brake;

    private enum DriveSetup { FWD, RWD, AWD }

    [Header("Car Setup"),Space]
    [SerializeField] private DriveSetup driveSetup;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float motorForce;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float brakeForce;
    [SerializeField] private float maxSteeringAngle;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float frontAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("The Value is express in N/m")] 
    private float rearAntirollStiffness = 5000.0f;
    [SerializeField, Tooltip("Is for relocate the center of mass relative to the original")] 
    private Vector3 centerOfMassOffset;

    [Header("Wheel Colliders")]
    [SerializeField, Space, Space] private WheelCollider frontLeftCollider;
    [SerializeField] private WheelCollider frontRightCollider;
    [SerializeField] private WheelCollider rearLeftCollider;
    [SerializeField] private WheelCollider rearRightCollider;

    [Header("Wheel Transform Models")]
    [SerializeField, Space, Space] private Transform frontLeftTransform;
    [SerializeField] private Transform frontRightTransform;
    [SerializeField] private Transform rearLeftTransform;
    [SerializeField] private Transform rearRightTransform;

    public float KMPH
    {
        get { return kmph; }
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
        wheelTrack = Vector3.Distance(FRWheel,FLWheel);
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

    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();

        //Calculate the Antirollforce for the antiroll bars in both axles (Front and Rear)
        AntirollBarCalculation(frontLeftCollider, frontRightCollider, frontAntirollStiffness);
        AntirollBarCalculation(rearLeftCollider, rearRightCollider, rearAntirollStiffness);

        //Calculate Speed in KMPH
        kmph = rb.velocity.magnitude * 3.6f;

        motor.text = "RPM: " + rearLeftCollider.rpm;
        brake.text = "Brake Torque R: " + rearLeftCollider.brakeTorque;
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxis(HORIZONTAL);
        verticalInput = Input.GetAxis(VERTICAL);
        isBraking = Input.GetKey(KeyCode.Space);
        isHandbraking = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleMotor()
    {
        float motorForceEachWheel = 0;
        switch(driveSetup)
        {
            case 0:
                motorForceEachWheel = motorForce / 2;
                frontLeftCollider.motorTorque = verticalInput * motorForce;
                frontRightCollider.motorTorque = verticalInput * motorForce;
                break;
            case (DriveSetup)1:
                motorForceEachWheel = motorForce / 2;
                rearLeftCollider.motorTorque = ((verticalInput * motorForce) / 3.14f) * 3.73f;
                rearRightCollider.motorTorque = ((verticalInput * motorForce) / 3.14f) * 3.73f;
                break;
            case (DriveSetup)2:
                motorForceEachWheel = motorForce / 4;
                frontLeftCollider.motorTorque = verticalInput * motorForce;
                frontRightCollider.motorTorque = verticalInput * motorForce;
                rearLeftCollider.motorTorque = verticalInput * motorForce;
                rearRightCollider.motorTorque = verticalInput * motorForce;
                break;
            default:
                frontLeftCollider.motorTorque = verticalInput * motorForce;
                frontRightCollider.motorTorque = verticalInput * motorForce;
                break;
        }

        currentBrakeForce = isBraking || isHandbraking ? brakeForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        WheelFrictionCurve forwardFriction = rearLeftCollider.forwardFriction;
        WheelFrictionCurve sidewaysFriction = rearLeftCollider.sidewaysFriction;

        if (isHandbraking)
        {
            rearLeftCollider.brakeTorque = currentBrakeForce * 1000;
            rearRightCollider.brakeTorque = currentBrakeForce * 1000;
            
            float stiffnessFriction = Mathf.SmoothDamp(
                rearLeftCollider.forwardFriction.stiffness, 0.15f, ref velocity, Time.deltaTime * 1.2f);

            forwardFriction.stiffness = stiffnessFriction;
            sidewaysFriction.stiffness = stiffnessFriction;

            rearLeftCollider.forwardFriction = forwardFriction;
            rearRightCollider.forwardFriction = forwardFriction;
            rearLeftCollider.sidewaysFriction = sidewaysFriction;
            rearRightCollider.sidewaysFriction = sidewaysFriction;
        }
        else
        {
            frontLeftCollider.brakeTorque = currentBrakeForce;
            frontRightCollider.brakeTorque = currentBrakeForce;
            rearLeftCollider.brakeTorque = currentBrakeForce;
            rearRightCollider.brakeTorque = currentBrakeForce;

            forwardFriction.stiffness = Mathf.SmoothDamp(
                rearLeftCollider.forwardFriction.stiffness, 0.75f, ref velocity, Time.deltaTime * 10f);
            sidewaysFriction.stiffness = Mathf.SmoothDamp(
                rearLeftCollider.sidewaysFriction.stiffness, 0.45f, ref velocity, Time.deltaTime * 2.5f);

            rearLeftCollider.forwardFriction = forwardFriction;
            rearRightCollider.forwardFriction = forwardFriction;
            rearLeftCollider.sidewaysFriction = sidewaysFriction;
            rearRightCollider.sidewaysFriction = sidewaysFriction;
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
}
