using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class waveOverScreenScript : MonoBehaviour
{
    public GameObject EnemyWaveController;

    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    private bool prevButtonState;

    void Start()
    {
    }

    void Update()
    {
        // Lazily acquire left and right hand devices
        if (!leftHandDevice.isValid)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left |
                InputDeviceCharacteristics.Controller |
                InputDeviceCharacteristics.HeldInHand,
                devices);
            if (devices.Count > 0)
                leftHandDevice = devices[0];
        }

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
        }

        // Check for any button press on either controller
        bool buttonPressed = false;

        // Check primary button (A on right, X on left)
        if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimary) && leftPrimary)
            buttonPressed = true;
        if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimary) && rightPrimary)
            buttonPressed = true;

        // Check secondary button (B on right, Y on left)
        if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftSecondary) && leftSecondary)
            buttonPressed = true;
        if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightSecondary) && rightSecondary)
            buttonPressed = true;

        // Detect press (transition from not pressed to pressed)
        if (buttonPressed && !prevButtonState)
        {
            RestartScene();
        }

        prevButtonState = buttonPressed;
    }

    void RestartScene()
    {
        Debug.Log("[waveOverScreenScript] Restarting scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}