using NavMeshPlus.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    [SerializeField] playerController PlayerController;

    [SerializeField] InputActionReference movement, attack, mousePosition, dash;

    private Vector2 pointerInput, MovementInput;

    public Vector2 PointerInput => pointerInput;

    public weaponParent WeaponParent;

    [SerializeField] Animator dashAnimator;

    private void OnEnable()
    {
        attack.action.performed += PerformAttack;
    }
    private void OnDisable()
    {
        attack.action.performed -= PerformAttack;
    }
    private void PerformAttack(InputAction.CallbackContext obj)
    {
        WeaponParent.Attack();
    }

    private void Awake()
    {
        WeaponParent = GetComponentInChildren<weaponParent>();

    }
    private void Update()
    {
        pointerInput = GetPointerInput();

        WeaponParent.pointerPosition = pointerInput;

        MovementInput = movement.action.ReadValue<Vector2>();

        PlayerController.playerInput = MovementInput;

        if (dash.action.triggered)
        {
            StartDash();
        }
    }
    private void StartDash()
    {
        PlayerController.PerformDash();
        dashAnimator.SetTrigger("Dash");
    }
    private Vector2 GetPointerInput()
    {
        Vector3 mousePos = mousePosition.action.ReadValue<Vector2>();
        mousePos.z = Camera.main.nearClipPlane;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
