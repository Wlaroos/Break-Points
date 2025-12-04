using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class MavMovinBet : MonoBehaviour
{
    [SerializeField] private Image _whoImage;
    [SerializeField] private Image _isImage;
    [SerializeField] private Image _fasterImage;
    [SerializeField] private Button[] _horseButtons;
    [SerializeField] private Button[] _betButtons;
    [SerializeField] private Color32[] _horseColors;
    [SerializeField] private Slider _betSlider;
    [SerializeField] private TextMeshProUGUI _betAmountText;
    [SerializeField] private TextMeshProUGUI _timerText;

    // Particle prefabs to instantiate as children of the UI images
    [SerializeField] private GameObject _smallParticles;
    [SerializeField] private GameObject _bigParticles;
    [SerializeField] private float _particleLifetime = 3f;

    // Shake target (e.g. main camera or canvas root) and parameters
    [SerializeField] private Transform _shakeTarget;
    [SerializeField] private float _shakeDuration = 0.5f;
    [SerializeField] private float _shakeMagnitude = 8f;

    [SerializeField] private int _totalCoins = 500;

    private List<TimeTrigger> _triggers;

    private void Awake()
    {
        _betSlider = _betSlider ?? GetComponentInChildren<Slider>();
        _betAmountText = _betAmountText;
        _betSlider.value = 0f;
        UpdateBetAmountText();

        SetImageState(_whoImage, false);
        SetImageState(_isImage, false);
        SetImageState(_fasterImage, false);

        DisableBetting();

        InitializeTriggers();

        StartCoroutine(TimerCoroutine());
    }

    private void OnEnable()
    {
        if (_betSlider != null)
            _betSlider.onValueChanged.AddListener(UpdateBetAmountText);

        if (_horseButtons != null)
        {
            for (int i = 0; i < _horseButtons.Length; i++)
            {
                int index = i; // capture
                if (_horseButtons[i] != null)
                    _horseButtons[i].onClick.AddListener(() => OnHorseButtonClicked(index));
            }
        }
    }

    private void OnDisable()
    {
        if (_betSlider != null)
            _betSlider.onValueChanged.RemoveAllListeners();

        if (_horseButtons != null)
        {
            foreach (var btn in _horseButtons)
                if (btn != null)
                    btn.onClick.RemoveAllListeners();
        }
    }

    private void UpdateBetAmountText(float _ = 0f)
    {
        var betAmount = Mathf.RoundToInt(_betSlider.value * _totalCoins);
        if (_betAmountText != null)
            _betAmountText.text = $"{betAmount} / {_totalCoins}";
    }

    private void OnHorseButtonClicked(int selectedHorseIndex)
    {
        UpdateColors(selectedHorseIndex);
    }

    private void UpdateColors(int selectedHorseIndex = -1)
    {
        Color color = Color.white;
        if (selectedHorseIndex >= 0 && selectedHorseIndex < _horseColors.Length)
            color = _horseColors[selectedHorseIndex];

        // get slider background and handle images once
        Image sliderBackground = null;
        Image sliderHandle = null;
        if (_betSlider != null)
        {
            var bg = _betSlider.transform.Find("Background");
            if (bg != null) sliderBackground = bg.GetComponent<Image>();
            var handleArea = _betSlider.transform.Find("HandleSlideArea");
            if (handleArea != null) sliderHandle = handleArea.GetComponentInChildren<Image>();
        }

        if (_betButtons != null)
        {
            foreach (var button in _betButtons)
            {
                if (button == null) continue;
                var img = button.GetComponent<Image>();
                if (img != null) img.color = color;
                if (sliderBackground != null) sliderBackground.color = color;
                if (sliderHandle != null) sliderHandle.color = color;
            }
        }
    }

    private IEnumerator TimerCoroutine()
    {
        float timeLeft = 20f;
        const float step = 0.5f;

        while (timeLeft > 0f)
        {
            // execute any triggers whose time is >= current timeLeft and not yet fired
            foreach (var t in _triggers)
            {
                if (!t.Fired && timeLeft <= t.Time)
                {
                    t.Execute();
                }
            }

            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(timeLeft).ToString();

            yield return new WaitForSeconds(step);
            timeLeft -= step;
        }

        if (_timerText != null) _timerText.text = "0";
        DisableBetting();
        SceneManager.LoadScene("MavMovin");
    }

    private void InitializeTriggers()
    {
        _triggers = new List<TimeTrigger>
        {
            new TimeTrigger(18f, () =>
            {
                SetImageState(_whoImage, true);
                SpawnParticles(_smallParticles, _whoImage?.transform);
                StartShake();
            }),
            new TimeTrigger(16f, () =>
            {
                SetImageState(_isImage, true);
                SpawnParticles(_smallParticles, _isImage?.transform);
                StartShake();
            }),
            new TimeTrigger(14f, () =>
            {
                SetImageState(_fasterImage, true);
                SpawnParticles(_smallParticles, _fasterImage?.transform);
                SpawnParticles(_bigParticles, _fasterImage?.transform);
                EnableBetting();
                StartShake();
            })
        };
    }

    private void SetImageState(Image img, bool enabled)
    {
        if (img != null) img.enabled = enabled;
    }

    private void StartShake()
    {
        if (_shakeTarget != null && _shakeDuration > 0f && _shakeMagnitude > 0f)
            StartCoroutine(Shake(_shakeDuration, _shakeMagnitude));
    }

    private void SpawnParticles(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null) return;
        var instance = Instantiate(prefab, parent);
        if (instance.transform is RectTransform rt)
            rt.SetAsLastSibling();
        Destroy(instance, _particleLifetime);
        var ps = instance.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        if (_shakeTarget == null || duration <= 0f || magnitude <= 0f) yield break;
        Vector3 originalPos = _shakeTarget.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            _shakeTarget.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeTarget.localPosition = originalPos;
    }

    private void DisableBetting()
    {
        SetInteractable(_horseButtons, false);
        SetInteractable(_betButtons, false);
        if (_betSlider != null) _betSlider.interactable = false;
    }

    private void EnableBetting()
    {
        SetInteractable(_horseButtons, true);
        SetInteractable(_betButtons, true);
        if (_betSlider != null) _betSlider.interactable = true;
    }

    private void SetInteractable(IEnumerable<Button> buttons, bool value)
    {
        if (buttons == null) return;
        foreach (var b in buttons)
            if (b != null) b.interactable = value;
    }

    // helper trigger class
    private class TimeTrigger
    {
        public float Time { get; }
        private readonly Action _action;
        public bool Fired { get; private set; }

        public TimeTrigger(float time, Action action)
        {
            Time = time;
            _action = action;
            Fired = false;
        }

        public void Execute()
        {
            if (Fired) return;
            _action?.Invoke();
            Fired = true;
        }
    }
}
