using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
 
public class MinigameVote : MonoBehaviour
{
    // These should be listed as 0: Empty Left Cap, 1: Empty Middle, 2: Empty Right Cap
    // 3: Filled Left Cap, 4: Filled Middle, 5: Filled Right Cap

    [System.Serializable]
    public class OptionConfig
    {
        public string name; // optional display
        public Button optionButton;
        public RectTransform pipContainer;
        public TMP_Text ratioText;
        public Sprite[] spriteSet = new Sprite[6];
        public string sceneName;
    }

    [Header("UI Setup")]
    [SerializeField, Min(1)] private int optionCount = 2;
    [SerializeField] private List<OptionConfig> options = new List<OptionConfig>();

    [Header("Pips")]
    [SerializeField, Min(3)] private int pipCount = 3;
    [Tooltip("Per-option sprite sets (each set must have 6 sprites as documented). If empty, defaults use _pipImagesSumoBall and _pipImagesMavMovin for option 0 & 1.")]
    [SerializeField] private float pipSizeMultiplier = 1f;

    [Header("Voting Timer")]
    [SerializeField, Min(0.1f)] private float voteDuration = 10f;
    [SerializeField] private TMP_Text timerText; // optional: show remaining seconds

    private Coroutine voteCoroutine;
    private int[] votes;
    private List<List<Image>> pipImagesPerOption = new List<List<Image>>();

    void Awake()
    {
        if (pipCount < 3) pipCount = 3;

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("No options assigned.");
            return;
        }

