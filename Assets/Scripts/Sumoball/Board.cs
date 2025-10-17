using UnityEngine;

namespace Sumoball
{
    [DisallowMultipleComponent]
    public class Board : MonoBehaviour
    {
        [Header("Board Settings")]
        [Min(3), SerializeField] private int _columns = 5;
        [SerializeField] private float _spanMultiplier = 1f;

        // runtime positions (world space)
        private Vector3[] _positions;

        [Header("Visualization")]
        [SerializeField] private Sprite _markerSprite; // optional sprite to show runtime markers
        [SerializeField] private Color _markerColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float _markerSize = 0.25f;
        [SerializeField] private float _yOffset = -1f; // vertical offset to avoid z-fighting

        // root for created marker GameObjects (not serialized)
        private Transform _markerRoot;

        public int Columns
        {
            get => _columns;
            set
            {
                // keep the public setter consistent with attributes: min 3 and odd
                _columns = Mathf.Max(3, value);
                if (_columns % 2 == 0) _columns += 1;
            }
        }

        public float SpanMultiplier
        {
            get => _spanMultiplier;
            set => _spanMultiplier = Mathf.Max(0f, value);
        }

        // return a copy to avoid external modification of internal array
        public Vector3[] Positions => (_positions != null) ? (Vector3[])_positions.Clone() : null;

        public int CenterIndex => (_positions != null && _positions.Length > 0) ? _positions.Length / 2 : 0;

        // Compute positions using two reference transforms (usually the two fighters)
        public void Setup(Transform leftTransform, Transform rightTransform)
        {
            if (leftTransform == null || rightTransform == null) return;

            // ensure valid column count via property
            Columns = _columns;

            Vector3 leftPos = leftTransform.position;
            Vector3 rightPos = rightTransform.position;
            Vector3 mid = (leftPos + rightPos) * 0.5f;

            float baseSpan = Vector3.Distance(leftPos, rightPos) * _spanMultiplier;
            float halfSpan = baseSpan * 0.5f;

            // Use the actual direction between the two transforms so positions follow that line.
            // If the two transforms coincide (or nearly), fall back to this object's right.
            Vector3 dir = rightPos - leftPos;
            if (dir.sqrMagnitude < 1e-6f)
            {
                dir = transform != null ? transform.right : Vector3.right;
            }
            dir = dir.normalized;

            Vector3 leftMost = mid - dir * halfSpan;
            float stepScalar = (_columns > 1) ? (baseSpan / (_columns - 1)) : 0f;
            Vector3 stepVec = dir * stepScalar;

            _positions = GetPreviewPositions();

            // create simple runtime markers if sprite provided
            CreateMarkers();
        }

        private void CreateMarkers()
        {
            ClearMarkers();

            if (_markerSprite == null || _positions == null || _positions.Length == 0) return;

            _markerRoot = new GameObject($"{name}_BoardMarkers").transform;
            // parent to this object and keep local positions so markers follow the board if it moves
            _markerRoot.SetParent(transform, false);

            for (int i = 0; i < _positions.Length; i++)
            {
                GameObject go = new GameObject($"marker_{i}");
                go.transform.SetParent(_markerRoot, false);

                // compute local position so markers follow the board transform
                Vector3 worldPos = _positions[i] + Vector3.up * (_yOffset + 0.01f);
                go.transform.localPosition = transform.InverseTransformPoint(worldPos);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _markerSprite;
                sr.color = _markerColor;
                float scale = Mathf.Max(0.001f, _markerSize);
                go.transform.localScale = Vector3.one * scale;
                // optionally set sorting order (so markers are visible)
                sr.sortingOrder = 1000;
#if UNITY_EDITOR
                // prevent accidentally saving editor-created markers into prefabs
                go.hideFlags = HideFlags.None;
#endif
            }
        }

        // removes old marker root (and children)
        private void ClearMarkers()
        {
            if (_markerRoot == null) return;

            if (Application.isPlaying)
            {
                Destroy(_markerRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_markerRoot.gameObject);
            }
            _markerRoot = null;
        }

        void OnDestroy()
        {
            ClearMarkers();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // use property setters to ensure consistent validation logic
            Columns = _columns;
            SpanMultiplier = _spanMultiplier;

            if (_markerSize < 0f) _markerSize = 0f;
        }
#endif

        // Use OnDrawGizmos so gizmos can be visible without selecting the object.
        void OnDrawGizmos()
        {
            Vector3[] drawPositions = GetPreviewPositions();
            if (drawPositions == null || drawPositions.Length == 0) return;

            Gizmos.color = _markerColor;
            // draw small spheres at each position and larger at center
            int center = drawPositions.Length / 2;
            for (int i = 0; i < drawPositions.Length; i++)
            {
                // visualize markers at the same vertical offset used by runtime markers
                Gizmos.DrawSphere(drawPositions[i] + Vector3.up * _yOffset, Mathf.Max(0.001f, _markerSize * 1f));
            }

            // highlight center with ring
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(drawPositions[center] + Vector3.up * _yOffset, Mathf.Max(0.001f, _markerSize * 1.5f));
        }

        // Compute a preview positions array for gizmos: prefer runtime _positions,
        // otherwise compute from preview transforms or a simple centered fallback.
        private Vector3[] GetPreviewPositions()
        {
            if (_positions != null && _positions.Length >= 3) return _positions;

            // Fallback: create a centered preview around this object's position using transform.right
            float baseUnit = (_spanMultiplier > 0f) ? _spanMultiplier : 1f;
            float baseSpanFallback = (_columns - 1) * baseUnit;
            Vector3 dir = transform != null ? transform.right : Vector3.right;
            Vector3 leftMostFallback = transform.position - dir * (baseSpanFallback * 0.5f);
            float stepFallback = (_columns > 1) ? (baseSpanFallback / (_columns - 1)) : 0f;

            Vector3[] fallback = new Vector3[_columns];
            for (int i = 0; i < _columns; i++)
                fallback[i] = leftMostFallback + dir * (i * stepFallback);
            return fallback;
        }
    }
}