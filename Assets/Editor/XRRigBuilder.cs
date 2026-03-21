#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;

public static class XRRigBuilder
{
    [MenuItem("GameObject/XR/Minimal XR Origin (Quest 3)", false, 10)]
    static void Build()
    {
        // Warn if there is already an unparented Main Camera
        Camera existingMain = Camera.main;
        if (existingMain != null && existingMain.transform.parent == null)
        {
            bool delete = EditorUtility.DisplayDialog(
                "Existing Main Camera Found",
                "There is already a root-level Main Camera in the scene. " +
                "It should be deleted so the XR Origin camera takes over.\n\n" +
                "Delete the existing Main Camera now?",
                "Delete", "Keep");

            if (delete)
            {
                Undo.DestroyObjectImmediate(existingMain.gameObject);
                Debug.Log("[XRRigBuilder] Deleted existing Main Camera.");
            }
        }

        // ── XR Origin root ──────────────────────────────────────────────
        GameObject root = new GameObject("XR Origin");
        Undo.RegisterCreatedObjectUndo(root, "Create XR Origin");
        XROrigin origin = root.AddComponent<XROrigin>();

        // ── Camera Offset ───────────────────────────────────────────────
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(root.transform, false);

        origin.CameraFloorOffsetObject = cameraOffset;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

        // ── Main Camera (head) ──────────────────────────────────────────
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(cameraOffset.transform, false);

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;
        camObj.AddComponent<AudioListener>();
        origin.Camera = cam;

        TrackedPoseDriver headTPD = camObj.AddComponent<TrackedPoseDriver>();
        headTPD.positionInput = MakeAction("HMD Position", "<XRHMD>/centerEyePosition");
        headTPD.rotationInput = MakeAction("HMD Rotation", "<XRHMD>/centerEyeRotation");
        headTPD.trackingStateInput = MakeAction("HMD TrackingState", "<XRHMD>/trackingState");
        EditorUtility.SetDirty(headTPD);

        // ── Left Controller ─────────────────────────────────────────────
        GameObject leftCtrl = new GameObject("Left Controller");
        leftCtrl.transform.SetParent(cameraOffset.transform, false);

        TrackedPoseDriver leftTPD = leftCtrl.AddComponent<TrackedPoseDriver>();
        leftTPD.positionInput = MakeAction("Left Position", "<XRController>{LeftHand}/devicePosition");
        leftTPD.rotationInput = MakeAction("Left Rotation", "<XRController>{LeftHand}/deviceRotation");
        leftTPD.trackingStateInput = MakeAction("Left TrackingState", "<XRController>{LeftHand}/trackingState");
        EditorUtility.SetDirty(leftTPD);

        // ── Right Controller ────────────────────────────────────────────
        GameObject rightCtrl = new GameObject("Right Controller");
        rightCtrl.transform.SetParent(cameraOffset.transform, false);

        TrackedPoseDriver rightTPD = rightCtrl.AddComponent<TrackedPoseDriver>();
        rightTPD.positionInput = MakeAction("Right Position", "<XRController>{RightHand}/devicePosition");
        rightTPD.rotationInput = MakeAction("Right Rotation", "<XRController>{RightHand}/deviceRotation");
        rightTPD.trackingStateInput = MakeAction("Right TrackingState", "<XRController>{RightHand}/trackingState");
        EditorUtility.SetDirty(rightTPD);

        // ── Finish ──────────────────────────────────────────────────────
        EditorUtility.SetDirty(origin);
        Selection.activeGameObject = root;

        Debug.Log(
            "[XRRigBuilder] XR Origin created with hierarchy:\n" +
            "  XR Origin\n" +
            "    └ Camera Offset\n" +
            "        ├ Main Camera      (TrackedPoseDriver → HMD)\n" +
            "        ├ Left Controller   (TrackedPoseDriver → Left Hand)\n" +
            "        └ Right Controller  (TrackedPoseDriver → Right Hand)");
    }

    static InputActionProperty MakeAction(string name, string binding)
    {
        return new InputActionProperty(
            new InputAction(name, InputActionType.Value, binding));
    }
}
#endif