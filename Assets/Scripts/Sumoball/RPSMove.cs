using System;
using UnityEngine;

namespace Sumoball
{
    public enum RPSMove
    {
        Shove,
        Slap,
        Grab,
        Super // very rare move that always wins
    }

    public static class RPSMoveExtensions
    {
        public static bool Beats(this RPSMove a, RPSMove b)
        {
            if (a == RPSMove.Super && b != RPSMove.Super) return true;
            if (b == RPSMove.Super && a != RPSMove.Super) return false;
            if (a == b) return false;
            return (a == RPSMove.Shove && b == RPSMove.Grab)
                || (a == RPSMove.Slap && b == RPSMove.Shove)
                || (a == RPSMove.Grab && b == RPSMove.Slap);
        }
    }
}
