using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Floofinator.RigidbodyCharacter
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class RigidbodyCharacterController : MonoBehaviour
    {
        [SerializeField, Tooltip("Layers considered in casting."), Header("Physics")] LayerMask castMask;
        [SerializeField, Tooltip("Distance to cast for the ground.")] float groundDistance = 0.3f;
        [SerializeField, Tooltip("Margin for casting.")] float castMargin = 0.01f;
        [SerializeField, Range(0, 1), Tooltip("Minimum dot product between ground normal and transform up to be considered ground.")] float groundDot = 0.5f;
        [SerializeField, Tooltip("Apply gravity to transform down.")] bool transformGravity = false;

        public bool LastGrounded => lastGrounded;
        public Rigidbody Rigidbody => rb;
        public UnityEvent OnGrounded, OnUngrounded, OnBeforeUpdate, OnAfterUpdate;
        CapsuleCollider capsule;
        Rigidbody rb;
        Vector3 lastGroundNormal = Vector3.up;
        Vector3 lastVelocity;
        bool lastGrounded;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();

            rb.freezeRotation = true;
            rb.useGravity = false;

            capsule.material = new()
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                bounciness = 0f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
        }
        void FixedUpdate()
        {
            rb.velocity = lastVelocity;

            OnBeforeUpdate?.Invoke();

            CheckGround();

            OnAfterUpdate?.Invoke();

            //store velocity to reinforce movement after collisions
            lastVelocity = rb.velocity;
        }
        public Vector3 VerticalVelocity()
        {
            return Vector3.Project(rb.velocity, GravityVector().normalized);
        }
        public Vector3 HorizontalVelocity()
        {
            return Vector3.ProjectOnPlane(rb.velocity, GravityVector().normalized);
        }
        public Vector3 GroundProject(Vector3 vector)
        {
            return Vector3.ProjectOnPlane(vector, lastGroundNormal);
        }
        public Vector3 GravityVector()
        {
            if (transformGravity)
                return Physics.gravity.magnitude * -transform.up;
            else
                return Physics.gravity;
        }
        void CheckGround()
        {
            bool grounded = GroundCast(out RaycastHit hitInfo);

            if (grounded)
            {
                lastGroundNormal = hitInfo.normal;

                if (!lastGrounded) OnGrounded?.Invoke();

                rb.velocity = GroundProject(rb.velocity);
            }
            else
            {
                lastGroundNormal = -GravityVector().normalized;

                if (lastGrounded) OnUngrounded?.Invoke();

                ApplyGravity();
            }

            lastGrounded = grounded;
        }
        void ApplyGravity()
        {
            rb.velocity += GravityVector() * Time.deltaTime;
        }
        bool GroundCast(out RaycastHit hitInfo)
        {
            Vector3 horizontal = HorizontalVelocity();

            Vector3 velocityDirection = horizontal.normalized;
            float velocityDistance = horizontal.magnitude * Time.deltaTime;

            Vector3 upDirection = -GravityVector().normalized;

            //start casting from our position;
            Vector3 castOffset = Vector3.zero;

            //first cast upwards by step height
            Cast(ref castOffset, groundDistance * upDirection, out hitInfo);

            //then cast forwards by distance
            Cast(ref castOffset, velocityDistance * velocityDirection, out hitInfo);

            //then cast down by twice step height
            if (Cast(ref castOffset, groundDistance * 2f * -upDirection, out hitInfo))
            {
                Vector3 verticalOffset = Vector3.Project(castOffset, upDirection);
                Vector3 hitDir = Vector3.ProjectOnPlane(rb.position - hitInfo.point, upDirection);

                Vector3 normalCastPoint = hitInfo.point + hitDir * Time.deltaTime + upDirection * groundDistance;
                //cast down again to get the normal from above
                Physics.Raycast(normalCastPoint, -upDirection, out hitInfo, groundDistance * 2, castMask);
                //check normal can be counted as ground
                if (Vector3.Dot(hitInfo.normal, upDirection) > groundDot)
                {
                    //move ourselves vertically if we cast for the ground
                    rb.position = (rb.position + verticalOffset);

                    return true;
                }
            }

            hitInfo = default;

            return false;
        }
        bool Cast(ref Vector3 pointOffset, Vector3 vector, out RaycastHit hitInfo)
        {
            Vector3 direction = vector.normalized;
            float distance = vector.magnitude + castMargin;

            Vector3 pointMargin = -direction * castMargin;

            Vector3 bottomCastPoint = CapsuleExtent(Vector3.down) + pointOffset + pointMargin;
            Vector3 topCastPoint = CapsuleExtent(Vector3.up) + pointOffset + pointMargin;

            bool hit = Physics.CapsuleCast(bottomCastPoint, topCastPoint, capsule.radius, direction, out hitInfo, distance, castMask);

            if (hit) distance = hitInfo.distance;

            distance -= castMargin;

            Vector3 castOffset = distance * direction;

            pointOffset += castOffset;

            Debug.DrawLine(bottomCastPoint, bottomCastPoint + castOffset);
            Debug.DrawLine(topCastPoint, topCastPoint + castOffset);

            return hit;
        }
        Vector3 CapsuleExtent(Vector3 direction)
        {
            return rb.position + rb.rotation * (capsule.center + direction * (capsule.height / 2f - capsule.radius));
        }
    }
}