        // basic validation
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].optionButton == null)
                Debug.LogWarning($"Option {i} button not assigned.");
            if (options[i].pipContainer == null)
                Debug.LogError($"Option {i} pipContainer not assigned.");
            if (options[i].spriteSet == null || options[i].spriteSet.Length < 6)
                options[i].spriteSet = new Sprite[6];
        }

        if (options.Exists(o => o.pipContainer == null))
            return;

        EnsureSpriteSets();

        int optCount = options.Count;
        votes = new int[optCount];
        pipImagesPerOption = new List<List<Image>>(optCount);

        for (int i = 0; i < optCount; i++)
        {
            int idx = i;
            var opt = options[i];
            if (opt.optionButton != null)
            {
                opt.optionButton.onClick.RemoveAllListeners();
                opt.optionButton.onClick.AddListener(() => OnVote(idx));
            }

            ClearChildren(opt.pipContainer);

            var list = new List<Image>(pipCount);
            for (int p = 0; p < pipCount; p++)
            {
                var go = Instantiate(optButtonPrefabSafe(), opt.pipContainer);
                go.name = $"Pip_{idx}_{p}";
                Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one * pipSizeMultiplier;
                list.Add(img);
            }
            pipImagesPerOption.Add(list);
        }

        UpdatePips();

        // start vote timer
        if (voteCoroutine != null) StopCoroutine(voteCoroutine);
        voteCoroutine = StartCoroutine(VoteTimer());
    }

    // helper to return a fallback pip prefab from existing children if pipPrefab isn't serialized in options (keeps compatibility)
    private GameObject optButtonPrefabSafe()
    {
        // if you used a pipPrefab previously, assign it here; for now create a simple GameObject with Image
        var go = new GameObject("Pip");
        var img = go.AddComponent<Image>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(16, 16);
        return go;
    }

    void OnValidate()
    {
        if (pipCount < 3) pipCount = 3;
        optionCount = Mathf.Max(1, optionCount);

        if (options == null) options = new List<OptionConfig>();
        while (options.Count < optionCount) options.Add(new OptionConfig());
        if (options.Count > optionCount) options.RemoveRange(optionCount, options.Count - optionCount);

        // ensure each option has sprite array of length 6
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == null) options[i] = new OptionConfig();
            if (options[i].spriteSet == null || options[i].spriteSet.Length != 6)
                options[i].spriteSet = new Sprite[6];
        }
    }

    public void OnVote(int optionIndex)
    {
        if (optionIndex < 0 || votes == null || optionIndex >= votes.Length) return;
        votes[optionIndex]++;
        UpdatePips();
    }

    public void ResetVotes()
    {
        if (votes == null) return;
        for (int i = 0; i < votes.Length; i++) votes[i] = 0;
        UpdatePips();

        // restart timer
        if (voteCoroutine != null) StopCoroutine(voteCoroutine);
        voteCoroutine = StartCoroutine(VoteTimer());
    }

    private IEnumerator VoteTimer()
    {
        float t = Mathf.Max(0.01f, voteDuration);
        while (t > 0f)
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(t).ToString();
            t -= Time.deltaTime;
            yield return null;
        }
        if (timerText != null) timerText.text = "0";
        FinishVoting();
    }

    private void FinishVoting()
    {
        if (votes == null || votes.Length == 0)
        {
            Debug.LogWarning("No votes/options configured.");
            return;
        }

        int bestIndex = 0;
        for (int i = 1; i < votes.Length; i++)
            if (votes[i] > votes[bestIndex]) bestIndex = i;

        Debug.Log($"Voting finished. Winning option: {bestIndex} with {votes[bestIndex]} votes.");

        // disable further voting
        if (options != null)
        {
            foreach (var o in options)
                if (o?.optionButton != null)
                    o.optionButton.interactable = false;
        }

        // show winner in the timerText for 1 second, then load scene
        if (timerText != null)
            StartCoroutine(ShowWinnerThenLoad(bestIndex));
        else
        {
            if (options != null && bestIndex < options.Count && !string.IsNullOrEmpty(options[bestIndex].sceneName))
                SceneManager.LoadScene($"{options[bestIndex].sceneName}");
        }
    }

    // show winner text in timerText for 1 second, then load the configured scene (if any)
    private IEnumerator ShowWinnerThenLoad(int bestIndex)
    {
        if (timerText == null)
            yield break;

        string winnerLabel = (options != null && bestIndex < options.Count && !string.IsNullOrEmpty(options[bestIndex].name))
            ? options[bestIndex].name
            : $"Option {bestIndex}";

        timerText.text = winnerLabel;
        yield return new WaitForSeconds(1f);

        if (options != null && bestIndex < options.Count && !string.IsNullOrEmpty(options[bestIndex].sceneName))
            SceneManager.LoadScene($"{options[bestIndex].sceneName}");
    }

    private void UpdatePips()
    {
        if (votes == null) return;
        int total = 0;
        foreach (var v in votes) total += v;

        int optCount = pipImagesPerOption.Count;
        for (int opt = 0; opt < optCount; opt++)
        {
            float percent = total == 0 ? 0f : (float)votes[opt] / total;
            SetPipsForOption(opt, percent);
            UpdateOptionUI(opt, percent, votes[opt], total);
        }
    }

    private void SetPipsForOption(int optionIndex, float percent)
    {
        if (optionIndex < 0 || optionIndex >= pipImagesPerOption.Count) return;

        int fillCount = Mathf.RoundToInt(percent * pipCount);
        fillCount = Mathf.Clamp(fillCount, 0, pipCount);

        Sprite[] sprites = (options != null && optionIndex < options.Count) ? options[optionIndex].spriteSet : null;

        var list = pipImagesPerOption[optionIndex];
        for (int p = 0; p < pipCount; p++)
        {
            bool filled = p < fillCount;
            Image img = list[p];
            img.sprite = GetSpriteForPosition(sprites, p, pipCount, filled);
            img.SetNativeSize();
        }
    }

    private void UpdateOptionUI(int opt, float percent, int count, int total)
    {
        if (options != null && opt < options.Count)
        {
            var ratioText = options[opt].ratioText;
            if (ratioText != null)
                ratioText.text = total == 0 ? $"0%({count}/{total})" : $"{percent:P0} ({count}/{total})";
        }
    }

    private void EnsureSpriteSets()
    {
        if (options == null) options = new List<OptionConfig>();

        int needed = optionCount;
        while (options.Count < needed) options.Add(new OptionConfig());

        for (int i = 0; i < needed; i++)
        {
            if (options[i].spriteSet == null || options[i].spriteSet.Length < 6)
            {
                options[i].spriteSet = new Sprite[6];
            }
        }
    }

    private void ClearChildren(RectTransform rt)
    {
        if (rt == null) return;
        for (int c = rt.childCount - 1; c >= 0; c--)
            DestroyImmediate(rt.GetChild(c).gameObject);
    }

    private Sprite GetSpriteForPosition(Sprite[] set, int pipIndex, int totalPips, bool filled)
    {
        int spriteIndex;
        bool isLeft = pipIndex == 0;
        bool isRight = pipIndex == totalPips - 1;
        bool isMiddle = !isLeft && !isRight;

        if (!filled)
        {
            spriteIndex = isLeft ? 0 : isMiddle ? 1 : 2;
        }
        else
        {
            spriteIndex = isLeft ? 3 : isMiddle ? 4 : 5;
        }

        if (set != null && spriteIndex >= 0 && spriteIndex < set.Length)
            return set[spriteIndex];

        return null;
    }

    public void SetPipSizeMultiplier(float factor)
    {
        pipSizeMultiplier = Mathf.Max(0.01f, factor);
        foreach (var list in pipImagesPerOption)
        {
            foreach (var img in list)
            {
                if (img == null) continue;
                var rt = img.rectTransform;
                rt.localScale = Vector3.one * pipSizeMultiplier;
            }
        }
    }
}
