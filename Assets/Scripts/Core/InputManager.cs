using System;
using UnityEngine;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Manages all player input in the game
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Singleton
        private static InputManager _instance;
        
        /// <summary>
        /// Singleton instance of InputManager
        /// </summary>
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion
        
        #region Input Events
        // Movement
        public event Action<Vector2> OnMove;
        public event Action OnSprint;
        public event Action OnSprintReleased;
        
        // Combat
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnReload;
        public event Action OnSwitchFireMode;
        public event Action OnSwitchWeapon;
        
        // Interaction
        public event Action OnInteract;
        
        // UI
        public event Action OnInventoryToggle;
        public event Action OnPause;
        #endregion
        
        #region Input State
        /// <summary>
        /// Current movement input direction
        /// </summary>
        public Vector2 MovementDirection { get; private set; }
        
        /// <summary>
        /// Current mouse position in screen coordinates
        /// </summary>
        public Vector2 MousePosition { get; private set; }
        
        /// <summary>
        /// Whether fire button is currently pressed
        /// </summary>
        public bool IsFirePressed { get; private set; }
        
        /// <summary>
        /// Whether sprint button is currently pressed
        /// </summary>
        public bool IsSprintPressed { get; private set; }
        #endregion
        
        #region Input Handling
        private void Update()
        {
            // Get keyboard input for movement using legacy input system
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            Vector2 newMovementDirection = new Vector2(horizontal, vertical).normalized;
            
            // Only trigger event if movement changed
            if (newMovementDirection != MovementDirection)
            {
                MovementDirection = newMovementDirection;
                OnMove?.Invoke(MovementDirection);
            }
            
            // Get mouse position
            MousePosition = Input.mousePosition;
            
            // Handle sprint input
            if (Input.GetKeyDown(KeyCode.Space))
            {
                IsSprintPressed = true;
                OnSprint?.Invoke();
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                IsSprintPressed = false;
                OnSprintReleased?.Invoke();
            }
            
            // Handle fire input
            if (Input.GetMouseButtonDown(0))
            {
                IsFirePressed = true;
                OnFirePressed?.Invoke();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                IsFirePressed = false;
                OnFireReleased?.Invoke();
            }
            
            // Handle other inputs
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnReload?.Invoke();
            }
            
            if (Input.GetMouseButtonDown(1))
            {
                OnSwitchFireMode?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnInteract?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                OnInventoryToggle?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPause?.Invoke();
            }
            
            // Handle weapon switching
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                OnSwitchWeapon?.Invoke();
            }
        }
        
        /// <summary>
        /// Get the current world position of the mouse
        /// </summary>
        /// <param name="camera">Camera to use for raycasting</param>
        /// <returns>World position of the mouse cursor</returns>
        public Vector3 GetMouseWorldPosition(Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(MousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
        #endregion
    }
}