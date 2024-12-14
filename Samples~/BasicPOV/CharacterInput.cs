using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Floofinator.RigidbodyCharacter.Samples
{
    [RequireComponent(typeof(RigidbodyCharacterController))]
    public class CharacterInput : MonoBehaviour
    {
        [SerializeField] float groundSpeed = 2f, airSpeed = 2f;
        [SerializeField] float groundAcceleration = 20f, airAcceleration = 10f;
        [SerializeField] float coyoteTimeLength = 0.2f, jumpBufferLength = 0.2f;
        [SerializeField] float jumpHeight = 2f;
        [SerializeField] Transform viewTransform;
        RigidbodyCharacterController character;
        Vector3 moveVector;
        Vector3 lastPos;
        float jumpBuffer;
        float coyoteTime;
        private void Awake()
        {
            character = GetComponent<RigidbodyCharacterController> ();
            character.OnBeforeUpdate.AddListener(ApplyCharacterInput);
            character.OnAfterUpdate.AddListener(After);
        }

        private void After()
        {
            Vector3 velocity = (character.Rigidbody.position - lastPos)/Time.deltaTime;
            Debug.Log(character.HorizontalVelocity() + "/" + velocity);
            lastPos = character.Rigidbody.position;
        }

        private void Update()
        {
            moveVector = viewTransform.forward * Input.GetAxisRaw("Vertical") + viewTransform.right * Input.GetAxisRaw("Horizontal");
            moveVector = moveVector.normalized;
        }
        void ApplyCharacterInput()
        {
            if (character.LastGrounded) coyoteTime = coyoteTimeLength;
            else coyoteTime -= Time.deltaTime;

            if (Input.GetButtonDown("Jump")) jumpBuffer = jumpBufferLength;
            else jumpBuffer -= Time.deltaTime;

            if (coyoteTime > 0 && jumpBuffer > 0)
            {
                Vector3 gravity = character.GravityVector();
                character.Rigidbody.velocity = character.HorizontalVelocity() + -gravity.normalized * Mathf.Sqrt(2 * jumpHeight * gravity.magnitude);
            }

            Vector3 groundVelocity = character.HorizontalVelocity();
            
            moveVector = character.GroundProject(moveVector);

            float speed = character.LastGrounded ? groundSpeed: airSpeed;
            float acceleration = character.LastGrounded ? groundAcceleration: airAcceleration;

            groundVelocity = Vector3.MoveTowards(groundVelocity, moveVector * speed, Time.deltaTime * acceleration);

            character.Rigidbody.velocity = groundVelocity + character.VerticalVelocity();
        }
    }
}
