using UnityEngine;
using Utilities;
using Utilities.Debugging;

namespace Samples.CharacterController3D.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class Character3DBalancer: MonoBehaviour
    {
        [SerializeField]
        private CharacterMovement3DDataScriptableObject characterMovementData;
        
        public bool Grounded => grounded;
        [SerializeField]
        private bool grounded;

        private Quaternion m_targetRotation;
        private Rigidbody m_rigidbody;
        private RaycastHit[] m_raycastHits;

        private float m_groundDifference;

        //Unity Functions
        //============================================================================================================//
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            m_raycastHits = new RaycastHit[5];
            m_rigidbody = GetComponent<Rigidbody>();

            FaceDirection(transform.forward);
        }

#if UNITY_EDITOR
        private void Update()
        {
            var velocity = m_rigidbody.linearVelocity;
            
            
            Debug.DrawRay(m_rigidbody.position, Vector3.down * (characterMovementData.rideHeight * 2f), Color.red);
            Debug.DrawRay(m_rigidbody.position, Vector3.down * characterMovementData.rideHeight, Color.yellow);
            
            Draw.Arrow(transform.position, velocity.normalized, Color.green);
            
            Debug.DrawRay(m_rigidbody.position + (Vector3.down * characterMovementData.rideHeight), Vector3.up * m_groundDifference, Color.green);

            if (m_raycastHits[0].collider == null)
                return;
            
            Draw.Circle(m_raycastHits[0].point, Vector3.up, Color.magenta, 0.05f, 6);

        }
#endif

        // Update is called once per frame
        private void FixedUpdate()
        {
            AddFloat();
            UpdateUprightForce(0f);
        }

        //============================================================================================================//

        public void FaceDirection(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return;
            
            m_targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void AddFloat()
        {
            var hitCount = Physics.RaycastNonAlloc(m_rigidbody.position, Vector3.down, m_raycastHits, characterMovementData.rideHeight * 2f,
                characterMovementData.GroundLayer.value);

            if (hitCount == 0)
            {
                grounded = false;
                return;
            }

            var rayHit = m_raycastHits.GetNearestHit(hitCount);

            //Check if Grounded
            //------------------------------------------------//
            m_groundDifference = rayHit.distance - characterMovementData.rideHeight;
            grounded = m_groundDifference <= 0f;

            if (!grounded)
                return;
            //------------------------------------------------//

            var velocity = m_rigidbody.linearVelocity;
            var rayDir = transform.TransformDirection(Vector3.down);
            
            var otherVelocity = Vector3.zero;
            var hitBody = rayHit.rigidbody;

            var hasHitBody = hitBody != null;

            if (hasHitBody)
                otherVelocity = hitBody.linearVelocity;

            var rayDirectionVelocity = Vector3.Dot(rayDir, velocity);
            var otherDirectionVelocity = Vector3.Dot(rayDir, otherVelocity);

            var directionVelocity = rayDirectionVelocity - otherDirectionVelocity;
            var springForce = (m_groundDifference * characterMovementData.springStrength) - (directionVelocity * characterMovementData.springDamper);

            m_rigidbody.AddForce(rayDir * springForce);

            if(hasHitBody)
                hitBody.AddForceAtPosition(rayDir * -springForce, rayHit.point);
        }

        private void UpdateUprightForce(float elapsed)
        {
            var characterCurrent = m_rigidbody.rotation;
            var toGoal = JMath.ShortestRotation(m_targetRotation, characterCurrent);

            toGoal.ToAngleAxis(out var rotDegrees, out var rotAxis);
            rotAxis.Normalize();

            var rotRadian = rotDegrees * Mathf.Deg2Rad;
            m_rigidbody.AddTorque((rotAxis * (rotRadian * characterMovementData.uprightStrength)) - (m_rigidbody.angularVelocity * characterMovementData.uprightDamper));
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
                return;
            
            Debug.DrawRay(transform.position, Vector3.down * (characterMovementData.rideHeight * 2f), Color.red);
            Debug.DrawRay(transform.position, Vector3.down * characterMovementData.rideHeight, Color.yellow);
        }

#endif
    }
}