using UnityEngine;

namespace FanExperiencePrototypes
{
    public class Board : MonoBehaviour
    {
        [Header("Board Settings")]
        [Min(3), SerializeField] private int _columns = 5;
        [SerializeField] private float _spanMultiplier = 1f;

        private Vector3[] _positions;

        public int Columns
        {
            get => _columns;
            set
            {
                _columns = Mathf.Max(3, value);
                if (_columns % 2 == 0) _columns += 1;
            }
        }

        public float SpanMultiplier
        {
            get => _spanMultiplier;
            set => _spanMultiplier = Mathf.Max(0f, value);
        }

        public Vector3[] Positions => _positions;
        public int CenterIndex => (_positions != null && _positions.Length > 0) ? _positions.Length / 2 : 0;

        // Compute positions using two reference transforms (usually the two fighters)
        public void Setup(Transform leftTransform, Transform rightTransform)
        {
            if (leftTransform == null || rightTransform == null) return;

            if (_columns < 3) Columns = 3;
            if (_columns % 2 == 0) _columns += 1;

            Vector3 leftPos = leftTransform.position;
            Vector3 rightPos = rightTransform.position;
            Vector3 mid = (leftPos + rightPos) * 0.5f;
            float baseSpan = Vector3.Distance(leftPos, rightPos) * _spanMultiplier;
            float halfSpan = baseSpan * 0.5f;
            Vector3 leftMost = mid + Vector3.left * halfSpan;
            float step = (_columns > 1) ? (baseSpan / (_columns - 1)) : 0f;

            _positions = new Vector3[_columns];
            for (int i = 0; i < _columns; i++)
            {
                _positions[i] = leftMost + Vector3.right * (i * step);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_columns < 3) _columns = 3;
            if (_columns % 2 == 0) _columns += 1;
            if (_spanMultiplier < 0f) _spanMultiplier = 0f;
        }
#endif
    }
}