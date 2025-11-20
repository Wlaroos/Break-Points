using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
public class MinigameVote : MonoBehaviour
{
    // These should be listed as 0: Empty Left Cap, 1: Empty Middle, 2: Empty Right Cap
    // 3: Filled Left Cap, 4: Filled Middle, 5: Filled Right Cap

    [SerializeField] private Sprite[] _pipImagesSumoBall;
    [SerializeField] private Sprite[] _pipImagesMavMovin;

    [Header("UI Setup")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private RectTransform[] pipContainers; // one container per option
    [SerializeField] private GameObject pipPrefab; // must have an Image component
    [SerializeField] private TMP_Text[] optionRatioTexts; // optional: show ratio/percent per option

    [Header("Pips")]
    [SerializeField, Min(3)] private int pipCount = 3;
    [Tooltip("Per-option sprite sets (each set must have 6 sprites as documented). If empty, defaults use _pipImagesSumoBall and _pipImagesMavMovin for option 0 & 1.")]
    [SerializeField] private List<Sprite[]> pipSpriteSets = new List<Sprite[]>();

    [SerializeField, Min(0.01f)] private float pipSizeMultiplier = 1f;

    private int[] votes;
    private List<List<Image>> pipImagesPerOption = new List<List<Image>>();

    void Awake()
    {
        if (pipCount < 3) pipCount = 3;

        if (optionButtons == null || optionButtons.Length == 0)
        {
            Debug.LogWarning("No option buttons assigned.");
            return;
        }

        if (pipContainers == null || pipContainers.Length != optionButtons.Length)
        {
            Debug.LogError("pipContainers must be assigned and match optionButtons length.");
            return;
        }

        if (optionRatioTexts != null && optionRatioTexts.Length != optionButtons.Length)
            Debug.LogWarning("optionRatioTexts length does not match optionButtons length. Leave null or match lengths to display ratios.");

        if (pipPrefab == null)
        {
            Debug.LogError("pipPrefab must be assigned (UI GameObject with Image).");
            return;
        }

        EnsureSpriteSets();

        int optionCount = optionButtons.Length;
        votes = new int[optionCount];
        pipImagesPerOption = new List<List<Image>>(optionCount);

        for (int i = 0; i < optionCount; i++)
        {
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnVote(idx));

            ClearChildren(pipContainers[i]);

            var list = new List<Image>(pipCount);
            for (int p = 0; p < pipCount; p++)
            {
                var go = Instantiate(pipPrefab, pipContainers[i]);
                go.name = $"Pip_{idx}_{p}";
                Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one * pipSizeMultiplier;
                list.Add(img);
            }
            pipImagesPerOption.Add(list);
        }

        UpdatePips();
    }

    void OnValidate()
    {
        if (pipCount < 3) pipCount = 3;
    }

    public void OnVote(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= votes.Length) return;
        votes[optionIndex]++;
        UpdatePips();
    }

    public void ResetVotes()
    {
        for (int i = 0; i < votes.Length; i++) votes[i] = 0;
        UpdatePips();
    }

    private void UpdatePips()
    {
        int total = 0;
        foreach (var v in votes) total += v;

        int optionCount = pipImagesPerOption.Count;
        for (int opt = 0; opt < optionCount; opt++)
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

        Sprite[] sprites = (optionIndex < pipSpriteSets.Count) ? pipSpriteSets[optionIndex] : null;

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
        if (optionRatioTexts != null && opt < optionRatioTexts.Length && optionRatioTexts[opt] != null)
            optionRatioTexts[opt].text = total == 0 ? $"0%({count}/{total})" : $"{percent:P0} ({count}/{total})";
    }

    private void EnsureSpriteSets()
    {
        if (pipSpriteSets == null) pipSpriteSets = new List<Sprite[]>();

        int needed = optionButtons != null ? optionButtons.Length : 0;
        for (int i = pipSpriteSets.Count; i < needed; i++)
        {
            if (i == 0 && _pipImagesSumoBall != null && _pipImagesSumoBall.Length >= 6)
                pipSpriteSets.Add(_pipImagesSumoBall);
            else if (i == 1 && _pipImagesMavMovin != null && _pipImagesMavMovin.Length >= 6)
                pipSpriteSets.Add(_pipImagesMavMovin);
            else
                pipSpriteSets.Add(new Sprite[6]);
        }
    }

    private void ClearChildren(RectTransform rt)
    {
        for (int c = rt.childCount - 1; c >= 0; c--)
            Destroy(rt.GetChild(c).gameObject);
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
