using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MavMovinBet : MonoBehaviour
{
    [SerializeField] private Image _whoImage;
    [SerializeField] private Image _isImage;
    [SerializeField] private Image _fasterImage;
    [SerializeField] private Button[] _horseButtons;
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

    private float _totalCoins = 500;

    private void Awake()
    {
        _betSlider.value = 0;
        UpdateBetAmountText();

        _whoImage.enabled = false;
        _isImage.enabled = false;
        _fasterImage.enabled = false;

        DisableBetting();

        StartCoroutine(TimerCoroutine());
    }

    private void OnEnable()
    {
        _betSlider.onValueChanged.AddListener(delegate { UpdateBetAmountText(); });
    }
    private void OnDisable()
    {
        _betSlider.onValueChanged.RemoveAllListeners();
    }

    private void UpdateBetAmountText()
    {
        float betAmount = _betSlider.value * _totalCoins;
        _betAmountText.text = $"{Mathf.RoundToInt(betAmount)} / {_totalCoins}";
    }

    private IEnumerator TimerCoroutine()
    {
        float timeLeft = 20;
        while (timeLeft > 0)
        {
            switch (timeLeft)
            {
                case 18f:
                    _whoImage.enabled = true;
                    SpawnParticles(_smallParticles, _whoImage.transform);
                    StartCoroutine(Shake(_shakeDuration, _shakeMagnitude));
                    break;
                case 16f:
                    _isImage.enabled = true;
                    SpawnParticles(_smallParticles, _isImage.transform);
                    StartCoroutine(Shake(_shakeDuration, _shakeMagnitude));
                    break;
                case 14f:
                    _fasterImage.enabled = true;
                    SpawnParticles(_smallParticles, _fasterImage.transform);
                    SpawnParticles(_bigParticles, _fasterImage.transform);
                    EnableBetting();
                    StartCoroutine(Shake(_shakeDuration, _shakeMagnitude));
                    break;
            }

            _timerText.text = timeLeft.ToString();
            yield return new WaitForSeconds(0.5f);
            timeLeft -= 0.5f;
        }
        _timerText.text = "0";
        DisableBetting();
    }

    private void SpawnParticles(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null) return;
        var instance = Instantiate(prefab, parent);
        // ensure it appears on top of the UI element
        if (instance.transform is RectTransform rt)
            rt.SetAsLastSibling();
        Destroy(instance, _particleLifetime);
        // if prefab has a ParticleSystem, play immediately
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
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            _shakeTarget.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeTarget.localPosition = originalPos;
    }

    private void DisableBetting()
    {
        foreach (var button in _horseButtons)
        {
            button.interactable = false;
        }
        _betSlider.interactable = false;
    }

    private void EnableBetting()
    {
        foreach (var button in _horseButtons)
        {
            button.interactable = true;
        }
        _betSlider.interactable = true;
    }
}
