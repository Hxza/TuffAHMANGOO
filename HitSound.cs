using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace Locomotion
{
    [Serializable]
    public struct HitData
    {
        public PhysicMaterial physicMaterial;
        public AudioClip[] audioClips;
    }
    
    [RequireComponent(typeof(AudioSource), typeof(SphereCollider))]
    public class HitSound : MonoBehaviour
    {
        [Header("Hit sound Config")]
        public List<HitData> hitData = new List<HitData>();
        
        public float defaultVolume = 0.3f;
        public float maxVolume = 1.0f;
        
        public float minVelocity = 0.5f;
        public float maxVelocity = 2.0f;
        
        
        [Space(12), Header("Haptic Config")]
        public XRNode hand = XRNode.LeftHand;
        private InputDevice _inputDevice;
        
        public float defaultHapticStrength = 0.05f;
        public float maxHapticStrength = 0.2f;
        public float hapticDuration = 0.1f;
        
        private void OnTriggerEnter(Collider c) => HandCollision(c);
        
        /// <summary>
        /// Calculates velocity and connects all the haptic & audio together.
        /// </summary>
        /// <param name="hitCollider"></param>
        private void HandCollision(Collider hitCollider)
        {
            if (hand is not (XRNode.LeftHand or XRNode.RightHand)) return;

            var data = GetHitData(hitCollider);
            if (!data.physicMaterial || data.audioClips.Length == 0) return;

            GetDeviceFromXRNode(hand);
            _inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var deviceVelocity);

            
            var speed = deviceVelocity.magnitude;
            if (speed < minVelocity) speed = minVelocity;
            if (speed > maxVelocity) speed = maxVelocity;

            var t = (speed - minVelocity) / (maxVelocity - minVelocity);
            var vol = Mathf.Lerp(defaultVolume, maxVolume, t);
            var haptic = Mathf.Lerp(defaultHapticStrength, maxHapticStrength, t);

            var clip = data.audioClips[UnityEngine.Random.Range(0, data.audioClips.Length)];
            GetComponent<AudioSource>().PlayOneShot(clip, vol);
            HapticImpulse(haptic, hapticDuration);
        }
        
        /// <summary>
        /// Tries to find a match of PhysicMaterial between "hitCollider" and the hitData. If it finds a match, it returns that.
        /// </summary>
        /// <param name="hitCollider"></param>
        /// <returns></returns>
        private HitData GetHitData(Collider hitCollider)
        {
            if (!hitCollider || !hitCollider.sharedMaterial || hitData.Count == 0) return new HitData();
            
            foreach (var data in hitData.Where(data => hitCollider.sharedMaterial.Equals(data.physicMaterial))) 
                return data;
            
            return new HitData();
        }

        /// <summary>
        /// This sends a haptic impulse to the input device (controller) with inputs as the haptic strength and duration the haptic is going to last.
        /// </summary>
        /// <param name="hapticstrength"></param>
        /// <param name="hapticduration"></param>
        private void HapticImpulse(float hapticstrength, float hapticduration)
        {
            if(!_inputDevice.isValid) GetDeviceFromXRNode(hand);
            
            _inputDevice.SendHapticImpulse(0, hapticstrength, hapticduration);
        }

        /// <summary>
        /// Gets the input device from the xrnode
        /// </summary>
        /// <param name="xrNode"></param>
        private void GetDeviceFromXRNode(XRNode xrNode)
        {
            if (_inputDevice.isValid) return;
            _inputDevice = InputDevices.GetDeviceAtXRNode(xrNode);
        }
    }
}
