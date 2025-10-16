using UnityEngine;

namespace FanExperiencePrototypes
{
    public class Board : MonoBehaviour
    {
        [Header("Board Settings")]
        [Min(3), SerializeField] private int _columns = 5;
        [SerializeField] private float _spanMultiplier = 1f;

        private Vector3[] _positions;

        [Header("Visualization")]
        [SerializeField] private Sprite _markerSprite; // optional sprite to show runtime markers
        [SerializeField] private Color _markerColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float _markerSize = 0.25f;
        [SerializeField] private float _yOffset = 0.75f; // vertical offset to avoid z-fighting

        private Transform _markerRoot;

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

            // create simple runtime markers if sprite provided
            CreateMarkers();
        }

        private void CreateMarkers()
        {
            // remove old root if present
            if (_markerRoot != null)
            {
                if (Application.isPlaying) Destroy(_markerRoot.gameObject);
                else DestroyImmediate(_markerRoot.gameObject);
                _markerRoot = null;
            }

            if (_markerSprite == null || _positions == null) return;

            _markerRoot = new GameObject("BoardMarkers").transform;
            _markerRoot.SetParent(transform, false);

            for (int i = 0; i < _positions.Length; i++)
            {
                GameObject go = new GameObject($"marker_{i}");
                go.transform.SetParent(_markerRoot, false);
                go.transform.position = (_positions[i] + Vector3.up * 0.01f) - new Vector3(0,_yOffset,0); // slight offset so not z-fighting
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _markerSprite;
                sr.color = _markerColor;
                float scale = Mathf.Max(0.001f, _markerSize);
                go.transform.localScale = Vector3.one * scale;
                // optionally set sorting order (so markers are visible)
                sr.sortingOrder = 1000;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_columns < 3) _columns = 3;
            if (_columns % 2 == 0) _columns += 1;
            if (_spanMultiplier < 0f) _spanMultiplier = 0f;
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
                Gizmos.DrawSphere(drawPositions[i] - new Vector3(0, _yOffset, 0), _markerSize * 1f);
            }

            // highlight center with ring
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(drawPositions[center] - new Vector3(0, _yOffset, 0), _markerSize * 1.5f);
        }

        // Compute a preview positions array for gizmos: prefer runtime _positions,
        // otherwise compute from preview transforms or a simple centered fallback.
        private Vector3[] GetPreviewPositions()
        {
            if (_positions != null && _positions.Length >= 3) return _positions;

            // Fallback: create a centered preview around this object's position using editorDefaultStep
            float baseSpanFallback = (_columns - 1) * _spanMultiplier;
            Vector3 leftMostFallback = transform.position + Vector3.left * (baseSpanFallback * 0.5f);
            float stepFallback = (_columns > 1) ? (baseSpanFallback / (_columns - 1)) : 0f;

            Vector3[] fallback = new Vector3[_columns];
            for (int i = 0; i < _columns; i++)
                fallback[i] = leftMostFallback + Vector3.right * (i * stepFallback);
            return fallback;
        }
    }
}