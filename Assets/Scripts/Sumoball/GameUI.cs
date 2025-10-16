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

    [Header("Visual Feedback")]
    [SerializeField] private Color _leftColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color _rightColor = new Color(1f, 0.6f, 0.6f);
    [SerializeField] private Color _winColor = Color.green;
    [SerializeField] private Color _tieColor = Color.yellow;
    [SerializeField] private float _flashDuration = 0.6f;

    [Header("Background / Icon Feedback (optional)")]
    [SerializeField] private Image _backgroundPanel; // semi-transparent panel to flash
    [SerializeField] private Image _resultIcon; // small icon Image to show win/lose/tie
    [SerializeField] private Sprite _winSprite;
    [SerializeField] private Sprite _loseSprite;
    [SerializeField] private Sprite _tieSprite;

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

    // result: 1 = left win, -1 = right win, 0 = tie
    public void HighlightRoundResult(int result)
    {
        StopAllCoroutines();
        StartCoroutine(DoHighlight(result));
        // additional visual: background + result icon
        StartCoroutine(DoResultVisual(result));
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

    // background flash + result icon pulse
    private IEnumerator DoResultVisual(int result)
    {
        if (_backgroundPanel == null && _resultIcon == null) yield break;

        // choose color & icon
        Color iconColor = Color.white;
        Sprite iconSprite = null;
        Color bgColor = Color.clear;
        if (result == 1)
        {
            iconSprite = _winSprite;
            bgColor = _winColor;
            iconColor = _winColor;
        }
        else if (result == -1)
        {
            iconSprite = _loseSprite ?? _winSprite;
            bgColor = _rightColor;
            iconColor = _rightColor;
        }
        else
        {
            iconSprite = _tieSprite ?? _winSprite;
            bgColor = _tieColor;
            iconColor = _tieColor;
        }

        float duration = Mathf.Max(0.05f, _flashDuration);
        float half = duration * 0.5f;

        // background fade in/out
        if (_backgroundPanel)
        {
            Color start = _backgroundPanel.color;
            Color mid = bgColor;
            mid.a = 0.35f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                _backgroundPanel.color = Color.Lerp(start, mid, t / half);
                yield return null;
            }
        }

        // icon pulse
        if (_resultIcon)
        {
            _resultIcon.gameObject.SetActive(true);
            _resultIcon.sprite = iconSprite;
            _resultIcon.color = iconColor;
            _resultIcon.transform.localScale = Vector3.zero;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                float scale = Mathf.Lerp(0f, 1.25f, p);
                _resultIcon.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            _resultIcon.transform.localScale = Vector3.one;
        }

        // hold a short moment
        yield return new WaitForSeconds(0.12f);

        // fade out background + hide icon
        if (_backgroundPanel)
        {
            Color from = _backgroundPanel.color;
            Color to = from;
            to.a = 0f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                _backgroundPanel.color = Color.Lerp(from, to, t / half);
                yield return null;
            }
            _backgroundPanel.color = to;
        }

        if (_resultIcon)
        {
            _resultIcon.gameObject.SetActive(false);
        }
    }
    }
}
