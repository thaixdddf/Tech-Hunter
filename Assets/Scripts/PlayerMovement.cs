using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;

        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (canMove)
        {
            rotationX += -GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    // Input abstraction to support both the new Input System and the legacy Input Manager
    private bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        if (kb == null) return false;
        switch (key)
        {
            case KeyCode.LeftShift: return kb.leftShiftKey.isPressed;
            case KeyCode.R: return kb.rKey.isPressed;
            default: return false;
        }
#else
        return Input.GetKey(key);
#endif
    }

    private bool GetButton(string name)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        if (name == "Jump")
        {
            bool kbJump = kb != null && kb.spaceKey.isPressed;
            bool gpJump = gp != null && gp.buttonSouth.isPressed;
            return kbJump || gpJump;
        }
        return false;
#else
        return Input.GetButton(name);
#endif
    }

    private float GetAxis(string name)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        var mouse = Mouse.current;
        float value = 0f;
        if (name == "Vertical")
        {
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) value += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) value -= 1f;
            }
            if (gp != null) value += gp.leftStick.y.ReadValue();
        }
        else if (name == "Horizontal")
        {
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) value += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) value -= 1f;
            }
            if (gp != null) value += gp.leftStick.x.ReadValue();
        }
        else if (name == "Mouse X")
        {
            if (mouse != null) value = mouse.delta.ReadValue().x * 0.1f;
        }
        else if (name == "Mouse Y")
        {
            if (mouse != null) value = mouse.delta.ReadValue().y * 0.1f;
        }
        return Mathf.Clamp(value, -1f, 1f);
#else
        return Input.GetAxis(name);
#endif
    }
}