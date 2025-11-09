using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

    public class GameCanvasManager : MonoBehaviour
    {
        [Header("Main message")]
        [SerializeField] private TextMeshProUGUI _mainTextTMP;

        [Header("Tick Pulse")]
        [SerializeField] private RectTransform _tickPulseRect; // assign a UI Image's RectTransform
        [SerializeField, Range(0.05f, 1f)] private float _minPulseScale = 0.25f;

        private Coroutine _pulseCoroutine;

        // Show a transient message in the main text area.
        public void ShowMessage(string message, Color? color = null)
        {
            SetMainText(message, color);
        }

        // Public API to trigger the tick pulse animation for a given duration.
        public void TriggerTickPulse(float duration)
        {
            if (_tickPulseRect == null || duration <= 0f) return;

            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);

            _pulseCoroutine = StartCoroutine(TickPulseCoroutine(duration));
        }

        private IEnumerator TickPulseCoroutine(float duration)
        {
            // start at full scale
            _tickPulseRect.localScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(1f, _minPulseScale, t);
                _tickPulseRect.localScale = new Vector3(scale, 1, 1);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Announce a winner (by index 0..3 or name). Updates per-horse status too.
        public void AnnounceWinner(int horseIndex, string horseName = null, Color? color = null)
        {
            string name = string.IsNullOrEmpty(horseName) ? $"Horse {horseIndex + 1}" : horseName;
            ShowMessage($"{name} wins!", color);
        }

        // Race lifecycle helpers
        public void ShowRaceStart()
        {
            ShowMessage("Race Start!");
        }

        public void ShowRaceEnd()
        {
            ShowMessage("Race Over");
        }

        // set the main text and optionally its color
        private void SetMainText(string message, Color? color)
        {
            if (_mainTextTMP != null)
            {
                _mainTextTMP.text = message;
                if (color.HasValue)
                    _mainTextTMP.color = color.Value;
                return;
            }
        }
    }