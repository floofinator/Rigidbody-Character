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
        float jumpBuffer, coyoteTime;
        private void Awake()
        {
            character = GetComponent<RigidbodyCharacterController>();
            character.OnBeforeUpdate.AddListener(ApplyMovement);
            character.OnAfterUpdate.AddListener(CheckJump);
        }

        private void Update()
        {
            moveVector = viewTransform.forward * Input.GetAxisRaw("Vertical") + viewTransform.right * Input.GetAxisRaw("Horizontal");
            moveVector = moveVector.normalized;

            if (character.LastGrounded) coyoteTime = coyoteTimeLength;
            else coyoteTime -= Time.deltaTime;

            if (Input.GetButtonDown("Jump")) jumpBuffer = jumpBufferLength;
            else jumpBuffer -= Time.deltaTime;
        }
        void ApplyMovement()
        {
            Vector3 groundVelocity = character.GetGroundVelocity();

            float speed = character.LastGrounded ? groundSpeed : airSpeed;
            float acceleration = character.LastGrounded ? groundAcceleration : airAcceleration;

            groundVelocity = Vector3.MoveTowards(groundVelocity, moveVector * speed, Time.deltaTime * acceleration);

            character.SetGroundVelocity(groundVelocity);
        }
        void CheckJump()
        {
            if (coyoteTime > 0 && jumpBuffer > 0)
            {
                character.LeaveGround();

                Vector3 gravity = character.GravityVector();
                character.SetNormalVelocity(-gravity.normalized * Mathf.Sqrt(2 * jumpHeight * gravity.magnitude));
                
                jumpBuffer = 0;
            }
        }
    }
}
