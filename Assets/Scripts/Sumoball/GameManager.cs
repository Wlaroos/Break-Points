using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Sumoball
{
    public class GameManager : MonoBehaviour
    {
        // Singleton
        public static GameManager Instance { get; private set; }

        // Shared combatant settings moved from Combatant
        [Header("Shared Combatant Settings")]
        [SerializeField] private float _pushSpeed = 4f; // speed of being pushed
        public float PushSpeed => _pushSpeed;

        [SerializeField] private float _lateralSeparation = 0.6f;
        public float LateralSeparation => _lateralSeparation;

        [Header("Round Feedback")]
        [SerializeField] private float _feedbackDuration = 0.25f;
        public float FeedbackDuration => _feedbackDuration;
        [SerializeField] private float _feedbackNudge = 0.22f;
        public float FeedbackNudge => _feedbackNudge;
        [SerializeField] private float _feedbackScale = 1.12f;
        public float FeedbackScale => _feedbackScale;

        [Header("Feedback Tints")]
        [SerializeField] private Color _winTint = Color.white;
        public Color WinTint => _winTint;
        [SerializeField] private Color _loseTint = new Color(0.8f, 0.6f, 0.6f);
        public Color LoseTint => _loseTint;
        [SerializeField] private Color _neutralTint = Color.white;
        public Color NeutralTint => _neutralTint;

        [Header("Combatants")]
        [SerializeField] private Combatant _leftCombatant;
        [SerializeField] private Combatant _rightCombatant;
        [SerializeField] private PlayerAI _leftAI;
        [SerializeField] private PlayerAI _rightAI;
        [SerializeField] private GameUI _ui;

        [Header("Board")]
        [SerializeField] private Board _board; // Board component computes positions

        [Header("Rounds")]
        [Min(1), SerializeField] private int _bestOf = 3; // best of N matches (will be forced odd)
        [SerializeField] private float _roundDelay = 1f;
        [SerializeField] private int _countdownStart = 3;
        [SerializeField] private float _gameSpeed = 1f; // multiplier for Time.timeScale

        [SerializeField] private GameObject _damageParticlePrefab;
        
        private bool _gameRunning = false;
        private int _boardIndex = 0; // center index set at Start

        // Per-RPS-round score (accumulates during a match; used for display and optional logic)
        private int _roundScoreLeft = 0;
        private int _roundScoreRight = 0;

        // Match score (how many matches each combatant has won; bestOf applies to this)
        private int _matchWinsLeft = 0;
        private int _matchWinsRight = 0;

        private int _winsNeeded = 1;

        void Awake()
        {
            // setup singleton early so Combatants can read settings in Start()
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Expose board/player AI info for other components (Combatant uses this)
        public int BoardColumns => (_board != null && _board.Positions != null) ? _board.Positions.Length : 0;
        public PlayerAI LeftAI => _leftAI;
        public PlayerAI RightAI => _rightAI;

        // Make Start a coroutine so we can wait for the initial positioning coroutines to finish
        IEnumerator Start()
        {
            // Basic validation
            if (!_leftCombatant || !_rightCombatant || !_leftAI || !_rightAI || _board == null)
            {
                Debug.LogError("Assign Combatants, AIs and Board on the GameManager in the inspector.");
                enabled = false;
                yield break;
            }

            // Ensure odd bestOf (applies to matches)
            if (_bestOf < 1) _bestOf = 1;
            if (_bestOf % 2 == 0) _bestOf += 1;
            _winsNeeded = _bestOf / 2 + 1;

            // Let the Board compute positions based on fighter transforms
            _board.Setup(_leftCombatant.transform, _rightCombatant.transform);

            // Get computed columns from Board
            Vector3[] columns = _board.Positions;
            if (columns == null || columns.Length < 3)
            {
                Debug.LogError("Board did not produce a valid positions array.");
                enabled = false;
                yield break;
            }

            // Start both in center (ask Board for center index)
            int centerIndex = _board.CenterIndex;
            _boardIndex = centerIndex;

            // Compute and send initial positions (use coroutines only)
            Vector3 basePos = columns[centerIndex];
            StartCoroutine(_leftCombatant.MoveToPositionCoroutine(basePos, centerIndex));
            StartCoroutine(_rightCombatant.MoveToPositionCoroutine(basePos, centerIndex));

            // Wait a short time (or until both IsMoving are false) so initial poses settle before gameplay
            float initTimeout = 2f;
            float it = 0f;
            while ((_leftCombatant.IsMoving || _rightCombatant.IsMoving) && it < initTimeout)
            {
                it += Time.deltaTime;
                yield return null;
            }

            // Reset scores
            _roundScoreLeft = 0;
            _roundScoreRight = 0;
            _matchWinsLeft = 0;
            _matchWinsRight = 0;

            _gameRunning = true;

            // Set game speed
            Time.timeScale = _gameSpeed;

            StartCoroutine(GameLoop());
        }

        private void Update()
        {
            // Debug reset game with R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        IEnumerator GameLoop()
        {
            while (_gameRunning)
            {
                // Show current scores each loop
                _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);
                // Update edge visits + current distribution names in the UI
                string leftDist = _leftAI != null ? _leftAI.GetCurrentDistributionName() : "";
                string rightDist = _rightAI != null ? _rightAI.GetCurrentDistributionName() : "";
                int leftVisits = _leftCombatant != null ? _leftCombatant.EdgeVisits : 0;
                int rightVisits = _rightCombatant != null ? _rightCombatant.EdgeVisits : 0;
                _ui?.ShowEdgeInfo(leftVisits, leftDist, rightVisits, rightDist);

                // Countdown
                for (int i = _countdownStart; i >= 0; i--)
                {
                    _ui?.ShowCountdown(i);
                    yield return new WaitForSeconds(1f);
                }

                // Pick moves
                RPSMove leftMove = _leftAI.PickMove();
                RPSMove rightMove = _rightAI.PickMove();
                _ui?.ShowMoves(leftMove, rightMove);
                // Update combatant sprites to reflect the chosen moves
                _leftCombatant?.SetMoveSprite(leftMove);
                _rightCombatant?.SetMoveSprite(rightMove);

                // Super move => immediate match win (not just a single push)
                if (leftMove == RPSMove.Super && rightMove != RPSMove.Super)
                {
                    _ui?.ShowStatus($"{_leftCombatant.CombatantName} used SUPER!");

                    // Push the right combatant to the right-most wall (visual knockout like normal edge push)
                    int lastIdx = _board.Positions.Length - 1;
                    _boardIndex = lastIdx;
                    Vector3 rightWallBase = _board.Positions[_boardIndex];
                    // Move right to wall, left remains one column inward for the visual
                    StartCoroutine(_rightCombatant.MoveToPositionCoroutine(rightWallBase, _boardIndex));
                    int leftStayIdx = Mathf.Max(0, lastIdx - 1);
                    Vector3 leftStayBase = _board.Positions[leftStayIdx];
                    StartCoroutine(_leftCombatant.MoveToPositionCoroutine(leftStayBase, leftStayIdx));

                    // wait for movement (or timeout)
                    yield return StartCoroutine(WaitForMovement(_roundDelay + 2f));
 
                    // Treat as knockout: right loses the match
                    _ui?.ShowStatus($"{_rightCombatant.CombatantName} knocked out by SUPER. {_leftCombatant.CombatantName} wins match!");
                    _matchWinsLeft++;
                    SpawnDamageParticles(_rightCombatant);

                    // Update match display and check series win
                    _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);
                    if (_matchWinsLeft >= _winsNeeded)
                    {
                        _ui?.ShowStatus($"{_leftCombatant.CombatantName} wins series {_matchWinsLeft}-{_matchWinsRight}");
                        _gameRunning = false;
                        break;
                    }

                    // reset for next match
                    yield return StartCoroutine(ResetMatchToCenterCoroutine());
                    continue;
                }

                if (rightMove == RPSMove.Super && leftMove != RPSMove.Super)
                {
                    _ui?.ShowStatus($"{_rightCombatant.CombatantName} used SUPER!");

                    // Push the left combatant to the left-most wall (visual knockout like normal edge push)
                    int firstIdx = 0;
                    _boardIndex = firstIdx;
                    Vector3 leftWallBase = _board.Positions[_boardIndex];
                    // Move left to wall, right remains one column inward for the visual
                    StartCoroutine(_leftCombatant.MoveToPositionCoroutine(leftWallBase, _boardIndex));
                    int rightStayIdx = Mathf.Min(_board.Positions.Length - 1, 1);
                    Vector3 rightStayBase = _board.Positions[rightStayIdx];
                    StartCoroutine(_rightCombatant.MoveToPositionCoroutine(rightStayBase, rightStayIdx));

                    // wait for movement (or timeout)
                    yield return StartCoroutine(WaitForMovement(_roundDelay + 2f));
 
                    // Treat as knockout: left loses the match
                    _ui?.ShowStatus($"{_leftCombatant.CombatantName} knocked out by SUPER. {_rightCombatant.CombatantName} wins match!");
                    _matchWinsRight++;
                    SpawnDamageParticles(_leftCombatant);

                    // Update match display and check series win
                    _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);
                    if (_matchWinsRight >= _winsNeeded)
                    {
                        _ui?.ShowStatus($"{_rightCombatant.CombatantName} wins series {_matchWinsRight}-{_matchWinsLeft}");
                        _gameRunning = false;
                        break;
                    }

                    // reset for next match
                    yield return StartCoroutine(ResetMatchToCenterCoroutine());
                    continue;
                }

                // Resolve single RPS round (affects board and round score)
                int result = 0; // 1 left wins, -1 right wins, 0 tie
                if (leftMove == rightMove)
                {
                    result = 0;
                    _ui?.ShowStatus("Tie");
                    // no board move on tie
                }
                else if (leftMove.Beats(rightMove))
                {
                    result = 1;
                    _ui?.ShowStatus($"{_leftCombatant.CombatantName} wins round");
                    _roundScoreLeft++;
                    _boardIndex = Mathf.Min(_boardIndex + 1, _board.Positions.Length - 1);
                }
                else
                {
                    result = -1;
                    _ui?.ShowStatus($"{_rightCombatant.CombatantName} wins round");
                    _roundScoreRight++;
                    _boardIndex = Mathf.Max(_boardIndex - 1, 0);
                }

                // Visual feedback: highlight UI and nudge/scale combatants
                _ui?.HighlightRoundResult(result);
                if (result == 1)
                {
                    _leftCombatant.PlayRoundFeedback(true);
                    _rightCombatant.PlayRoundFeedback(false);
                    SpawnDamageParticles(_rightCombatant);
                }
                else if (result == -1)
                {
                    _leftCombatant.PlayRoundFeedback(false);
                    _rightCombatant.PlayRoundFeedback(true);
                    SpawnDamageParticles(_leftCombatant);
                }
                else
                {
                    // tie: small neutral feedback for both (use false to give slight recoil)
                    _leftCombatant.PlayRoundFeedback(false);
                    _rightCombatant.PlayRoundFeedback(false);
                }

                // Update score display (round + match)
                _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);

                // Move both combatants to the new board positions
                Vector3 basePos = _board.Positions[_boardIndex];
                int lastIndexLocal = _board.Positions.Length - 1;
                // If we've hit an edge, only move the loser to the wall column.
                if (_boardIndex == lastIndexLocal)
                {
                    // right-most wall reached -> right is loser, move right to wall, left stays one column inward
                    int leftIndex = Mathf.Max(0, lastIndexLocal - 1);
                    StartCoroutine(_rightCombatant.MoveToPositionCoroutine(basePos, _boardIndex));
                }
                else if (_boardIndex == 0)
                {
                    // left-most wall reached -> left is loser, move left to wall, right stays one column inward
                    int rightIndex = Mathf.Min(lastIndexLocal, 1);
                    StartCoroutine(_leftCombatant.MoveToPositionCoroutine(basePos, _boardIndex));
                    }
                else
                {
                    // normal case: move both to the same column
                    StartCoroutine(_leftCombatant.MoveToPositionCoroutine(basePos, _boardIndex));
                    StartCoroutine(_rightCombatant.MoveToPositionCoroutine(basePos, _boardIndex));
                }

                // Wait while movements occur (wait until both combatants finish or timeout)
                yield return StartCoroutine(WaitForMovement(_roundDelay + 2f));
 
                // Check knockout by wall using Board column count
                int lastIndex = _board.Positions.Length - 1;
                bool matchEndedByKnockout = false;
                if (_boardIndex == lastIndex)
                {
                    // right-most wall reached -> right knocked out -> left wins match
                    _ui?.ShowStatus($"{_leftCombatant.CombatantName} knocked out. {_rightCombatant.CombatantName} wins match!");
                    _matchWinsLeft++;
                    // reset edge visits for the loser (right)
                    matchEndedByKnockout = true;
                }
                else if (_boardIndex == 0)
                {
                    // left-most wall reached -> left knocked out -> right wins match
                    _ui?.ShowStatus($"{_rightCombatant.CombatantName} knocked out. {_leftCombatant.CombatantName} wins match!");
                    _matchWinsRight++;
                    // reset edge visits for the loser (left)
                    matchEndedByKnockout = true;
                }

                if (matchEndedByKnockout)
                {
                    // Update match display
                    _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);

                    // Check series win (best-of applies to match wins)
                    if (_matchWinsLeft >= _winsNeeded)
                    {
                        _ui?.ShowStatus($"{_leftCombatant.CombatantName} wins series {_matchWinsLeft}-{_matchWinsRight}");
                        _gameRunning = false;
                        break;
                    }
                    if (_matchWinsRight >= _winsNeeded)
                    {
                        _ui?.ShowStatus($"{_rightCombatant.CombatantName} wins series {_matchWinsRight}-{_matchWinsLeft}");
                        _gameRunning = false;
                        break;
                    }

                    // Otherwise prepare next match: reset round scores and board to center
                    yield return StartCoroutine(ResetMatchToCenterCoroutine());
                }

                // If no knockout, matches continue; series only tracked by match wins
                // small delay before next RPS round
                yield return new WaitForSeconds(0.5f);
            }

            // Game over - ensure UI shows final state
            _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);
            _ui?.ShowCountdown(0);
        }

        // Spawn damage particles at the losing combatant's position
        private void SpawnDamageParticles(Combatant loser)
        {
            if (_damageParticlePrefab == null || loser == null) return;
            GameObject go = Instantiate(_damageParticlePrefab, loser.transform.position, Quaternion.identity);
 
            // Try to find a ParticleSystem on the prefab and tint it based on who lost:
            // - left loses -> blue
            // - right loses -> red
            ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                if (loser == _leftCombatant)
                {
                    main.startColor = new Color(79, 222, 236, 255);
                }
                else if (loser == _rightCombatant)
                {
                    main.startColor = new Color(244, 116, 166, 255);
                }
                else
                {
                    main.startColor = Color.white;
                }
            }
        }
 
        // Wait until both combatants stop moving or the given timeout elapses
        private IEnumerator WaitForMovement(float timeout)
        {
            float t = 0f;
            while ((_leftCombatant.IsMoving || _rightCombatant.IsMoving) && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
 
        // Reset round scores, move both combatants back to center and reset edge visits
        private IEnumerator ResetMatchToCenterCoroutine()
        {
            _roundScoreLeft = 0;
            _roundScoreRight = 0;
            int center = _board.CenterIndex;
            _boardIndex = center;
            Vector3 centerBase = _board.Positions[center];
            StartCoroutine(_leftCombatant.MoveToPositionCoroutine(centerBase, center));
            StartCoroutine(_rightCombatant.MoveToPositionCoroutine(centerBase, center));
            yield return StartCoroutine(WaitForMovement(2f));
            _leftCombatant?.ResetEdgeVisits();
            _rightCombatant?.ResetEdgeVisits();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
