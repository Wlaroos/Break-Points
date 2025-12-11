using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CupShuffle
{
    public class GameManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<Transform> _cups;      // Cup transforms (the moving objects)
        [SerializeField] private Transform _ball;           // Ball object
        [SerializeField] private TextMeshProUGUI _countdownText;       // UI Text for countdown
        [SerializeField] private List<Button> _guessButtons; // Buttons for player guesses

        [Header("Shuffle Settings")]
        [SerializeField] private float _shuffleSpeed = 2f;  // Higher = faster swap animation
        [SerializeField] private int _shuffleCount = 10;    // Number of swap operations
        [SerializeField] private float _ballOffsetY = 0.5f; // Vertical offset of ball relative to cup

        [Header("Evaluation Settings")]
        [SerializeField] private float _evaluationDuration = 10f; // Time the player has to change their guess

        [Header("Game Objects")]
        [SerializeField] private Transform _selectionMarker;
        [SerializeField] private Transform[] _thingsToHideAndShow;

        [Header("Betting")]
        [SerializeField] private BettingMenu _bettingMenu;

        private int _ballCupIndex;        // Index of the cup hiding the ball
        private bool _isShuffling = false;
        private bool _isGuessing = false;

        private List<int> _cupOrder;      // Tracks the final order of the cups after shuffling

        // New fields for evaluation phase
        private int _lastGuessIndex = -1;
        private Coroutine _evaluationCoroutine;

        private void Start()
        {
            if (!ValidateSetup())
                return;

            if (_bettingMenu == null)
            {
                Debug.LogError("BettingMenu reference is missing on GameManager.");
                return;
            }

            StartNewRound();
        }

        private void Update()
        {
            // Disable manual input during guessing phase
        }

        private bool ValidateSetup()
        {
            if (_cups == null || _cups.Count < 3)
            {
                Debug.LogError("You need at least 3 cups assigned to 'cups' in the inspector.");
                return false;
            }

            if (_ball == null)
            {
                Debug.LogError("Ball reference is missing.");
                return false;
            }

            if (_countdownText == null)
            {
                Debug.LogError("Countdown Text reference is missing.");
                return false;
            }

            if (_guessButtons == null || _guessButtons.Count < 3)
            {
                Debug.LogError("You need 3 buttons assigned to 'guessButtons' in the inspector.");
                return false;
            }

            return true;
        }

        public void StartNewRound()
        {
            if (_isShuffling)
                return;

            // Cancel any leftover evaluation coroutine from previous round
            if (_evaluationCoroutine != null)
            {
                StopCoroutine(_evaluationCoroutine);
                _evaluationCoroutine = null;
            }
            _lastGuessIndex = -1;
            _isGuessing = false;

            ResetUI();
            StartCoroutine(CountdownAndStart());
        }

        private void ResetUI()
        {
            _countdownText.gameObject.SetActive(true);
            foreach (var button in _guessButtons)
            {
                button.gameObject.SetActive(false);
            }
            foreach (var obj in _thingsToHideAndShow)
            {
                if (obj != null)
                    obj.gameObject.SetActive(false);
            }
            _selectionMarker.gameObject.SetActive(false);
        }

        private IEnumerator CountdownAndStart()
        {
            _countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            _countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            _countdownText.text = "1";
            yield return new WaitForSeconds(1f);
            _countdownText.text = "Go!";
            yield return new WaitForSeconds(0.5f);

            _countdownText.gameObject.SetActive(false);
            StartCoroutine(PlaceBallAndShuffle());
        }

        private IEnumerator PlaceBallAndShuffle()
        {
            // Move the ball into the cup
            _ballCupIndex = Random.Range(0, _cups.Count);
            Transform targetCup = _cups[_ballCupIndex];

            Vector3 startPosition = _ball.position;
            Vector3 targetPosition = targetCup.position + Vector3.up * _ballOffsetY;

            float duration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _ball.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
                yield return null;
            }

            _ball.SetParent(targetCup);
            _ball.localPosition = Vector3.up * _ballOffsetY;

            // Start shuffling
            StartCoroutine(ShuffleCups());
        }

        private IEnumerator ShuffleCups()
        {
            _isShuffling = true;
            _isGuessing = false;

            // Initialize the cup order
            _cupOrder = new List<int>();
            for (int i = 0; i < _cups.Count; i++)
            {
                _cupOrder.Add(i);
            }

            for (int i = 0; i < _shuffleCount; i++)
            {
                // Pick two different cups to swap
                int cupAIndex = Random.Range(0, _cups.Count);
                int cupBIndex = Random.Range(0, _cups.Count);
                while (cupAIndex == cupBIndex)
                {
                    cupBIndex = Random.Range(0, _cups.Count);
                }

                Transform cupA = _cups[cupAIndex];
                Transform cupB = _cups[cupBIndex];

                Vector3 cupAPosition = cupA.position;
                Vector3 cupBPosition = cupB.position;

                float duration = 1f / Mathf.Max(0.01f, _shuffleSpeed);
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / duration);

                    cupA.position = Vector3.Lerp(cupAPosition, cupBPosition, t);
                    cupB.position = Vector3.Lerp(cupBPosition, cupAPosition, t);

                    yield return null;
                }

                // Snap to final positions to avoid drift
                cupA.position = cupBPosition;
                cupB.position = cupAPosition;

                // Update the cup order
                int temp = _cupOrder[cupAIndex];
                _cupOrder[cupAIndex] = _cupOrder[cupBIndex];
                _cupOrder[cupBIndex] = temp;
            }

            _isShuffling = false;
            _isGuessing = true;

            ShowGuessButtons();
        }

        private void ShowGuessButtons()
        {
            // Sort the cups based on their x-position (left to right)
            List<Transform> sortedCups = new List<Transform>(_cups);
            sortedCups.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            _lastGuessIndex = -1;

            // Map the buttons to the sorted cups (left, middle, right)
            for (int i = 0; i < _guessButtons.Count; i++)
            {
                int index = _cups.IndexOf(sortedCups[i]); // Get the original index of the sorted cup
                _guessButtons[i].gameObject.SetActive(true);
                _guessButtons[i].onClick.RemoveAllListeners();

                // Each click just registers the latest guess (does NOT immediately evaluate)
                _guessButtons[i].onClick.AddListener(() => RegisterGuess(index));
            }

            foreach (var obj in _thingsToHideAndShow)
            {
                if (obj != null)
                    obj.gameObject.SetActive(true);
            }
            _selectionMarker.gameObject.SetActive(true);

            // Start the betting menu sequence (WHO / IS / ER, particles, timer, etc.)
            if (_bettingMenu != null)
            {
                _bettingMenu.BeginBetting(OnBettingFinished);
            }
            else
            {
                Debug.LogWarning("BettingMenu not set, falling back to simple evaluation timer.");
                if (_evaluationCoroutine != null)
                {
                    StopCoroutine(_evaluationCoroutine);
                }
                _evaluationCoroutine = StartCoroutine(EvaluationPhase());
            }
        }

        // New: record the latest guess during the 10-second window
        private void RegisterGuess(int index)
        {
            if (!_isGuessing)
                return;

            _lastGuessIndex = index;
            Debug.Log($"Current guess set to cup {index + 1}");
            _countdownText.text = $"Guess: {index + 1}";

            // Move selection marker's X to match the chosen cup
            if (_selectionMarker != null && index >= 0 && index < _cups.Count)
            {
                Vector3 markerPos = _selectionMarker.position;
                markerPos.x = _cups[index].position.x;
                _selectionMarker.position = markerPos;
            }
        }

        // New: 10-second evaluation window; last guess is used when time is up
        private IEnumerator EvaluationPhase()
        {
            float elapsed = 0f;

            while (elapsed < _evaluationDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Time's up; stop guessing and evaluate the last choice
            _isGuessing = false;

            // Use the last guess, even if it was changed multiple times
            EvaluateGuess(_lastGuessIndex);

            _evaluationCoroutine = null;
        }

        private void EvaluateGuess(int guessedIndex)
        {
            // Disable buttons once we move to reveal phase
            foreach (var button in _guessButtons)
            {
                button.gameObject.SetActive(false);
            }

            // If the player never picked a valid cup, treat as a wrong guess
            if (guessedIndex < 0 || guessedIndex >= _cups.Count)
            {
                Debug.Log("Time's up! You didn't make a valid guess.");
                _countdownText.text = "Time's Up!";
                guessedIndex = -1; // Keep as invalid but still show the reveal
            }

            StartCoroutine(RevealBall(guessedIndex));
            ResetUI();
        }

        private IEnumerator RevealBall(int guessedIndex)
        {
            // Show the result of the guess
            if (guessedIndex == _ballCupIndex)
            {
                Debug.Log("Correct! You found the ball!");
                _countdownText.text = "Correct!";
            }
            else
            {
                Debug.Log($"Wrong! The ball was under cup {_ballCupIndex + 1}");
                _countdownText.text = "Wrong!";
            }

            // Detach the ball from the cup and move it slightly upward to reveal it
            _ball.SetParent(null);
            Vector3 revealPosition = _cups[_ballCupIndex].position + Vector3.up * (_ballOffsetY + 5f); // Move ball up to reveal
            Vector3 originalPosition = _ball.position;

            float duration = 0.5f; // Duration of the reveal animation
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _ball.position = Vector3.Lerp(originalPosition, revealPosition, elapsedTime / duration);
                yield return null;
            }

            // Wait for a moment to let the player see the revealed ball
            yield return new WaitForSeconds(2f);

            // Start a new round
            StartNewRound();
        }

        // Called by BettingMenu when its timer finishes
        private void OnBettingFinished()
        {
            _isGuessing = false;
            EvaluateGuess(_lastGuessIndex);
        }
    }
}
