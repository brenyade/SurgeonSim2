using TraumaSurgeon.Core;
using TraumaSurgeon.Tools;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Player
{
    /// <summary>
    /// Assembles the first person surgeon rig at runtime: character controller, camera,
    /// hand anchor, tool controller and interactor. Returned as a small handle struct so callers
    /// do not have to go hunting for the components.
    /// </summary>
    public static class PlayerRigBuilder
    {
        public class Rig
        {
            public GameObject Root;
            public Camera Camera;
            public FirstPersonController Movement;
            public HandController Hand;
            public ToolController Tools;
            public PlayerInteractor Interactor;
            public Transform HandAnchor;
            public CameraFocus Focus;
        }

        public static Rig Build(Vector3 position, float yaw)
        {
            var root = new GameObject("Surgeon");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.88f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.35f;

            // Camera pivot at eye height.
            GameObject pivot = PrimitiveFactory.Empty("CameraPivot", root.transform, new Vector3(0f, 1.62f, 0f));

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(pivot.transform, false);
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;
            camera.fieldOfView = SettingsManager.Exists ? SettingsManager.Instance.Data.fieldOfView : 72f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";

            var movement = root.AddComponent<FirstPersonController>();
            movement.CameraPivot = pivot.transform;

            // Hand anchor sits in front of and below the camera, like a held instrument.
            GameObject handAnchor = PrimitiveFactory.Empty("HandAnchor", cameraGo.transform,
                new Vector3(0.16f, -0.19f, 0.28f));
            handAnchor.transform.localRotation = Quaternion.Euler(28f, -8f, 0f);

            var tools = root.AddComponent<ToolController>();
            tools.HandAnchor = handAnchor.transform;

            var hand = cameraGo.AddComponent<HandController>();
            hand.HandAnchor = handAnchor.transform;
            hand.PlayerCamera = camera;
            hand.Movement = movement;
            hand.Tools = tools;

            var interactor = root.AddComponent<PlayerInteractor>();
            interactor.PlayerCamera = camera;

            var focus = cameraGo.AddComponent<CameraFocus>();
            focus.TargetCamera = camera;

            return new Rig
            {
                Root = root,
                Camera = camera,
                Movement = movement,
                Hand = hand,
                Tools = tools,
                Interactor = interactor,
                HandAnchor = handAnchor.transform,
                Focus = focus
            };
        }
    }
}
