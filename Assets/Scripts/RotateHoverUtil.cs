using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateHoverUtil : MonoBehaviour
{
    [SerializeField] private bool usesLocalTime = true;
    private float localTime;
    [Space(10)]
    [SerializeField] private bool _canRotate;
    [SerializeField] private Vector3 _degrees = new Vector3(0f,0f,15f);
    [SerializeField] private float _rotateFrequency = 1f;
    [Space(10)]
    [SerializeField] private bool _canHover;
    [SerializeField] private float _hoverAmplitude = 0.5f;
    [SerializeField] private float _hoverFrequency = 1f;

    private Vector3 _posOffset = new Vector3();
    private Vector3 _tempPos = new Vector3();

    private float _rotateSine;
    private float _hoverSine;

    private Vector3 _rotOffset = new Vector3();
    private Vector3 _tempRot = new Vector3();

    void Awake()
    {
        _posOffset = transform.localPosition;
        _rotOffset = transform.localEulerAngles;
        localTime = Random.Range(0,1000);
        _hoverFrequency = Random.Range(_hoverFrequency, _hoverFrequency+.25f);
    }

    void Update()
    {
        if(usesLocalTime)
        {
            localTime += Time.deltaTime;
        }
        else
        {
            localTime = Time.fixedTime;
        }

        if (_canRotate)
        {
            float _rotateSine = Mathf.Sin(localTime * Mathf.PI * 2f / _rotateFrequency);
            _tempRot = _rotOffset;
            _tempRot += _degrees * _rotateSine;
            transform.localEulerAngles = _tempRot;
        }

        if (_canHover)
        {
            float _hoverSine = Mathf.Sin(localTime * Mathf.PI * 2f / _hoverFrequency);

            _tempPos = _posOffset;

            _tempPos.y += (_hoverSine * _hoverAmplitude);

            transform.localPosition = _tempPos;
        }
    }
}
