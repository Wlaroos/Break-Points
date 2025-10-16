using System;
using UnityEngine;

namespace FanExperiencePrototypes
{
    public enum RPSMove
    {
        Rock,
        Paper,
        Scissors,
        Super // very rare move that always wins
    }

    public static class RPSMoveExtensions
    {
        public static bool Beats(this RPSMove a, RPSMove b)
        {
            if (a == RPSMove.Super && b != RPSMove.Super) return true;
            if (b == RPSMove.Super && a != RPSMove.Super) return false;
            if (a == b) return false;
            return (a == RPSMove.Rock && b == RPSMove.Scissors)
                || (a == RPSMove.Paper && b == RPSMove.Rock)
                || (a == RPSMove.Scissors && b == RPSMove.Paper);
        }
    }
}
