using System.Collections;
using UnityEngine;

namespace FanExperiencePrototypes
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

        void Start()
        {
            // Basic validation
            if (!_leftCombatant || !_rightCombatant || !_leftAI || !_rightAI)
            {
                Debug.LogError("Assign Combatants and AIs on the GameManager in the inspector.");
                enabled = false;
                return;
            }

            if (_board == null)
            {
                Debug.LogError("Assign a Board component to the GameManager in the inspector.");
                enabled = false;
                return;
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
                return;
            }

            // Start both in center (ask Board for center index)
            int centerIndex = _board.CenterIndex;
            _boardIndex = centerIndex;

            // Compute and send initial positions (GameManager composes board base + combatant lateral offset)
            Vector3 basePos = columns[centerIndex];
            _leftCombatant.MoveToPosition(basePos, centerIndex);
            _rightCombatant.MoveToPosition(basePos, centerIndex);

            // Reset scores
            _roundScoreLeft = 0;
            _roundScoreRight = 0;
            _matchWinsLeft = 0;
            _matchWinsRight = 0;

            _gameRunning = true;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop()
        {
            while (_gameRunning)
            {
                // Show current scores each loop
                _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);

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
                }
                else if (result == -1)
                {
                    _leftCombatant.PlayRoundFeedback(false);
                    _rightCombatant.PlayRoundFeedback(true);
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
                    _rightCombatant.MoveToPosition(basePos, _boardIndex);
                }
                else if (_boardIndex == 0)
                {
                    // left-most wall reached -> left is loser, move left to wall, right stays one column inward
                    int rightIndex = Mathf.Min(lastIndexLocal, 1);
                    _leftCombatant.MoveToPosition(basePos, _boardIndex);
                    }
                else
                {
                    // normal case: move both to the same column
                    _leftCombatant.MoveToPosition(basePos, _boardIndex);
                    _rightCombatant.MoveToPosition(basePos, _boardIndex);
                }

                // Wait while movements occur (simple wait)
                yield return new WaitForSeconds(_roundDelay);

                // Check knockout by wall using Board column count
                int lastIndex = _board.Positions.Length - 1;
                bool matchEndedByKnockout = false;
                if (_boardIndex == lastIndex)
                {
                    // right-most wall reached -> right knocked out -> left wins match
                    _ui?.ShowStatus($"{_leftCombatant.CombatantName} knocked out. {_rightCombatant.CombatantName} wins match!");
                    _matchWinsLeft++;
                    matchEndedByKnockout = true;
                }
                else if (_boardIndex == 0)
                {
                    // left-most wall reached -> left knocked out -> right wins match
                    _ui?.ShowStatus($"{_rightCombatant.CombatantName} knocked out. {_leftCombatant.CombatantName} wins match!");
                    _matchWinsRight++;
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
                    _roundScoreLeft = 0;
                    _roundScoreRight = 0;
                    int center = _board.CenterIndex;
                    _boardIndex = center;
                    Vector3 centerBase = _board.Positions[center];
                    _leftCombatant.MoveToPosition(centerBase, center);
                    _rightCombatant.MoveToPosition(centerBase, center);

                    // small delay before starting next match
                    yield return new WaitForSeconds(1f);
                    continue; // next match continues
                }

                // If no knockout, matches continue; series only tracked by match wins
                // small delay before next RPS round
                yield return new WaitForSeconds(0.5f);
            }

            // Game over - ensure UI shows final state
            _ui?.ShowScores(_roundScoreLeft, _roundScoreRight, _matchWinsLeft, _matchWinsRight);
            _ui?.ShowCountdown(0);
        }
    }
}
