using UnityEngine;

namespace Locomotion.Scripts
{
    public class ClimbableObject : MonoBehaviour, IGrabbable
    {
        public void OnGrab()
        {
            Debug.Log("Grabbed a climbable obj");
        }

        public void OnDrop()
        {
            Debug.Log("Released a climbable obj");
        }
    }
}