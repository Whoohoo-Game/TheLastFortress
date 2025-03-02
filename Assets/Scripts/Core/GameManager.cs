using System;
using System.Collections;
using UnityEngine;
using ZombieSurvival.Logic.Enemies;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Main game manager class that controls the game flow and systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        
        /// <summary>
        /// Singleton instance of the GameManager
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
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
            
            InitializeGame();
        }
        #endregion
        
        #region Game State
        /// <summary>
        /// Possible game states
        /// </summary>
        public enum GameState
        {
            MainMenu,
            Loading,
            Playing,
            Paused,
            GameOver,
            Victory
        }
        
        /// <summary>
        /// Current game state
        /// </summary>
        public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
        
        /// <summary>
        /// Event fired when game state changes
        /// </summary>
        public event Action<GameState> OnGameStateChanged;
        
        /// <summary>
        /// Change the current game state
        /// </summary>
        /// <param name="newState">The new game state</param>
        public void ChangeState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            
            GameState previousState = CurrentGameState;
            CurrentGameState = newState;
            
            // Handle state specific logic
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Loading:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
                case GameState.Victory:
                    Time.timeScale = 0f;
                    break;
            }
            
            // Notify subscribers of state change
            OnGameStateChanged?.Invoke(newState);
            
            Debug.Log($"Game state changed from {previousState} to {newState}");
        }
        #endregion
        
        #region Game Systems
        [Header("Game Systems")]
        [SerializeField] private ZombieWaveManager zombieWaveManager;
        [SerializeField] private float initialDelay = 5f;
        
        [Header("Game Settings")]
        [SerializeField] private int targetFrameRate = 60;
        
        private bool _gameInitialized = false;
        
        /// <summary>
        /// Initialize the game systems
        /// </summary>
        private void InitializeGame()
        {
            if (_gameInitialized) return;
            
            // Set application target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Initialize systems
            AddressableManager.Instance.Initialize();
            
            _gameInitialized = true;
            Debug.Log("Game initialized");
        }
        
        /// <summary>
        /// Start the gameplay
        /// </summary>
        public void StartGame()
        {
            if (!_gameInitialized)
            {
                InitializeGame();
            }
            
            ChangeState(GameState.Playing);
            StartCoroutine(StartGameSequence());
        }
        
        /// <summary>
        /// Game start sequence with initial delay
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator StartGameSequence()
        {
            // Wait for initial delay
            yield return new WaitForSeconds(initialDelay);
            
            // Start zombie waves if assigned
            if (zombieWaveManager != null)
            {
                zombieWaveManager.StartWaves();
            }
            else
            {
                Debug.LogWarning("ZombieWaveManager not assigned to GameManager");
            }
        }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (CurrentGameState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }
        
        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentGameState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }
        
        /// <summary>
        /// End the game with game over state
        /// </summary>
        public void GameOver()
        {
            ChangeState(GameState.GameOver);
        }
        
        /// <summary>
        /// End the game with victory state
        /// </summary>
        public void Victory()
        {
            ChangeState(GameState.Victory);
        }
        #endregion
    }
}