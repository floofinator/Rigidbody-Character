using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Floofinator.RigidbodyCharacter
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class RigidbodyCharacterController : MonoBehaviour
    {
        [SerializeField, Tooltip("Layers considered in casting."), Header("Physics")] LayerMask castMask;
        [SerializeField, Tooltip("Distance to cast for the ground.")] float groundDistance = 0.05f;
        [SerializeField, Tooltip("Height to cast for step.")] float stepHeight = 0.3f;
        [SerializeField, Tooltip("Margin for casting.")] float castMargin = 0.01f;
        [SerializeField, Range(0, 1), Tooltip("Minimum dot product between ground normal and transform up to be considered ground.")] float groundDot = 0.5f;
        public bool LastGrounded => lastGrounded;
        public Rigidbody Rigidbody => rb;
        public UnityEvent OnGrounded, OnUngrounded, OnBeforeUpdate, OnAfterUpdate;
        CapsuleCollider capsule;
        Rigidbody rb;
        Vector3 groundNormal = Vector3.up, groundPoint = Vector3.zero;
        bool lastGrounded, leaveGround;
        Rigidbody groundBody;
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
            OnBeforeUpdate?.Invoke();

            CheckGround();

            OnAfterUpdate?.Invoke();
        }
        public Vector3 GetGroundVelocity()
        {
            return GroundProject(rb.velocity) - GroundProject(BodyVelocity());
        }
        public void SetGroundVelocity(Vector3 velocity)
        {
            Vector3 vertical = NormalProject(rb.velocity);
            Vector3 horizontal = velocity + GroundProject(BodyVelocity());

            rb.velocity = vertical + horizontal;
        }
        public Vector3 GetNormalVelocity()
        {
            return NormalProject(rb.velocity) - NormalProject(BodyVelocity());
        }
        public void SetNormalVelocity(Vector3 velocity)
        {
            Vector3 vertical = velocity + NormalProject(BodyVelocity());
            Vector3 horizontal = GroundProject(rb.velocity);

            rb.velocity = vertical + horizontal;
        }
        public Vector3 GroundProject(Vector3 vector)
        {
            return Vector3.ProjectOnPlane(vector, groundNormal);
        }
        public Vector3 NormalProject(Vector3 vector)
        {
            return Vector3.Project(vector, groundNormal);
        }
        public Vector3 GravityVector()
        {
            return Physics.gravity.magnitude * -transform.up;
        }
        public void LeaveGround()
        {
            leaveGround = true;
        }
        Vector3 BodyVelocity()
        {
            if (!groundBody) return Vector3.zero;
            return groundBody.GetPointVelocity(groundPoint);
        }
        void CheckGround()
        {
            bool canGround = GroundCast(out RaycastHit groundInfo);

            bool canStep = StepCast(out RaycastHit stepInfo, out Vector3 stepOffset);

            groundBody = null;
            groundNormal = -GravityVector().normalized;

            if (canGround)
            {
                groundNormal = groundInfo.normal;
                groundPoint = groundInfo.point;
                if (groundInfo.collider) groundBody = groundInfo.collider.attachedRigidbody;
            }

            bool groundToStep = canStep && canGround && lastGrounded;
            bool airToStep = canStep && canGround && IsFalling();

            if (airToStep || groundToStep)
            {
                groundNormal = stepInfo.normal;
                groundPoint = stepInfo.point;
                if (stepInfo.collider) groundBody = stepInfo.collider.attachedRigidbody;
            }

            bool grounded = !leaveGround && canGround && IsGround(groundNormal);

            if (grounded)
            {
                ApplyGroundSnap(stepOffset);
                if (!lastGrounded) OnGrounded?.Invoke();
            }
            else
            {
                ApplyGravity();
                if (lastGrounded) OnUngrounded?.Invoke();
            }

            leaveGround = false;
            lastGrounded = grounded;
        }
        void ApplyGroundSnap(Vector3 stepOffset)
        {
            rb.MovePosition(rb.position + stepOffset);
            rb.velocity = GroundProject(rb.velocity) + NormalProject(BodyVelocity());
        }
        void ApplyGroundVelocity()
        {
            rb.MovePosition(rb.position + BodyVelocity() * Time.deltaTime);
        }
        void ApplyGravity()
        {
            rb.velocity += GravityVector() * Time.deltaTime;
        }
        bool GroundCast(out RaycastHit hitInfo)
        {
            Vector3 castOffset = Vector3.zero;
            Vector3 upDirection = -GravityVector().normalized;

            return Cast(ref castOffset, groundDistance * -upDirection, out hitInfo);
        }
        bool IsFalling()
        {
            Vector3 upDirection = -GravityVector().normalized;
            return Vector3.Dot(rb.velocity.normalized, upDirection) < groundDot;
        }
        bool IsGround(Vector3 normal)
        {
            Vector3 upDirection = -GravityVector().normalized;
            return Vector3.Dot(normal, upDirection) > groundDot;
        }
        bool StepCast(out RaycastHit hitInfo, out Vector3 stepOffset)
        {
            stepOffset = Vector3.zero;

            Vector3 horizontal = GroundProject(rb.velocity);

            Vector3 velocityDirection = horizontal.normalized;
            float velocityDistance = horizontal.magnitude * Time.deltaTime;

            Vector3 upDirection = -GravityVector().normalized;

            //start casting from our position;
            Vector3 castOffset = Vector3.zero;

            //first cast upwards by step height
            Cast(ref castOffset, stepHeight * upDirection, out _);

            //then cast forwards by distance
            Cast(ref castOffset, velocityDistance * velocityDirection, out _);

            //then cast down by twice step height
            if (Cast(ref castOffset, stepHeight * 2f * -upDirection, out hitInfo))
            {
                //cast down again to get the normal from above
                Vector3 hitDir = Vector3.ProjectOnPlane(rb.position - hitInfo.point, upDirection);
                Vector3 normalCastPoint = hitInfo.point + hitDir * Time.deltaTime + upDirection * stepHeight;
                Physics.Raycast(normalCastPoint, -upDirection, out hitInfo, stepHeight * 2, castMask);

                //move ourselves vertically if we cast for the ground

                stepOffset = NormalProject(castOffset);

                return true;
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

            return hit;
        }
        Vector3 CapsuleExtent(Vector3 direction)
        {
            return rb.position + rb.rotation * (capsule.center + direction * (capsule.height / 2f - capsule.radius));
        }
    }
}