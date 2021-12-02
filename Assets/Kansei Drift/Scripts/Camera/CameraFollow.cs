using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private Transform target;
    [SerializeField] private double smoothTime = 0.3F;
    [SerializeField] private float maxTranslateSpeed;
    [SerializeField] private float rotationSpeed;
    private Vector3 velocity = Vector3.zero;
    private float fVelocity = 0;
    private CarController carController;

    private void Start()
    {
        carController = target.GetComponent<CarController>();
    }

    private void FixedUpdate()
    {
        HandleTranslation();
        HandleRotation();

        if (carController != null)
        {
            if(carController.KMPH >= 25)
            {
                smoothTime = Mathf.SmoothDamp((float)smoothTime, 0.01f, ref fVelocity, 1.5f);
            }
            else
            {
                smoothTime = carController.KMPH / 75;
            }
        }
    }

    private void HandleTranslation()
    {
        var targetPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, (float)smoothTime, maxTranslateSpeed);
    }

    private void HandleRotation()
    {
        var dir = target.position - transform.position;
        var rotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }
}
