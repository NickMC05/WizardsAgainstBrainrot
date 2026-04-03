using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;


public class waveOverScreenScript : MonoBehaviour
{
    [SerializeField] private EnemyWaveScript enemyWaveController;

    [SerializeField] private TextMeshProUGUI waveOverText;
[SerializeField] private Button waveOverButton;
private bool hasUpdatedForCompletion = false;

    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    private bool prevButtonState;

    void Start()
    {
        if (enemyWaveController == null)
            enemyWaveController = FindObjectOfType<EnemyWaveScript>();
    }

void Update()
{
    // Check if we should update UI for game completion
    if (!hasUpdatedForCompletion && enemyWaveController != null && enemyWaveController.IsGameComplete())
    {
        ShowGameCompletedUI();
        hasUpdatedForCompletion = true;
    }

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
        OnButtonPressed();
    }

    prevButtonState = buttonPressed;
}

void ShowGameCompletedUI()
{
    if (waveOverText != null)
        waveOverText.text = "Game Cleared!";

    if (waveOverButton != null)
    {
        TextMeshProUGUI buttonText = waveOverButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = "Press A to reset";
    }

    Debug.Log("[waveOverScreenScript] Game completion UI updated.");
}

    void OnButtonPressed()
    {
        if (enemyWaveController == null)
            enemyWaveController = FindObjectOfType<EnemyWaveScript>();

        if (enemyWaveController == null)
            return;

        // Check if game is complete (Wave 3 cleared)
        if (enemyWaveController.IsGameComplete())
        {
            RestartScene();
        }
        else
        {
            enemyWaveController.NextWave();
        }
    }

    void RestartScene()
    {
        Debug.Log("[waveOverScreenScript] Restarting scene...");
        
        // Destroy the audio manager singleton to allow it to reinitialize
        BackgroundMusicManager audioMgr = FindObjectOfType<BackgroundMusicManager>();
        if (audioMgr != null)
        {
            Destroy(audioMgr.gameObject);
        }

        // Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}