using System;
using UnityEngine;
using UnityEngine.XR;

namespace Locomotion.Scripts
{
    public class Player : MonoBehaviour
    {
        private static Player _instance;
        public static Player Instance => _instance;

        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        public Transform leftHandFollower;
        public Transform rightHandFollower;

        public Transform rightHandTransform;
        public Transform leftHandTransform;

        public ConfigurableJoint leftPhysic, rightPhysic;

        private Vector3 _lastLeftHandPosition;
        private Vector3 _lastRightHandPosition;
        private Vector3 _lastHeadPosition;
        private Vector3 _lastBodyPosition;

        private Rigidbody _playerRigidBody;

        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;

        public float velocityLimit;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;

        private Vector3[] _velocityHistory;
        private int _velocityIndex;
        private Vector3 _currentVelocity;
        private Vector3 _denormalizedVelocityAverage;
        private bool _jumpHandIsLeft;
        private Vector3 _lastPosition;

        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;

        public LayerMask locomotionEnabledLayers;

        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;

        public bool disableMovement;

        private void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(gameObject);
            else
                _instance = this;

            InitializeValues();
        }

        private void InitializeValues()
        {
            _playerRigidBody = GetComponent<Rigidbody>();
            _velocityHistory = new Vector3[velocityHistorySize];
            _lastLeftHandPosition = leftHandFollower.transform.position;
            _lastRightHandPosition = rightHandFollower.transform.position;
            _lastHeadPosition = headCollider.transform.position;
            _lastBodyPosition = bodyCollider.transform.position;
            _velocityIndex = 0;
            _lastPosition = transform.position;
        }

