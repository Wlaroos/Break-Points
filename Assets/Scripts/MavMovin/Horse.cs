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
        // remaining number of track spaces the boost is still active for
        private int _boostRemainingMoves = 0;

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

        // Apply a percentage boost to move chance for a number of track moves (moveCount).
        // percent is e.g. 0.2f to add 20%. moveCount is how many actual moves this boost should remain active.
        public void ApplyMovePercentBoost(float percent, int moveCount)
        {
            if (moveCount <= 0) return;

            _currentMoveChance = Mathf.Clamp01(_currentMoveChance + percent);
            _boostRemainingMoves = moveCount;
        }

        // Call this each time the horse actually moves forward one or more track spaces.
        // If a boost is active it will decrement the remaining move count and clear the boost when used up.
        public void NotifyMoved(int spacesMoved = 1)
        {
            if (_boostRemainingMoves <= 0) return;

            _boostRemainingMoves = Mathf.Max(0, _boostRemainingMoves - spacesMoved);
            if (_boostRemainingMoves == 0)
            {
                _currentMoveChance = _baseMoveChance;
            }
        }
    }
}
