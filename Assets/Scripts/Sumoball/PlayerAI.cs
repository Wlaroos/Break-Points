using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sumoball
{
    [Serializable]
    public class MoveDistribution
    {
        [SerializeField, Tooltip("Name shown in the inspector — used to select this distribution by name.")]
        private string distributionName = "Default";
        public string Name => distributionName;

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
        // Replace single distribution with a serializable list you can edit in the Inspector.
        [SerializeField] private List<MoveDistribution> distributions = new List<MoveDistribution>() { new MoveDistribution() };
        [SerializeField] private int currentDistributionIndex = 0;

        private System.Random rng;

        void Awake()
        {
            rng = new System.Random(System.Guid.NewGuid().GetHashCode());

            if (distributions == null || distributions.Count == 0)
            {
                distributions = new List<MoveDistribution>() { new MoveDistribution() };
            }

            foreach (var d in distributions) d.Normalize();
            currentDistributionIndex = Mathf.Clamp(currentDistributionIndex, 0, distributions.Count - 1);
        }

        // Ensure inspector changes are normalized and index stays valid
        private void OnValidate()
        {
            if (distributions == null || distributions.Count == 0) return;
            foreach (var d in distributions) d.Normalize();
            currentDistributionIndex = Mathf.Clamp(currentDistributionIndex, 0, distributions.Count - 1);
        }

        private MoveDistribution CurrentDistribution => distributions[currentDistributionIndex];

        public RPSMove PickMove()
        {
            return CurrentDistribution.Sample(rng);
        }

        // Select distribution by index (keeps backward compatibility)
        public void SetDistribution(int index)
        {
            if (distributions == null || index < 0 || index >= distributions.Count) return;
            currentDistributionIndex = index;
        }

        // New: select distribution by name (returns true if found and selected)
        public bool SetDistribution(string name)
        {
            if (string.IsNullOrEmpty(name) || distributions == null) return false;
            int idx = distributions.FindIndex(d => string.Equals(d.Name, name, StringComparison.Ordinal));
            if (idx < 0) return false;
            currentDistributionIndex = idx;
            return true;
        }

        // Helper: get available distribution names
        public string[] GetDistributionNames()
        {
            if (distributions == null || distributions.Count == 0) return Array.Empty<string>();
            var arr = new string[distributions.Count];
            for (int i = 0; i < distributions.Count; i++) arr[i] = distributions[i].Name;
            return arr;
        }

        // Optional: legacy cycling methods kept for convenience
        public void NextDistribution()
        {
            if (distributions == null || distributions.Count == 0) return;
            currentDistributionIndex = (currentDistributionIndex + 1) % distributions.Count;
        }

        public void PrevDistribution()
        {
            if (distributions == null || distributions.Count == 0) return;
            currentDistributionIndex = (currentDistributionIndex - 1 + distributions.Count) % distributions.Count;
        }
    }
}
