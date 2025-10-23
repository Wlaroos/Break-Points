using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MavMovin
{
    public class GameManager : MonoBehaviour
    {
        [Header("Horse Settings")]
        [SerializeField] private Horse _horse01;
        [SerializeField] private Horse _horse02;
        [SerializeField] private Horse _horse03;
        [SerializeField] private Horse _horse04;

        [Header("Track Settings")]
        [SerializeField] private Transform _trackSectionPrefab;
        [SerializeField] private int _trackLength = 10;
        [SerializeField] private float _trackVerticalSpacing = 2.5f;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 _spawnOrigin = Vector3.zero;
        [SerializeField] private Color _horseGizmoColor = Color.cyan;
        [SerializeField] private Color _trackGizmoColor = Color.yellow;
        [SerializeField] private float _horseGizmoRadius = 0.25f;

        void Awake()
        {
            for (int i = 0; i < 4; i++)
            {
                Horse horseInstance = Instantiate(GetHorseByIndex(i));
                Vector3 pos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);

                horseInstance.transform.position = pos;
                
                GenerateTrack(pos);
            }
        }

        private void GenerateTrack(Vector3 startPosition)
        {
            float trackWidth = 1f;
            if (_trackSectionPrefab != null)
            {
                var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) trackWidth = sr.bounds.size.x;
            }

            for (int i = 0; i < _trackLength; i++)
            {
                Vector3 spawnPosition = startPosition + new Vector3(i * trackWidth, 0, 0);
                Instantiate(_trackSectionPrefab, spawnPosition, Quaternion.identity);
            }
        }

        private Horse GetHorseByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return _horse01;
                case 1:
                    return _horse02;
                case 2:
                    return _horse03;
                case 3:
                    return _horse04;
                default:
                    return null;
            }
        }

        // Draw gizmos for spawn origin, horse positions and track sections.
        void OnDrawGizmos()
        {
            // show origin
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_spawnOrigin, _horseGizmoRadius * 0.75f);

            float trackWidth = 1f;
            if (_trackSectionPrefab != null)
            {
                var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) trackWidth = sr.bounds.size.x;
            }

            // draw horses
            Gizmos.color = _horseGizmoColor;
            for (int i = 0; i < 4; i++)
            {
                Vector3 horsePos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);
                Gizmos.DrawSphere(horsePos, _horseGizmoRadius);
            }

            // draw track sections for each lane
            Gizmos.color = _trackGizmoColor;
            for (int lane = 0; lane < 4; lane++)
            {
                Vector3 laneStart = _spawnOrigin + new Vector3(0, -(lane * _trackVerticalSpacing), 0);
                for (int i = 0; i < _trackLength; i++)
                {
                    Vector3 spawnPos = laneStart + new Vector3(i * trackWidth, 0, 0);
                    Gizmos.DrawWireCube(spawnPos, new Vector3(trackWidth * 0.9f, _trackVerticalSpacing * 0.8f, 0.1f));
                }
            }
        }

#if UNITY_EDITOR
        // Make spawn origin draggable in the Scene view when this object is selected.
        void OnDrawGizmosSelected()
        {
            // Position handle for adjusting the spawn origin
            EditorGUI.BeginChangeCheck();
            Vector3 newOrigin = Handles.PositionHandle(_spawnOrigin, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move Spawn Origin");
                _spawnOrigin = newOrigin;
                EditorUtility.SetDirty(this);
            }

            // labels to clarify gizmos
            Handles.color = _horseGizmoColor;
            Handles.Label(_spawnOrigin + Vector3.up * 0.2f, "Spawn Origin");
            for (int i = 0; i < 4; i++)
            {
                Vector3 horsePos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);
                Handles.Label(horsePos + Vector3.right * 0.2f, $"Horse {i+1}");
            }
        }
#endif

    }
}
