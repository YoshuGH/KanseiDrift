using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.3F;
    private Vector3 velocity = Vector3.zero;
    [SerializeField] private float maxTranslateSpeed;
    [SerializeField] private float rotationSpeed;

    private void FixedUpdate()
    {
        HandleTranslation();
        HandleRotation();
    }

    private void HandleTranslation()
    {
        var targetPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, maxTranslateSpeed);
    }

    private void HandleRotation()
    {
        var dir = target.position - transform.position;
        var rotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }
}
