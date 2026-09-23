using UnityEngine;
using EmberKnight.Rooms;

namespace EmberKnight.Core
{
    public enum GameState { Menu, Playing, RoomClear, GameOver, Victory }

    /// <summary>
    /// Máquina de estados de alto nível (equivalente ao `gameState` do
    /// original: menu / playing / gameover / victory). Ligue os painéis
    /// de UI (tela de início, game over, vitória) aos GameObjects abaixo
    /// pelo Inspector, ou escute os eventos públicos.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;

        [Header("Painéis de UI (opcional, arraste aqui)")]
        public GameObject menuPanel;
        public GameObject gameOverPanel;
        public GameObject victoryPanel;

        public System.Action<GameState> OnStateChanged;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            var rm = RoomManager.Instance;
            if (rm != null)
            {
                rm.OnAllRoomsCleared += HandleVictory;
            }
            SetState(GameState.Menu);
        }

        public void StartGame()
        {
            SetState(GameState.Playing);
            RoomManager.Instance?.LoadRoom(0);
        }

        public void PlayerDied()
        {
            SetState(GameState.GameOver);
        }

        void HandleVictory()
        {
            SetState(GameState.Victory);
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        void SetState(GameState newState)
        {
            State = newState;

            if (menuPanel != null) menuPanel.SetActive(newState == GameState.Menu);
            if (gameOverPanel != null) gameOverPanel.SetActive(newState == GameState.GameOver);
            if (victoryPanel != null) victoryPanel.SetActive(newState == GameState.Victory);

            OnStateChanged?.Invoke(newState);
        }
    }
}
