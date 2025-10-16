using System;
using UnityEngine;

namespace Sumoball
{
    [Serializable]
    public class MoveDistribution
    {
    [Range(0,1), SerializeField] private float _rock = 0.33f;
    [Range(0,1), SerializeField] private float _paper = 0.33f;
    [Range(0,1), SerializeField] private float _scissors = 0.33f;
    [Range(0,1), SerializeField] private float _super = 0.01f; // very rare

    public void Normalize()
    {
        float sum = _rock + _paper + _scissors + _super;
        if (sum <= 0) { _rock = _paper = _scissors = 1f/3f; _super = 0f; return; }
        _rock /= sum; _paper /= sum; _scissors /= sum; _super /= sum;
    }

    public RPSMove Sample(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        if (r < _rock) return RPSMove.Rock;
        if (r < _rock + _paper) return RPSMove.Paper;
        if (r < _rock + _paper + _scissors) return RPSMove.Scissors;
        return RPSMove.Super;
    }
    }

    public class PlayerAI : MonoBehaviour
    {
    public MoveDistribution distribution = new MoveDistribution();
    private System.Random rng;

    void Awake()
    {
        rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        distribution.Normalize();
    }

    public RPSMove PickMove()
    {
        return distribution.Sample(rng);
    }
    }
}
