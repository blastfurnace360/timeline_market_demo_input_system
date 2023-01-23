using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GlobalInput
{
	public class InputManager : MonoBehaviour
	{
		
		private PlayerInput playerInput;
		private InputAction interactAction;
		private InputAction jumpAction;
		private InputAction spawnCollectableAction;
		private InputAction crouchAction;
		private InputAction sprintAction;
		
		[HideInInspector]
		public static InputManager instance = new InputManager();
		[HideInInspector]
		public Vector3 inputDirection;
		[HideInInspector]
		public bool jumpButtonDown = false;
		[HideInInspector]
		public bool interactButtonDown = false;
		[HideInInspector]
		public bool spawnCollectableButtonDown = false;
		[HideInInspector]
		public bool crouchButtonDown = false;
		[HideInInspector]
		public bool sprintButtonDown = false;
		
	    // Start is called before the first frame update
	    void Start()
	    {
		    playerInput = GetComponent<PlayerInput>();
		    interactAction = playerInput.actions["Interact"];
		    jumpAction = playerInput.actions["Jump"];
		    spawnCollectableAction = playerInput.actions["Spawn Collectable"];
		    crouchAction = playerInput.actions["Crouch"];
		    sprintAction = playerInput.actions["Sprint"];

		    interactAction.performed +=
			    ctx => {
				    instance.interactButtonDown = true;
			    };

		    interactAction.canceled +=
			    ctx => {
				    instance.interactButtonDown = false;
			    };

		    jumpAction.performed +=
			    ctx => {
				    instance.jumpButtonDown = true;
			    };

		    jumpAction.canceled +=
			    ctx => {
				    instance.jumpButtonDown = false;
			    };

		    spawnCollectableAction.performed +=
			    ctx => {
				    instance.spawnCollectableButtonDown = true;
			    };

		    spawnCollectableAction.canceled +=
			    ctx => {
				    instance.spawnCollectableButtonDown = false;
			    };

		    crouchAction.performed +=
			    ctx => {
				    instance.crouchButtonDown = true;
			    };

		    crouchAction.canceled +=
			    ctx => {
				    instance.crouchButtonDown = false;
			    };
			    
		    sprintAction.performed +=
			    ctx => {
				    instance.sprintButtonDown = true;
			    };

		    sprintAction.canceled +=
			    ctx => {
				    instance.sprintButtonDown = false;
			    };

		    interactAction.Enable();
		    jumpAction.Enable();
		    spawnCollectableAction.Enable();
		    crouchAction.Enable();
		    sprintAction.Enable();
	    }
	
		public void OnMovement(InputValue value)
		{
			instance.inputDirection = value.Get<Vector2>();
		}
	}
}