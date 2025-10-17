using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sumoball
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _leftMoveText;
        [SerializeField] private TextMeshProUGUI _rightMoveText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _leftEdgeVisitsText;
        [SerializeField] private TextMeshProUGUI _rightEdgeVisitsText;
        [SerializeField] private TextMeshProUGUI _leftDistributionText;
        [SerializeField] private TextMeshProUGUI _rightDistributionText;

        [Header("Visual Feedback")]
        [SerializeField] private Color _leftColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color _rightColor = new Color(1f, 0.6f, 0.6f);
        [SerializeField] private Color _winColor = Color.green;
        [SerializeField] private Color _tieColor = Color.yellow;
        [SerializeField] private float _flashDuration = 0.6f;

        public void ShowMoves(RPSMove left, RPSMove right)
        {
            if (_leftMoveText) { _leftMoveText.text = "Left: " + left.ToString(); _leftMoveText.color = _leftColor; }
            if (_rightMoveText) { _rightMoveText.text = "Right: " + right.ToString(); _rightMoveText.color = _rightColor; }
        }

        public void ShowCountdown(int seconds)
        {
            if (_countdownText) _countdownText.text = seconds > 0 ? seconds.ToString() : "Go!";
        }

        public void ShowStatus(string s)
        {
            if (_statusText) _statusText.text = s;
        }

        // Combined score display (round score = current match RPS wins; match score = matches won)
        public void ShowScores(int roundLeft, int roundRight, int matchLeft, int matchRight)
        {
            if (_statusText)
            {
                _statusText.text = $"Round: {roundLeft} - {roundRight}\nMatch: {matchLeft} - {matchRight}";
                _statusText.color = Color.white;
            }
        }

        // Show edge visit counts and the active distribution names for each fighter.
        public void ShowEdgeInfo(int leftVisits, string leftDistribution, int rightVisits, string rightDistribution)
        {
            if (_leftEdgeVisitsText) _leftEdgeVisitsText.text = $"Edge Visits: {leftVisits}";
            if (_rightEdgeVisitsText) _rightEdgeVisitsText.text = $"Edge Visits: {rightVisits}";
            if (_leftDistributionText) _leftDistributionText.text = $"Dist: {leftDistribution}";
            if (_rightDistributionText) _rightDistributionText.text = $"Dist: {rightDistribution}";
        }

        // result: 1 = left win, -1 = right win, 0 = tie
        public void HighlightRoundResult(int result)
        {
            StopAllCoroutines();
            StartCoroutine(DoHighlight(result));
        }

        private IEnumerator DoHighlight(int result)
        {
            // set initial colors based on result
            if (_leftMoveText) _leftMoveText.color = (result == 1) ? _winColor : (result == 0 ? _tieColor : _leftColor);
            if (_rightMoveText) _rightMoveText.color = (result == -1) ? _winColor : (result == 0 ? _tieColor : _rightColor);

            // pulse status text scale for clarity
            if (_statusText)
            {
                Vector3 origScale = _statusText.transform.localScale;
                Vector3 targetScale = origScale * 1.15f;
                float half = _flashDuration * 0.5f;
                float t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    _statusText.transform.localScale = Vector3.Lerp(origScale, targetScale, t / half);
                    yield return null;
                }
                t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    _statusText.transform.localScale = Vector3.Lerp(targetScale, origScale, t / half);
                    yield return null;
                }

                _statusText.transform.localScale = origScale;
            }

            // wait then reset colors
            yield return new WaitForSeconds(_flashDuration * 0.2f);
            if (_leftMoveText) _leftMoveText.color = _leftColor;
            if (_rightMoveText) _rightMoveText.color = _rightColor;
        }
    }
}
