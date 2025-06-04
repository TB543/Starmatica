using UnityEngine;
using UnityEngine.InputSystem;

/*
 * CameraMovement script handles the movement and rotation of the camera
 * using Unity's new Input System.
 * It allows for smooth movement and looking around in a 3D space.
 */
public class CameraMovement : MonoBehaviour
{
    // fields set in unity editor
    [SerializeField] float moveSpeed;
    [SerializeField] float lookSpeed;

    // defines the fields for the camera movement script
    private InputAction move;
    private InputAction look;
    private float yaw = 0;
    private float pitch = 0;

    /*
     * Awake is called when the script instance is being loaded.
     * It initializes the InputActions for movement and looking.
     */
    private void Awake()
    {
        move = InputSystem.actions.FindAction("Move");
        look = InputSystem.actions.FindAction("Look");
    }

    /*
     * Start is called before the first frame update.
     * It enables the InputActions for movement and looking.
     */
    private void Update()
    {
        // handles movement actions
        if (move.IsPressed()) {
            Vector3 moveDirection = move.ReadValue<Vector3>();
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
        }

        // handles look actions
        if (look.IsPressed()) {
            Vector2 lookInput = look.ReadValue<Vector2>();
            yaw += lookInput.x * lookSpeed * Time.deltaTime;
            pitch -= lookInput.y * lookSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -90, 90);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }
}
