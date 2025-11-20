using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    private List<Horse> _horses = new List<Horse>();

    [Header("Follow Mode")]
    [Tooltip("If true the camera follows the horizontal average X of all horses. If false follows the horse closest to the finish (max X).")]
    [SerializeField] private bool _followAverageCenter = false;

    [Header("Movement / Rotation")]
    [SerializeField, Range(0.1f, 10f)] private float _horizontalSmooth = 3f;
    [SerializeField, Range(0.1f, 10f)] private float _rotationSmooth = 4f;
    [SerializeField] private bool _smoothLookAt = false;

    [Header("Zoom")]
    [SerializeField, Range(0.01f, 20f)] private float _zoomSmooth = 3f;
    [SerializeField] private float _minOrthoSize = 3f;
    [SerializeField] private float _maxOrthoSize = 8f;
    
    private Transform _startLineTransform;
    private Transform _finishLineTransform;

    [Header("Clamping (X)")]
    [Tooltip("X clamp limits. Use x = minX, y = maxX. If x > y clamping is disabled.")]
    [SerializeField] private Vector2 _clampDistance = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

    private Camera _cam;

    // allow other managers to populate the camera's horse list at runtime
    public void SetHorses(List<Horse> horses)
    {
        _horses = (horses != null) ? new List<Horse>(horses) : new List<Horse>();
    }

    public void AddHorse(Horse horse)
    {
        if (_horses == null) _horses = new List<Horse>();
        if (horse != null && !_horses.Contains(horse)) _horses.Add(horse);
    }

    // allow external code to populate start/finish line references
    public void SetStartFinish(Transform start, Transform finish)
    {
        _startLineTransform = start;
        _finishLineTransform = finish;
    }

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
            Debug.LogWarning("CameraController: No Camera component found on this GameObject.");
    }

    void Update()
    {
        if (_horses == null || _horses.Count == 0) return;

        // compute target X
        float targetX = _followAverageCenter ? GetAverageHorseX() : GetLeadHorseX();

        // target position (only move horizontally)
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);

        // slerp position (spherical interpolation for a smooth arc-like move)
        transform.position = Vector3.Slerp(transform.position, targetPos, Time.deltaTime * _horizontalSmooth);

        // optional smooth rotation to look at target horizontally (keeps camera upright)
        if (_smoothLookAt)
        {
            Vector3 lookTarget = new Vector3(targetX, transform.position.y, transform.position.z - 10f); // look-forward target
            Quaternion desired = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * _rotationSmooth);
        }

        // Zoom based on progress toward finish
        float progress = ComputeProgressTowardFinish(targetX); // 0..1
        ApplyZoom(progress);

        // Apply clamping so camera edges don't cross the limits in _clampDistance
        if (_clampDistance.x <= _clampDistance.y)
        {
            // For orthographic cameras clamp camera center so the view edge stays inside _clampDistance
            if (_cam != null && _cam.orthographic)
            {
                float halfWidth = _cam.orthographicSize * _cam.aspect;
                float minX = _clampDistance.x + halfWidth;
                float maxX = _clampDistance.y - halfWidth;
                float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
                transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            }
            else
            {
                // Fallback for perspective/no-camera: clamp the center (approximation)
                float clampedX = Mathf.Clamp(transform.position.x, _clampDistance.x, _clampDistance.y);
                transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            }
        }
    }

    private float GetLeadHorseX()
    {
        float maxX = float.NegativeInfinity;
        foreach (var h in _horses)
        {
            if (h == null) continue;
            float x = h.transform.position.x;
            if (x > maxX) maxX = x;
        }
        return (maxX == float.NegativeInfinity) ? transform.position.x : maxX;
    }

    private float GetAverageHorseX()
    {
        float sum = 0f;
        int count = 0;
        foreach (var h in _horses)
        {
            if (h == null) continue;
            sum += h.transform.position.x;
            count++;
        }
        return (count == 0) ? transform.position.x : (sum / count);
    }

    // Compute a normalized progress value (0=start, 1=finish).
    // If start/finish transforms are assigned use them. Otherwise fallback to current horses min/max X.
    private float ComputeProgressTowardFinish(float targetX)
    {
        float startX, finishX;

        if (_startLineTransform != null && _finishLineTransform != null)
        {
            startX = _startLineTransform.position.x;
            finishX = _finishLineTransform.position.x;
            if (Mathf.Approximately(startX, finishX))
                return 0f;
            return Mathf.Clamp01(Mathf.InverseLerp(startX, finishX, targetX));
        }

        // fallback: derive from horses' min/max X
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        foreach (var h in _horses)
        {
            if (h == null) continue;
            float x = h.transform.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        if (minX == float.PositiveInfinity || Mathf.Approximately(minX, maxX))
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(minX, maxX, targetX));
    }

    private void ApplyZoom(float progress)
    {
        if (_cam == null) return;

        // progress 0 => far (max size), progress 1 => near (min size)
        float desiredOrtho = Mathf.Lerp(_maxOrthoSize, _minOrthoSize, progress);

        if (_cam.orthographic)
        {
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, desiredOrtho, Time.deltaTime * _zoomSmooth);
        }
        else
        {
            // map ortho sizes to approximate FOV range; adjust or expose as needed
            float maxFov = 60f;
            float minFov = 30f;
            float desiredFov = Mathf.Lerp(maxFov, minFov, progress);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, desiredFov, Time.deltaTime * _zoomSmooth);
        }
    }
}
