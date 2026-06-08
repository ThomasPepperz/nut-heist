using System.Collections;
using UnityEngine;

namespace NutHeist.Core
{
    public sealed class GameLoop : MonoBehaviour
    {
        public enum GameState { Idle, Playing, Won, Caught }

        public static GameLoop Instance { get; private set; }

        [SerializeField] float respawnDelay = 2f;

        GameState state = GameState.Idle;
        Vector3 checkpointPos;
        Quaternion checkpointRot;

        public GameState State => state;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Call once when the level is ready to begin (sets the initial respawn point).
        public void StartGame(Vector3 spawnPos, Quaternion spawnRot)
        {
            checkpointPos = spawnPos;
            checkpointRot = spawnRot;
            state = GameState.Playing;
        }

        public void RegisterCheckpoint(Vector3 position, Quaternion rotation)
        {
            if (state != GameState.Playing) return;
            checkpointPos = position;
            checkpointRot = rotation;
        }

        public void NotifyExitReached()
        {
            if (state != GameState.Playing) return;
            state = GameState.Won;
            NutHeist.UI.GameLoopUI.Instance?.ShowWon();
        }

        public void NotifyCaught()
        {
            if (state != GameState.Playing) return;
            state = GameState.Caught;
            NutHeist.UI.GameLoopUI.Instance?.ShowCaught();
            StartCoroutine(CatchSequence());
        }

        IEnumerator CatchSequence()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        void Respawn()
        {
            NutHeist.Player.SquirrelController squirrel =
                Object.FindFirstObjectByType<NutHeist.Player.SquirrelController>();
            if (squirrel)
            {
                CharacterController cc = squirrel.GetComponent<CharacterController>();
                cc.enabled = false;
                squirrel.transform.SetPositionAndRotation(checkpointPos, checkpointRot);
                cc.enabled = true;
            }
            state = GameState.Playing;
            NutHeist.UI.GameLoopUI.Instance?.HideAll();
        }

        public void Restart()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
