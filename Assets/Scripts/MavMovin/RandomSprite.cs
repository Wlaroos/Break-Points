using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] _sprites;
    private SpriteRenderer _sr;

    private void Awake() 
    {
        _sr = GetComponent<SpriteRenderer>();
        AssignRandomSprite();
    }

    private void AssignRandomSprite()
    {
        if (_sprites != null && _sprites.Length > 0)
        {
            int randomIndex = Random.Range(0, _sprites.Length);
            _sr.sprite = _sprites[randomIndex];
        }
    }
}
