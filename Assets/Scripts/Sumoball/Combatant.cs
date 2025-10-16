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

        private Vector3 _targetPosition;
        private bool isMoving;

        void Start()
        {
            // Ensure currentIndex is at least initialized
            if (_currentIndex < 0) _currentIndex = 0;

            // Initialize transform to current position (will be moved by GameManager when board is created)
            _targetPosition = transform.position;
            transform.position = _targetPosition;
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
    }
}