        private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude <
                maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }

            return headCollider.transform.position +
                   (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position)
                   .normalized * maxArmLength;
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude <
                maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }

            return headCollider.transform.position +
                   (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position)
                   .normalized * maxArmLength;
        }

        private static Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private void Update()
        {
            Vector3 rigidBodyMovement;
            var firstIterationLeftHand = Vector3.zero;
            var firstIterationRightHand = Vector3.zero;

            var leftHandColliding = false;
            var rightHandColliding = false;

            bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

            //left hand

            var distanceTraveled = CurrentLeftHandPosition() - _lastLeftHandPosition +
                                   Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

            if (IterativeCollisionSphereCast(_lastLeftHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out var finalPosition, true))
            {
                //this lets you stick to the position you touch, as long as you keep touching the surface,
                //this will be the zero point for that hand
                if (wasLeftHandTouching)
                {
                    firstIterationLeftHand = _lastLeftHandPosition - CurrentLeftHandPosition();
                }
                else
                {
                    firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();
                }

                _playerRigidBody.velocity = Vector3.zero;

                leftHandColliding = true;
            }

            //right hand

            distanceTraveled = CurrentRightHandPosition() - _lastRightHandPosition +
                               Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

            if (IterativeCollisionSphereCast(_lastRightHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, true))
            {
                if (wasRightHandTouching)
                {
                    firstIterationRightHand = _lastRightHandPosition - CurrentRightHandPosition();
                }
                else
                {
                    firstIterationRightHand = finalPosition - CurrentRightHandPosition();
                }

                _playerRigidBody.velocity = Vector3.zero;

                rightHandColliding = true;
            }

            //average or add

            if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
            {
                //this lets you grab stuff with both hands at the same time
                rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
            }
            else
            {
                rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
            }

            //check valid head movement

            if (IterativeCollisionSphereCast(_lastHeadPosition, headCollider.radius,
                    headCollider.transform.position + rigidBodyMovement - _lastHeadPosition, defaultPrecision,
                    out finalPosition, false))
            {
                rigidBodyMovement = finalPosition - _lastHeadPosition;
                //last check to make sure the head won't phase through geometry
                if (Physics.Raycast(_lastHeadPosition,
                        headCollider.transform.position - _lastHeadPosition + rigidBodyMovement, out _,
                        (headCollider.transform.position - _lastHeadPosition + rigidBodyMovement).magnitude +
                        headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = _lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position += rigidBodyMovement;
            }

            _lastHeadPosition = headCollider.transform.position;
            
            //do the final left-hand position

            distanceTraveled = CurrentLeftHandPosition() - _lastLeftHandPosition;

            if (IterativeCollisionSphereCast(_lastLeftHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition,
                    !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
            {
                _lastLeftHandPosition = finalPosition;
                leftHandColliding = true;
            }
            else
            {
                _lastLeftHandPosition = CurrentLeftHandPosition();
            }

            //do the final right-hand position

            distanceTraveled = CurrentRightHandPosition() - _lastRightHandPosition;

            if (IterativeCollisionSphereCast(_lastRightHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition,
                    !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
            {
                _lastRightHandPosition = finalPosition;
                rightHandColliding = true;
            }
            else
            {
                _lastRightHandPosition = CurrentRightHandPosition();
            }

            StoreVelocities();

            if ((rightHandColliding || leftHandColliding) && !disableMovement)
            {
                if (_denormalizedVelocityAverage.magnitude > velocityLimit)
                {
                    if (_denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                    {
                        _playerRigidBody.velocity = _denormalizedVelocityAverage.normalized * maxJumpSpeed;
                    }
                    else
                    {
                        _playerRigidBody.velocity = jumpMultiplier * _denormalizedVelocityAverage;
                    }
                }
            }

            //check to see if the left-hand is stuck and we should unstick it

            if (leftHandColliding && (CurrentLeftHandPosition() - _lastLeftHandPosition).magnitude > unStickDistance &&
                !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision,
                    CurrentLeftHandPosition() - headCollider.transform.position, out _,
                    (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance,
                    locomotionEnabledLayers.value))
            {
                _lastLeftHandPosition = CurrentLeftHandPosition();
                leftHandColliding = false;
            }

            //check to see if the right-hand is stuck and we should unstick it

            if (rightHandColliding &&
                (CurrentRightHandPosition() - _lastRightHandPosition).magnitude > unStickDistance &&
                !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision,
                    CurrentRightHandPosition() - headCollider.transform.position, out _,
                    (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance,
                    locomotionEnabledLayers.value))
            {
                _lastRightHandPosition = CurrentRightHandPosition();
                rightHandColliding = false;
            }

            leftHandFollower.position = _lastLeftHandPosition;
            rightHandFollower.position = _lastRightHandPosition;

            leftHandFollower.rotation = leftHandTransform.rotation;
            rightHandFollower.rotation = rightHandTransform.rotation;

            wasLeftHandTouching = leftHandColliding;
            wasRightHandTouching = rightHandColliding;
        }
        
        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector,
            float precision, out Vector3 endPosition, bool singleHand)
        {
            //first sphere cast from the starting position to the final position
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision,
                    out endPosition, out var hitInfo))
            {
                //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands, you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

                //take the surface normal that we hit, then along that plane, do a sphere cast to a position a small distance away to account for moving perpendicular to that surface
                var firstPosition = endPosition;
                var gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                var slipPercentage = gorillaSurface != null
                    ? gorillaSurface.slipPercentage
                    : (!singleHand ? defaultSlideFactor : 0.001f);
                var movementToProjectedAboveCollisionPlane =
                    Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) *
                    slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane,
                        precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface

                if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius,
                        startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition),
                        precision * precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }

                endPosition = firstPosition;
                return true;
            }

            //as a kind of check, try a smaller sphere cast. this accounts for times when the original sphere cast was already touching a surface, so it didn't trigger correctly
            if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f,
                    movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f),
                    precision * 0.66f, out endPosition, out hitInfo))
            {
                endPosition = startPosition;
                return true;
            }

            endPosition = Vector3.zero;
            return false;
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector,
            float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped-up sphere cast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphere radius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial sphere case
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo,
                    movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphere radius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a sphere case from the original position to the intended final position
                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision,
                        finalPosition - startPosition, out var innerHit,
                        (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision),
                        locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized *
                        Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the sphere cast. helps prevent clipping through geometry
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit,
                             (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                             locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }

                return true;
            }

            //anti-clipping through geometry check
            if (Physics.Raycast(startPosition, movementVector, out hitInfo,
                    movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }

            finalPosition = Vector3.zero;
            return false;
        }
        
        public bool IsHandTouching(bool forLeftHand)
        {
            return forLeftHand ? wasLeftHandTouching : wasRightHandTouching;
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            _denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * _denormalizedVelocityAverage;

            for (var i = 0; i < _velocityHistory.Length; i++)
                _velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * _velocityHistory[i];
        }

        private void StoreVelocities()
        {
            _velocityIndex = (_velocityIndex + 1) % velocityHistorySize;
            var oldestVelocity = _velocityHistory[_velocityIndex];
            _currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
            _denormalizedVelocityAverage += (_currentVelocity - oldestVelocity) / velocityHistorySize;
            _velocityHistory[_velocityIndex] = _currentVelocity;
            _lastPosition = transform.position;
        }

        /// <summary>
        /// This sends a haptic impulse to the input device (controller) with inputs as the haptic strength and duration the haptic is going to last.
        /// </summary>
        /// <param name="isLeft"></param>
        /// <param name="hapticstrength"></param>
        /// <param name="hapticduration"></param>
        public void HapticImpulse(bool isLeft, float hapticstrength, float hapticduration)
        {
            GetDeviceFromXRNode(isLeft ? XRNode.LeftHand : XRNode.RightHand, out var inputDevice);
            inputDevice.SendHapticImpulse(0, hapticstrength, hapticduration);
        }

        /// <summary>
        /// Gets the input device from the xrnode
        /// </summary>
        /// <param name="xrNode"></param>
        /// <param name="inputDevice"></param>
        public void GetDeviceFromXRNode(XRNode xrNode, out InputDevice inputDevice)
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(xrNode);
        }
    }
}