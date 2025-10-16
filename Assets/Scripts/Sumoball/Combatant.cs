using System.Collections;
using UnityEngine;

namespace Sumoball
{
    [RequireComponent(typeof(Transform))]
    public class Combatant : MonoBehaviour
    {
        [SerializeField] private string _combatantName = "Fighter";
        public string CombatantName { get { return _combatantName; } }

        // per-combatant state
        [SerializeField] private int _currentIndex = 0; // will be set by GameManager to center
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set { _currentIndex = value; }
        }

        // whether this combatant is the left-side fighter (used to offset so fighters don't overlap)
        [SerializeField] private bool _isLeft = true;
        public bool IsLeft => _isLeft;

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
                // read shared speed from GameManager singleton (fallback to 4f if none)
                float speed = GameManager.Instance != null ? GameManager.Instance.PushSpeed : 4f;
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.deltaTime);
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
            float lateralSeparation = GameManager.Instance != null ? GameManager.Instance.LateralSeparation : 0.6f;
            Vector3 lateral = (_isLeft ? Vector3.left : Vector3.right) * lateralSeparation;
            _targetPosition = boardBasePosition + lateral;
            isMoving = true;
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
            // read shared feedback settings (use reasonable fallbacks if Instance missing)
            var gm = GameManager.Instance;
            float duration = gm != null ? gm.FeedbackDuration : 0.25f;
            float nudge = gm != null ? gm.FeedbackNudge : 0.22f;
            float scaleFactor = gm != null ? gm.FeedbackScale : 1.12f;
            Color winTint = gm != null ? gm.WinTint : Color.white;
            Color loseTint = gm != null ? gm.LoseTint : new Color(0.8f, 0.6f, 0.6f);

            Vector3 origPos = transform.position;
            Vector3 nudgeDir;
            nudgeDir = (_isLeft) ? Vector3.right : Vector3.left;
            float dirSign = won ? 1f : -1f;
            Vector3 targetPos = origPos + nudgeDir * nudge * dirSign;

            Vector3 origScale = transform.localScale;
            Vector3 targetScale = origScale * (won ? scaleFactor : (1f / scaleFactor));

            // tint sprite if available
            Color origColor = _spriteRenderer ? _spriteRenderer.color : Color.white;
            Color targetTint = won ? winTint : loseTint;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
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
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
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
