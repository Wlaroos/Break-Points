using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horse : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float _startMoveChance = 0.5f;
    public float MoveChance => _startMoveChance;
    [SerializeField, Range(0, 1)] private float _startWaitChance = 0.5f;
    public float WaitChance => _startWaitChance;
    [SerializeField] private int _startMoveDistance = 1;
    public int StartMoveDistance => _startMoveDistance;
    [Space]
    [Space]
    [Space]
    [SerializeField, Range(0, 1)] private float _middleMoveChance = 0.5f;
    public float MiddleMoveChance => _middleMoveChance;
    [SerializeField, Range(0, 1)] private float _middleWaitChance = 0.5f;
    public float MiddleWaitChance => _middleWaitChance;
    [SerializeField] private int _middleMoveDistance = 2;
    public int MiddleMoveDistance => _middleMoveDistance;
    [Space]
    [Space]
    [Space]
    [SerializeField, Range(0, 1)] private float _finishMoveChance = 0.5f;
    public float FinishMoveChance => _finishMoveChance;
    [SerializeField, Range(0, 1)] private float _finishWaitChance = 0.5f;
    public float FinishWaitChance => _finishWaitChance;
    [SerializeField] private int _finishMoveDistance = 3;
    public int FinishMoveDistance => _finishMoveDistance;
    [Space]
    [Space]
    [Space]
    [SerializeField] private Color _displayColor;
    public Color DisplayColor => _displayColor;

    private float _moveLerpDuration;

    private Coroutine _moveCoroutine;

    private Animator _anim;

    // Ensure serialized values are valid in the editor and always sum to 1.0
    void OnValidate()
    {
        _startMoveChance = Mathf.Clamp01(_startMoveChance);
        _startWaitChance = 1f - _startMoveChance;

        _middleMoveChance = Mathf.Clamp01(_middleMoveChance);
        _middleWaitChance = 1f - _middleMoveChance;

        _finishMoveChance = Mathf.Clamp01(_finishMoveChance);
        _finishWaitChance = 1f - _finishMoveChance;
    }

    void Awake()
    {
        _moveLerpDuration = FindObjectOfType<GameManager>().TickTimeSeconds / 2;

        _anim = GetComponent<Animator>();
    }

    public void MoveTo(Vector3 worldPos)
    {
        // if duration is zero, teleport immediately
        if (_moveLerpDuration <= 0f)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            transform.position = worldPos;
            return;
        }

        // start smooth move coroutine
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveToCoroutine(worldPos, _moveLerpDuration));
    }

    private IEnumerator MoveToCoroutine(Vector3 target, float duration)
    {
        _anim?.SetBool("isMoving", true);
        
        Vector3 start = transform.position;
        if (duration <= 0f)
        {
            transform.position = target;
            _moveCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        _moveCoroutine = null;

        _anim?.SetBool("isMoving", false);
    }
}
