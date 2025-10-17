using System.Collections;
using System.Collections.Generic; // added for List<T>
using UnityEngine;

namespace Sumoball
{
    [RequireComponent(typeof(Transform))]
    public class Combatant : MonoBehaviour
    {
        [SerializeField] private string _combatantName = "Fighter";
        public string CombatantName { get { return _combatantName; } }

        // per-combatant state
        private int _currentIndex = 0; // will be set by GameManager to center
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set { _currentIndex = value; }
        }

        // whether this combatant is the left-side fighter (used to offset so fighters don't overlap)
        [SerializeField] private bool _isLeft = true;
        public bool IsLeft => _isLeft;

        // Edge visit tracking + per-distribution visit threshold (editable in inspector).
        [System.Serializable]
        private class EdgeDistributionEntry
        {
            [SerializeField, Tooltip("Name of the MoveDistribution in PlayerAI to apply (case-sensitive).")]
            private string distributionName = "";
            public string Name => distributionName;

            [SerializeField, Min(1), Tooltip("Number of edge visits required before this distribution becomes active.")]
            private int visitsRequired = 1;
            public int VisitsRequired => visitsRequired;
        }

        private int _edgeVisits = 0;
        // Expose number of times this combatant has visited its edge (read-only)
        public int EdgeVisits => _edgeVisits;
        // Reset edge visit count (call when this combatant loses a match)
        [Header("Edge Visits and AI Distribution")]
        [Tooltip("Configure which PlayerAI distribution to apply and how many edge visits are required before applying it.")]
        [SerializeField] private List<EdgeDistributionEntry> _edgeDistributions = new List<EdgeDistributionEntry>();

        private Vector3 _targetPosition;
        private bool isMoving;
        private SpriteRenderer _spriteRenderer;

        // PlayerAI lives on the same GameObject — cache it.
        private PlayerAI _playerAI;

        // Public state other systems (GameManager) can wait on
        public bool IsMoving { get; private set; }
        private Coroutine _moveCoroutine;

        void Start()
        {
            // Ensure currentIndex is at least initialized
            if (_currentIndex < 0) _currentIndex = 0;

            // Initialize transform to current position (will be moved by GameManager when board is created)
            _targetPosition = transform.position;
            transform.position = _targetPosition;

            // cache optional sprite renderer for tint feedback
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // cache PlayerAI (assumed present on same GameObject)
            _playerAI = GetComponent<PlayerAI>();
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

        // Example coroutine that moves this transform smoothly to the computed position.
        // Replace lateral offset logic with whatever your existing MoveToPosition used.
        public IEnumerator MoveToPositionCoroutine(Vector3 basePosition, int columnIndex)
        {
            // If already moving, stop previous movement (or bail depending on desired behavior)
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            IsMoving = true;
            // remember previous index so we don't count an "arrival" if we were already on that edge
            int prevIndex = _currentIndex;
            // ensure current index is set for this move
            _currentIndex = columnIndex;
            // compute target using GameManager.LateralSeparation etc.
            Vector3 target = basePosition + (_isLeft ? Vector3.left : Vector3.right) * (GameManager.Instance != null ? GameManager.Instance.LateralSeparation : 0.6f);
            float tolerance = 0.01f;
            float speed = GameManager.Instance.PushSpeed;

            while (Vector3.Distance(transform.position, target) > tolerance)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            // Arrival: check if this column is the "one-from-side" edge and increment visits if so.
            int boardCols = GameManager.Instance != null ? GameManager.Instance.BoardColumns : 0;
            if (boardCols >= 3)
            {
                int edgeIndex = _isLeft ? 1 : (boardCols - 2);
                // Only count an edge "visit" if we arrived at the edge from a different column.
                if (_currentIndex == edgeIndex && prevIndex != edgeIndex)
                {
                    _edgeVisits++;
                    ApplyDistributionForEdgeVisits();
                }
            }
            IsMoving = false;
            _moveCoroutine = null;
        }

        // Try to pick a PlayerAI distribution based on the number of edge visits.
        private void ApplyDistributionForEdgeVisits()
        {
            if (_edgeDistributions == null || _edgeDistributions.Count == 0) return;

            // Find the distribution with the highest VisitsRequired that is <= current visits.
            EdgeDistributionEntry best = null;
            foreach (var e in _edgeDistributions)
            {
                if (e == null) continue;
                if (e.VisitsRequired <= _edgeVisits)
                {
                    if (best == null || e.VisitsRequired > best.VisitsRequired) best = e;
                }
            }
            if (best == null) return;
            string distName = best.Name;
            if (string.IsNullOrEmpty(distName)) return;

            // Use the PlayerAI on this GameObject (assumed present)
            if (_playerAI != null)
            {
                _playerAI.SetDistribution(distName);
            }
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

        public void ResetEdgeVisits()
        {
            _edgeVisits = 0;
        }
    }
}
