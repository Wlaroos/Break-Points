using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MavMovin
{
    public class GameCanvasManager : MonoBehaviour
    {
        [Header("Main message")]
        [SerializeField] private TextMeshProUGUI _mainTextTMP;

        // Show a transient message in the main text area.
        public void ShowMessage(string message, Color? color = null)
        {
            SetMainText(message, color);
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
}