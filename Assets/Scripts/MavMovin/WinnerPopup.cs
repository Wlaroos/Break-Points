using UnityEngine;
using UnityEngine.UI;

public class WinnerPopup : MonoBehaviour
{
    
    [SerializeField] private Sprite[] _horseImages;
    [SerializeField] private Sprite[] _horseNameTags;
    private Image _horseImage;
    private Image _horseNameTag;
    private CanvasGroup _canvasGroup;
    private ParticleSystem _confettiParticles01;
    private ParticleSystem _confettiParticles02;
    private AudioSource _audioSource;

    private void Awake() 
    {
        _horseImage = GameObject.Find("HorseImage").GetComponent<Image>();
        _horseNameTag = GameObject.Find("HorseNameTag").GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _confettiParticles01 = GameObject.Find("ConfettiParticles01").GetComponent<ParticleSystem>();
        _confettiParticles02 = GameObject.Find("ConfettiParticles02").GetComponent<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>();
    }

    public void ShowWinnerPopup(int horseIndex)
    {
        _horseImage.sprite = _horseImages[horseIndex];
        _horseNameTag.sprite = _horseNameTags[horseIndex];
        _canvasGroup.alpha = 1;

        _confettiParticles01.Play();
        _confettiParticles02.Play();
        _audioSource.Play();
    }

    public void HideWinnerPopup()
    {
        _canvasGroup.alpha = 0; // Hide the popup

        _confettiParticles01.Stop(); // Stop confetti particles
        _confettiParticles02.Stop();
        _audioSource.Stop(); // Stop audio
    }
}
