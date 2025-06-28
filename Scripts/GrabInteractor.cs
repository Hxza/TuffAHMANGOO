using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace Locomotion.Scripts
{
    public class GrabInteractor : MonoBehaviour
    {
        public bool testGrab;
        public LayerMask interactableLayer;
        public Transform palm;
        public float RaycastLenght = 0.1f;
        public Rigidbody physicHand;

        public XRNode node = XRNode.LeftHand;
        private InputDevice _inputDevice;
        [Header("joint settings")] 
        public float breakForce = 5000f;

        public bool isGrabbing;
        private FixedJoint _grabbableJoint;
        private Transform _grabbableTransform;
        private Rigidbody _grabbableRigidbody;
        private IGrabbable _grabbableInterface;
        
        private void TryGrab()
        {
            if (isGrabbing) return;
            isGrabbing = true;
            
            if (Physics.Raycast(palm.position, -palm.up, out var hitInfo, RaycastLenght, interactableLayer))
            {
                if (!hitInfo.transform) return;
                Grab(hitInfo);
            }
        }

        private void Grab(RaycastHit hitInfo)
        {
            _grabbableTransform = hitInfo.transform;
            hitInfo.transform.TryGetComponent(out _grabbableInterface);
            hitInfo.transform.TryGetComponent(out _grabbableRigidbody);
            
            _grabbableInterface?.OnGrab();

            if (_grabbableJoint)
                Destroy(_grabbableJoint);

            _grabbableJoint = physicHand.gameObject.AddComponent<FixedJoint>();
            //_grabbableJoint.breakForce = breakForce;

            if (_grabbableRigidbody)
            {
                _grabbableJoint.connectedBody = _grabbableRigidbody;
                _grabbableJoint.autoConfigureConnectedAnchor = true;
            }
            else
            {
                _grabbableJoint.connectedBody = null;
                _grabbableJoint.autoConfigureConnectedAnchor = false;
                _grabbableJoint.anchor = 
                    transform.InverseTransformPoint(hitInfo.point);
                _grabbableJoint.connectedAnchor = hitInfo.point;
            }

        }

        private void Drop()
        {
            if (!isGrabbing) return;
            isGrabbing = false;
            
            _grabbableInterface?.OnDrop();
            
            _grabbableTransform = null;
            _grabbableInterface = null;
            
            if(_grabbableJoint)
                Destroy(_grabbableJoint);
        }
        
        public void Update()
        {
            if (!_inputDevice.isValid)
                _inputDevice = InputDevices.GetDeviceAtXRNode(node);
            else
                testGrab = _inputDevice.TryGetFeatureValue(CommonUsages.grip, out var gripValue) && gripValue > 0.7f;
            
            if(testGrab)
                TryGrab();

            if (isGrabbing)
            {
                // Not grabbing anymore
                if (!testGrab)
                    Drop();

                if (_grabbableRigidbody)
                {
                    var velocity = _grabbableRigidbody.velocity.magnitude;
                    var dragStrength = Mathf.InverseLerp(3f, 10f, velocity);
                    var hapticStrength = Mathf.Lerp(0f, 1f, dragStrength);

                    Player.Instance.HapticImpulse(
                        isLeft: node == XRNode.LeftHand,
                        hapticstrength: hapticStrength,
                        hapticduration: 0.05f
                    );
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!palm || RaycastLenght <= 0) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(palm.position, palm.position + -palm.up * RaycastLenght);
        }
    }
}