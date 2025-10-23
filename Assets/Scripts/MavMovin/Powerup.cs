using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MavMovin
{
    public class Powerup : MonoBehaviour
    {
        [SerializeField] private float _movePercentBoost = 0.2f;
        public float MovePercentBoost => _movePercentBoost;
    }
}
