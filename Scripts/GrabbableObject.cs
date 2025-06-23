using UnityEngine;

namespace Locomotion.Scripts
{
    public class GrabbableObject : MonoBehaviour, IGrabbable
    {
        public void OnGrab()
        {
            Debug.Log("grabbed de objeect");
        }

        public void OnDrop()
        {
            Debug.Log("released de objeect");
        }
    }
}