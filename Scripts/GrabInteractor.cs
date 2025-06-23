using UnityEngine;

namespace Locomotion.Scripts
{
    public class GrabInteractor : MonoBehaviour
    {
        public bool testGrab;
        public LayerMask interactableLayer;
        public Transform palm;
        public float rayLenght = 0.1f;

        private bool _isGrabbing;
        private Transform _grabbableTransform;
        private IGrabbable _grabbableInterface;
        
        private void TryGrab()
        {
            if (_isGrabbing) return;
            _isGrabbing = true;
            
            if (Physics.Raycast(palm.position, -palm.up, out var hitInfo, rayLenght, interactableLayer))
            {
                if (!hitInfo.transform) return;
                
                hitInfo.transform.TryGetComponent<IGrabbable>(out var grabbable);
                Grab(hitInfo.transform, grabbable);
            }
        }

        private void Grab(Transform grabTransform, IGrabbable grabbable)
        {
            _grabbableTransform = grabTransform;
            _grabbableInterface = grabbable;
            
            _grabbableInterface?.OnGrab();
        }

        private void Drop()
        {
            if (!_isGrabbing) return;
            _isGrabbing = false;
            
            _grabbableInterface?.OnDrop();
            
            _grabbableTransform = null;
            _grabbableInterface = null;
        }
        
        public void Update()
        {
            if(testGrab)
                TryGrab();

            if (_isGrabbing)
            {
                // Not grabbing anymore
                if (!testGrab)
                    Drop();
                
                // Update grab shit here.
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!palm || rayLenght <= 0) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(palm.position, palm.position + -palm.up * rayLenght);
        }
    }
}