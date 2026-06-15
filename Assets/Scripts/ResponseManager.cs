using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Alias pour lever l'ambiguïté entre UnityEngine.XR.InputDevice et UnityEngine.InputSystem.InputDevice
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using InputDevices = UnityEngine.XR.InputDevices;
using InputDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;

public class ResponseManager : MonoBehaviour
{
    private string currentResponse = "none";
    private float responseTime = 0f;
    private bool hasResponded = false;

    private XRInputDevice rightController;
    private XRInputDevice leftController;

    private const float TRIGGER_THRESHOLD = 0.5f;

    private float prevRightTrigger = 0f;
    private float prevLeftTrigger = 0f;
    private bool prevConfirmButton = false;
    private bool confirmPressed = false;

    void Start() { TryGetControllers(); }

    void Update()
    {
        if (!rightController.isValid || !leftController.isValid)
            TryGetControllers();
        ReadConfirmButton();
        if (!hasResponded) ReadInputs();
    }

    private void TryGetControllers()
    {
        var rightDevices = new List<XRInputDevice>();
        var leftDevices = new List<XRInputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
        if (rightDevices.Count > 0) rightController = rightDevices[0];
        if (leftDevices.Count > 0) leftController = leftDevices[0];
    }

    private void ReadInputs()
    {
        float rightTrigger = 0f;
        rightController.TryGetFeatureValue(XRCommonUsages.trigger, out rightTrigger);
        if (rightTrigger >= TRIGGER_THRESHOLD && prevRightTrigger < TRIGGER_THRESHOLD)
            RegisterResponse("right");
        prevRightTrigger = rightTrigger;

        float leftTrigger = 0f;
        leftController.TryGetFeatureValue(XRCommonUsages.trigger, out leftTrigger);
        if (leftTrigger >= TRIGGER_THRESHOLD && prevLeftTrigger < TRIGGER_THRESHOLD)
            RegisterResponse("left");
        prevLeftTrigger = leftTrigger;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.wasPressedThisFrame) RegisterResponse("right");
            if (keyboard.leftArrowKey.wasPressedThisFrame) RegisterResponse("left");
        }
    }

    private void ReadConfirmButton()
    {
        bool rightB = false;
        bool leftY = false;
        rightController.TryGetFeatureValue(XRCommonUsages.secondaryButton, out rightB);
        leftController.TryGetFeatureValue(XRCommonUsages.secondaryButton, out leftY);

        bool spaceDown = false;
        var keyboard = Keyboard.current;
        if (keyboard != null) spaceDown = keyboard.spaceKey.wasPressedThisFrame;

        bool anyConfirm = rightB || leftY || spaceDown;
        if (anyConfirm && !prevConfirmButton) confirmPressed = true;
        prevConfirmButton = anyConfirm;
    }

    private void RegisterResponse(string side)
    {
        if (hasResponded) return;
        currentResponse = side;
        responseTime = Time.time;
        hasResponded = true;
    }

    public void ResetResponse()
    {
        currentResponse = "none";
        responseTime = 0f;
        hasResponded = false;
    }

    public string GetResponse() => currentResponse;
    public float GetRT() => responseTime;
    public bool HasResponded() => hasResponded;

    public bool SpacePressed()
    {
        if (confirmPressed) { confirmPressed = false; return true; }
        return false;
    }

    public void ClearConfirm()
    {
        confirmPressed = false;
    }
}