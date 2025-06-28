using System.Collections.Generic;
using AdaptiveHands;
using UnityEngine;
using UnityEngine.XR;

namespace Locomotion.Scripts
{
    public class FingerMovement : MonoBehaviour
    {
        public XRNode node = XRNode.LeftHand;
        private InputDevice _inputDevice;
        
        public KinematicHand hand;
        public KinematicFinger[] fingers;
        private readonly Dictionary<object, float> _maxBendLimits = new Dictionary<object, float>();

        public float indexValue;
        public float middleValue;
        public float thumbValue;

        private void Update()
        {
            if (!_inputDevice.isValid) 
                _inputDevice = InputDevices.GetDeviceAtXRNode(node);
            else
            {
                _inputDevice.TryGetFeatureValue(CommonUsages.trigger, out indexValue);
                _inputDevice.TryGetFeatureValue(CommonUsages.grip, out middleValue);
                _inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out var btn);
                thumbValue = btn ? 1 : 0;
                
                hand.SetFingerBend(fingers[1], indexValue);
                hand.SetFingerBend(fingers[0], middleValue);
                hand.SetFingerBend(fingers[2], thumbValue);
            }
            
        }
    }
}
