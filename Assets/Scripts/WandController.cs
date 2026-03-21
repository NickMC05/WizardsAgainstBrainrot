using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class WandController : MonoBehaviour
{
    [Header("Hand Attachment")]
    [Tooltip("Drag the Right Controller from XR Origin > Camera Offset > Right Controller")]
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 0.05f);
    [SerializeField] private Vector3 rotationOffset = new Vector3(45f, 0f, 0f);

    [Header("References")]
    [SerializeField] private SpellManager spellManager;
    [SerializeField] private WandTipDetector wandTipDetector;

    [Header("Input")]
    [SerializeField] private float triggerThreshold = 0.7f;

    private InputDevice rightHandDevice;
    private bool prevTriggerState;

    void Start()
    {
        // Parent the wand to the right-hand controller transform
        if (rightHandAnchor != null)
        {
            transform.SetParent(rightHandAnchor);
            transform.localPosition = positionOffset;
            transform.localRotation = Quaternion.Euler(rotationOffset);
        }
        else
        {
            Debug.LogWarning("[WandController] rightHandAnchor is not assigned. " +
                             "Drag the Right Controller transform into this field.");
        }
    }

    void Update()
    {
        // Lazily acquire the right-hand InputDevice
        if (!rightHandDevice.isValid)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right |
                InputDeviceCharacteristics.Controller |
                InputDeviceCharacteristics.HeldInHand,
                devices);

            if (devices.Count > 0)
                rightHandDevice = devices[0];
            else
                return;
        }

        rightHandDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        bool triggerDown = triggerValue > triggerThreshold;

        if (triggerDown && !prevTriggerState)
        {
            spellManager?.StartCasting();
            // Pick up any collider the wand tip is already inside of
            wandTipDetector?.CheckInitialOverlaps();
        }
        else if (!triggerDown && prevTriggerState)
        {
            spellManager?.FinishCasting();
        }

        prevTriggerState = triggerDown;
    }

    /// <summary>
    /// Fire a short haptic impulse on the right controller.
    /// </summary>
    public void SendHapticPulse(float amplitude = 0.3f, float duration = 0.1f)
    {
        if (rightHandDevice.isValid)
            rightHandDevice.SendHapticImpulse(0, amplitude, duration);
    }
}