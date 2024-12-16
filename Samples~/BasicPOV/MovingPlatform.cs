using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] Vector3 rotation;
    [SerializeField] Vector3 tranlation;
    [SerializeField] float frequency;
    Rigidbody rb;
    Vector3 startPos;
    Quaternion startRot;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        startPos = rb.position;
        startRot = rb.rotation;
    }
    void FixedUpdate()
    {
        rb.MovePosition(startPos + Mathf.Sin(Time.time * frequency) * tranlation);
        rb.MoveRotation(Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Euler(rotation), Mathf.Sin(Time.time * frequency)) * startRot);
    }
}
