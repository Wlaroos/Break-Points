using TMPro;
using UnityEngine;
using UnityEngine.UI; // Ensure you include this for UI components like Slider and Text

public class SliderText : MonoBehaviour
{
    [SerializeField] private Slider _betSlider; // Reference to the slider
    [SerializeField] private TextMeshProUGUI _betAmountText; // Reference to the text component
    [SerializeField] private int _totalCoins; // Total coins available

    private void Start()
    {
        // Add a listener to the slider to call UpdateBetAmountText whenever the value changes
        if (_betSlider != null)
            _betSlider.onValueChanged.AddListener(UpdateBetAmountText);

        // Initialize the text
        UpdateBetAmountText(_betSlider.value);
    }

    private void UpdateBetAmountText(float value)
    {
        var betAmount = Mathf.RoundToInt(value * _totalCoins);
        var percentage = Mathf.RoundToInt(value * 100); // Calculate the percentage
        if (_betAmountText != null)
            _betAmountText.text = $"{betAmount} / {_totalCoins} ({percentage}%)";
    }
}
