using System.Collections;
using UnityEngine;

namespace FanExperiencePrototypes
{
    [RequireComponent(typeof(Transform))]
    public class Combatant : MonoBehaviour
    {
        [SerializeField] private string _combatantName = "Fighter";
        public string CombatantName { get { return _combatantName; } }
        [SerializeField] private float _pushSpeed = 4f; // speed of being pushed

        // NOTE: Board positions removed from Combatant - Board owns the grid.
        [SerializeField] private int _currentIndex = 0; // will be set by GameManager to center
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set { _currentIndex = value; }
        }

        // whether this combatant is the left-side fighter (used to offset so fighters don't overlap)
        [SerializeField] private bool _isLeft = true;
        public bool IsLeft => _isLeft;

        // How far to offset each fighter from the central column position (tweak in Inspector)
        [SerializeField] private float _lateralSeparation = 0.6f;
        public float LateralSeparation => _lateralSeparation;

        // Feedback settings
        [Header("Round Feedback")]
        [SerializeField] private float _feedbackDuration = 0.25f;
        [SerializeField] private float _feedbackNudge = 0.22f;
        [SerializeField] private float _feedbackScale = 1.12f;

        [Header("Feedback Tints")]
        [SerializeField] private Color _winTint = Color.white;
        [SerializeField] private Color _loseTint = new Color(0.8f, 0.6f, 0.6f);
        [SerializeField] private Color _neutralTint = Color.white;

        private Vector3 _targetPosition;
        private bool isMoving;
        private SpriteRenderer _spriteRenderer;

        void Start()
        {
            // Ensure currentIndex is at least initialized
            if (_currentIndex < 0) _currentIndex = 0;

            // Initialize transform to current position (will be moved by GameManager when board is created)
            _targetPosition = transform.position;
            transform.position = _targetPosition;

            // cache optional sprite renderer for tint feedback
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _pushSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
                {
                    isMoving = false;
                }
            }
        }

        // Public method used by GameManager: receive the board base position for the column
        // Combatant will apply its lateral offset and move toward the computed target.
        public void MoveToPosition(Vector3 boardBasePosition, int columnIndex)
        {
            _currentIndex = columnIndex;
            Vector3 lateral = (_isLeft ? Vector3.left : Vector3.right) * _lateralSeparation;
            _targetPosition = boardBasePosition + lateral;
            isMoving = true;
        }

        // Backwards-compatible helper (keeps API if something still calls MoveToIndex)
        public void MoveToIndex(int idx)
        {
            // deprecated: no internal board; caller should provide base position via MoveToPosition
            _currentIndex = idx;
            // keep current target if not set; caller should call MoveToPosition for proper placement
        }

        // Check side using board column count supplied by caller (Board owns columns)
        public bool IsOnSide(int boardColumns)
        {
            if (boardColumns <= 0) return false;
            return _currentIndex == 0 || _currentIndex == boardColumns - 1;
        }

        // Play a brief visual feedback for a round: winner gets a small scale-up + nudge toward opponent,
        // loser gets a slight recoil. Call from GameManager after round resolution.
        public void PlayRoundFeedback(bool won)
        {
            StopAllCoroutines();
            StartCoroutine(RoundFeedbackCoroutine(won));
        }

        private IEnumerator RoundFeedbackCoroutine(bool won)
        {
            Vector3 origPos = transform.position;
            Vector3 nudgeDir;
            // if left-side fighter, a win nudges rightwards (toward opponent); otherwise leftwards
            nudgeDir = (_isLeft) ? Vector3.right : Vector3.left;
            float dirSign = won ? 1f : -1f;
            Vector3 targetPos = origPos + nudgeDir * _feedbackNudge * dirSign;

            Vector3 origScale = transform.localScale;
            Vector3 targetScale = origScale * (won ? _feedbackScale : (1f / _feedbackScale));

            // tint sprite if available
            Color origColor = _spriteRenderer ? _spriteRenderer.color : Color.white;
            Color targetTint = won ? _winTint : _loseTint;

            float t = 0f;
            while (t < _feedbackDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / _feedbackDuration);
                transform.position = Vector3.Lerp(origPos, targetPos, p);
                transform.localScale = Vector3.Lerp(origScale, targetScale, p);
                if (_spriteRenderer)
                {
                    _spriteRenderer.color = Color.Lerp(origColor, targetTint, p);
                }
                yield return null;
            }

            // return
            t = 0f;
            while (t < _feedbackDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / _feedbackDuration);
                transform.position = Vector3.Lerp(targetPos, origPos, p);
                transform.localScale = Vector3.Lerp(targetScale, origScale, p);
                if (_spriteRenderer)
                {
                    _spriteRenderer.color = Color.Lerp(targetTint, origColor, p);
                }
                yield return null;
            }

            transform.position = origPos;
            transform.localScale = origScale;
            if (_spriteRenderer) _spriteRenderer.color = origColor;
        }
    }
}
