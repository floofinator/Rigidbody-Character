using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Floofinator.RigidbodyCharacter.Samples
{
    public class CharacterPOV : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField] float mouseSensitivity = 0.5f;
        Vector3 angles = Vector3.zero;
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        private void Update()
        {
            pivot.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

            angles.x = Mathf.Clamp(angles.x - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -80, 80);
            transform.localEulerAngles = angles;
        }
    }
}
