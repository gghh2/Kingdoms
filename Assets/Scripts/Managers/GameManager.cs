using UnityEngine;
using UnityEngine.InputSystem;

namespace Kingdoms.Managers
{
    /// <summary>
    /// Core game manager - Singleton pattern
    /// Handles global game state and coordinates other managers
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        
        private static GameManager _instance;
        
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("GameManager not found in scene! Make sure it exists.");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Game State
        
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            Waiting,
            GameOver
        }
        
        private GameState _currentState = GameState.MainMenu;
        
        public GameState CurrentState => _currentState;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton initialization
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Start()
        {
            // Auto-start game for now
            StartGame();
        }
        
        private void Update()
        {
            // Handle pause input (ESC key) using new Input System
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize all game systems
        /// </summary>
        private void Initialize()
        {
            Debug.Log("GameManager: Initializing game systems...");
            
            // Initialize managers
            // WorldManager will initialize itself and generate world
            // TimeManager will initialize itself and start time
            // MusicManager will initialize itself
            
            // Ensure FogManager exists for dynamic fog
            if (FindFirstObjectByType<FogManager>() == null)
            {
                GameObject fogManagerObj = new GameObject("FogManager");
                fogManagerObj.transform.SetParent(transform);
                fogManagerObj.AddComponent<FogManager>();
                Debug.Log("GameManager: Created FogManager");
            }
            
            // Ensure CloudManager exists for animated volumetric clouds
            if (FindFirstObjectByType<CloudManager>() == null)
            {
                GameObject cloudManagerObj = new GameObject("CloudManager");
                cloudManagerObj.transform.SetParent(transform);
                cloudManagerObj.AddComponent<CloudManager>();
                Debug.Log("GameManager: Created CloudManager");
            }
            
            Debug.Log("GameManager: Initialization complete");
        }
        
        #endregion
        
        #region Game Control
        
        /// <summary>
        /// Start the game
        /// </summary>
        public void StartGame()
        {
            Debug.Log("GameManager: Starting game...");
            ChangeState(GameState.Playing);
        }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing) return;
            
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            Debug.Log("GameManager: Game paused");
        }
        
        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;
            
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            Debug.Log("GameManager: Game resumed");
        }
        
        /// <summary>
        /// Toggle between pause and play states
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        /// <summary>
        /// Set waiting state (for wait system)
        /// </summary>
        public void SetWaitingState(bool isWaiting)
        {
            if (isWaiting)
            {
                if (_currentState == GameState.Playing)
                {
                    ChangeState(GameState.Waiting);
                    Debug.Log("GameManager: Entered waiting state");
                }
            }
            else
            {
                if (_currentState == GameState.Waiting)
                {
                    ChangeState(GameState.Playing);
                    Debug.Log("GameManager: Exited waiting state");
                }
            }
        }
        
        /// <summary>
        /// Quit the game
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("GameManager: Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Change game state
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;
            
            GameState oldState = _currentState;
            _currentState = newState;
            
            Debug.Log($"GameManager: State changed from {oldState} to {newState}");
            
            // TODO: Notify listeners about state change
            OnStateChanged(oldState, newState);
        }
        
        /// <summary>
        /// Called when game state changes
        /// Override this in future to add specific behaviors per state
        /// </summary>
        private void OnStateChanged(GameState oldState, GameState newState)
        {
            // Handle state-specific logic here
            switch (newState)
            {
                case GameState.MainMenu:
                    // TODO: Show main menu UI
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                    
                case GameState.Playing:
                    // TODO: Hide menus, enable player controls
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                    
                case GameState.Paused:
                    // TODO: Show pause menu
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                    
                case GameState.Waiting:
                    // Keep cursor visible during wait
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    // Don't pause Unity's time (Time.timeScale stays at 1)
                    break;
                    
                case GameState.GameOver:
                    // TODO: Show game over screen
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }
        
        #endregion
    }
}
