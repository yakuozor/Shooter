using UnityEngine;

namespace ShooterRange
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RangePlayer : MonoBehaviour
    {
        public Camera viewCamera;
        public float moveSpeed = 4f;
        public float mouseSensitivity = 2f;
        public bool IsControlling => Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
        CharacterController controller;
        float pitch;
        float verticalSpeed;
        ArenaCombatant combatant;
        public bool CanAct => combatant == null || combatant.game != null && combatant.game.IsSimulationRunning && combatant.IsAlive;

        void Awake() { controller = GetComponent<CharacterController>(); combatant = GetComponent<ArenaCombatant>(); }
        void Start() { ReleaseCursor(); }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ReleaseCursor();
            if (combatant != null && combatant.game != null && combatant.game.IsFinished) return;
            if (!IsControlling)
            {
                if (Input.GetMouseButtonDown(0) && Application.isFocused) CaptureCursor();
                return;
            }
            if (!CanAct) return;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -75f, 75f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(0f, Input.GetAxisRaw("Mouse X") * mouseSensitivity, 0f);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")), 1f);
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((transform.TransformDirection(input) * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        public void ResetView() { pitch = 0; verticalSpeed = 0; viewCamera.transform.localRotation = Quaternion.identity; }
        void LateUpdate()
        {
            if (combatant == null || !combatant.IsAlive || !CanAct) return;
            float kick = Mathf.Clamp01(1f - (Time.time - combatant.LastDamageTime) * 6f);
            if (kick > 0) viewCamera.transform.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 90f) * kick * 1.2f, 0, Mathf.Sin(Time.time * 65f) * kick * .8f);
        }

        public void CaptureCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        public void ReleaseCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        void OnDisable() { ReleaseCursor(); }
    }
}
