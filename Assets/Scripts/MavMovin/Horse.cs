using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MavMovin
{
    public class Horse : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float _moveChance = 0.5f;
        // internal current chance (can be boosted temporarily)
        private float _currentMoveChance;
        public float MoveChance => _currentMoveChance;

        [SerializeField, Range(0, 1)] private float _waitChance = 0.5f;
        public float WaitChance => _waitChance;

        [SerializeField] private int _moveDistance = 1;
        public int MoveDistance => _moveDistance;

        private float _baseMoveChance;
        private Coroutine _boostRoutine;

        [SerializeField] private Color _displayColor;
        public Color DisplayColor => _displayColor;

        // Ensure serialized values are valid in the editor and always sum to 1.0
        void OnValidate()
        {
            _moveChance = Mathf.Clamp01(_moveChance);
            // enforce complementary value
            _waitChance = 1f - _moveChance;

            // keep base/current in sync when editing in inspector
            _baseMoveChance = _moveChance;
            // if current hasn't been initialized yet, set it to base
            if (!Application.isPlaying)
                _currentMoveChance = _baseMoveChance;
            else
                _currentMoveChance = Mathf.Clamp01(_currentMoveChance);
        }

        void Awake()
        {
            // initialize base/current ensuring they reflect the serialized move chance
            _baseMoveChance = Mathf.Clamp01(_moveChance);
            _currentMoveChance = _baseMoveChance;

            // ensure wait is complementary at runtime as well
            _waitChance = 1f - _baseMoveChance;
        }

        public void TeleportTo(Vector3 worldPos)
        {
            transform.position = worldPos;
        }

        // Apply a percentage boost to move chance for a duration (percent is e.g. 0.2f to add 20%)
        public void ApplyMovePercentBoost(float percent, float duration = 3f)
        {
            if (_boostRoutine != null)
                StopCoroutine(_boostRoutine);
            _boostRoutine = StartCoroutine(ApplyBoostRoutine(percent, duration));
        }

        private IEnumerator ApplyBoostRoutine(float percent, float duration)
        {
            _currentMoveChance = Mathf.Clamp01(_currentMoveChance + percent);
            yield return new WaitForSeconds(duration);
            _currentMoveChance = _baseMoveChance;
            _boostRoutine = null;
        }
    }
}
