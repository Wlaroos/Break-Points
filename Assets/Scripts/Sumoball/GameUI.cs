using UnityEngine;
using TMPro;

namespace FanExperiencePrototypes
{
    public class GameUI : MonoBehaviour
    {
    [SerializeField] private TextMeshProUGUI _leftMoveText;
    [SerializeField] private TextMeshProUGUI _rightMoveText;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI _statusText;

    public void ShowMoves(RPSMove left, RPSMove right)
    {
        if (_leftMoveText) _leftMoveText.text = "Left: " + left.ToString();
        if (_rightMoveText) _rightMoveText.text = "Right: " + right.ToString();
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
        }
    }
    }
}
