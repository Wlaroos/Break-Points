using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameManager : MonoBehaviour
{
    [Header("Horse Settings")]
    [SerializeField] private Horse _horse01;
    [SerializeField] private Horse _horse02;
    [SerializeField] private Horse _horse03;
    [SerializeField] private Horse _horse04;
    [Header("UI")]
    [SerializeField] private GameCanvasManager _canvasManager;
    [Header("Game Settings")]
    [SerializeField] private float _tickTimeSeconds = 1f;
    public float TickTimeSeconds => _tickTimeSeconds;
    [Header("Track Settings")]
    [SerializeField] private GameObject _trackSectionPrefab;
    [SerializeField] private GameObject _smoothTrackSectionPrefab;
    [SerializeField] private bool _useSmoothTrackSections = false;
    [Space]
    [SerializeField] private int _trackLength = 40;
    [SerializeField] private int _startingTrackAmount = 10;
    [SerializeField] private int _finishTrackAmount = 13;
    [Space]
    [SerializeField] private float _trackHorizontalSpacing = 1.5f;
    [SerializeField] private float _trackVerticalSpacing = 2.5f;
    [SerializeField] private Vector3 _spawnOrigin = Vector3.zero;
    [Header("Other Settings")]
    [SerializeField] GameObject _startLinePrefab;
    [SerializeField] GameObject _finishLinePrefab;
    [SerializeField] GameObject _obstaclePrefab;
    [SerializeField] private WinnerPopup _winnerPopup;

    private Color _horseGizmoColor = Color.cyan;
    private Color _startTrackGizmoColor = Color.green;
    private Color _trackGizmoColor = Color.yellow;
    private Color _finishTrackGizmoColor = Color.red;
    private float _horseGizmoRadius = 0.25f;

    // Default lane colors used if a Horse.DisplayColor isn't available
    private Color[] _laneDefaultColors = new Color[] {
        new Color(0.0f, 0.7f, 1.0f), // cyan-ish
        new Color(1.0f, 0.5f, 0.0f), // orange
        new Color(0.8f, 0.2f, 0.8f), // magenta
        new Color(0.4f, 1.0f, 0.2f)  // greenish
    };

    private List<Horse> _horses = new List<Horse>();
    private List<List<Transform>> _trackLanes = new List<List<Transform>>();
    private List<int> _horseTrackIndices = new List<int>();
    private List<GameObject> _startLines = new List<GameObject>();
    private List<GameObject> _finishLines = new List<GameObject>();
    
    void Awake()
    {
        _horses.Clear();
        _trackLanes.Clear();
        _horseTrackIndices.Clear();

        for (int i = 0; i < 4; i++)
        {
            Vector3 laneOrigin = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);

            // generate track and get references to sections
            var laneSections = GenerateTrack(laneOrigin);
            _trackLanes.Add(laneSections);

            // instantiate horse prefab and place at first section (index 0) if available
            Horse horsePrefab = GetHorseByIndex(i);
            if (horsePrefab != null)
            {
                Horse horseInstance = Instantiate(horsePrefab);
                Vector3 pos = (laneSections.Count > 0) ? laneSections[0].position : laneOrigin;
                horseInstance.MoveTo(pos);
                _horses.Add(horseInstance);
                _horseTrackIndices.Add(0);
            }
            else
            {
                _horses.Add(null);
                _horseTrackIndices.Add(0);
            }
        }

        // populate CameraController with the created horses (uses Main Camera)
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var camCtrl = mainCam.GetComponent<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.SetHorses(_horses);

                // pass the start/finish transforms created for the track (use first created of each)
                Transform startTransform = (_startLines != null && _startLines.Count > 0) ? _startLines[0].transform : null;
                Transform finishTransform = (_finishLines != null && _finishLines.Count > 0) ? _finishLines[0].transform : null;
                camCtrl.SetStartFinish(startTransform, finishTransform);
            }
        }


        // do a 3-second countdown (showing numbers with horse colors) then start the race
        if (_canvasManager != null)
        {
            StartCoroutine(CountdownThenStart());
        }
        else
        {
            // no UI -> start immediately
            StartCoroutine(TickLoop());
        }
    }

    private IEnumerator CountdownThenStart()
    {
        int countdown = 3;
        for (int i = countdown; i >= 1; i--)
        {
            Color msgColor = Color.white;

            _canvasManager.ShowMessage(i.ToString(), msgColor);
            yield return new WaitForSeconds(1f);
        }

        // final "Go!" then start
        _canvasManager.ShowMessage("Go!", Color.green);
        yield return new WaitForSeconds(0.6f);

        _canvasManager.ShowRaceStart();

        foreach (var line in _startLines)
        {
            Destroy(line);
        }

        StartCoroutine(TickLoop());
    }

    // Generate the track and return the list of spawned section transforms for this lane.
    private List<Transform> GenerateTrack(Vector3 startPosition)
    {
        List<Transform> sections = new List<Transform>();

        // Use the configured horizontal spacing as the track section width.
        float trackWidth = _trackHorizontalSpacing;
        if (_trackSectionPrefab != null)
        {
            var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // If spacing is invalid, fall back to prefab bounds width
                if (_trackHorizontalSpacing <= 0f)
                    trackWidth = sr.bounds.size.x;
            }
        }

        for (int i = 0; i < _trackLength; i++)
        {
            Vector3 spawnPosition = startPosition + new Vector3(i * trackWidth, 0, 0);
            Transform inst = null;
            if (_trackSectionPrefab != null)
            {
                if (_useSmoothTrackSections && _smoothTrackSectionPrefab != null)
                {
                    _trackSectionPrefab = _smoothTrackSectionPrefab;
                }

                GameObject go = Instantiate(_trackSectionPrefab, spawnPosition, Quaternion.identity);

                // make the instantiated track section a child of this manager for hierarchy organization
                // keep world position so placement doesn't change
                go.transform.SetParent(this.transform, true);

                // rename to a consistent track name (optional)
                go.name = $"Track_{sections.Count}";

                // adjust sliced/tiled sprite width to match spacing so visuals line up with placement
                var srInst = go.GetComponent<SpriteRenderer>();
                if (srInst != null)
                {
                    // determine a sensible height (use current bounds if available)
                    float height = (srInst.bounds.size.y > 0f) ? srInst.bounds.size.y : 1f;

                    // ensure drawMode supports sizing. Sliced (or Tiled) allows setting size.
                    // Note: Sliced requires a sprite with borders; Tiled may be used instead depending on art.
                    srInst.drawMode = SpriteDrawMode.Sliced;
                    srInst.size = new Vector2(trackWidth, height);
                }

                inst = go.transform;
            }
            // If no prefab provided, create an empty transform placeholder
            if (inst == null)
            {
                GameObject go = new GameObject($"Track_{sections.Count}");
                go.transform.position = spawnPosition;

                // parent placeholder to this manager as well
                go.transform.SetParent(this.transform, true);

                inst = go.transform;
            }

            if (i == 1 && _startLinePrefab != null)
            {
                GameObject startLine = Instantiate(_startLinePrefab, spawnPosition, Quaternion.identity);
                startLine.transform.SetParent(this.transform, true);
                _startLines.Add(startLine);
            }
            if (i == _trackLength - 1 && _finishLinePrefab != null)
            {
                GameObject finishLine = Instantiate(_finishLinePrefab, spawnPosition, Quaternion.identity);
                finishLine.transform.SetParent(this.transform, true);
                _finishLines.Add(finishLine);
            }

            sections.Add(inst);
        }

        return sections;
    }

    private Horse GetHorseByIndex(int index)
    {
        switch (index)
        {
            case 0:
                return _horse01;
            case 1:
                return _horse02;
            case 2:
                return _horse03;
            case 3:
                return _horse04;
            default:
                return null;
        }
    }

    // Core tick loop that lets each horse decide to move or wait
    private IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(_tickTimeSeconds);
        while (true)
        {
            // trigger pulse visual each tick
            if (_canvasManager != null)
                _canvasManager.TriggerTickPulse(_tickTimeSeconds);

            for (int lane = 0; lane < _horses.Count; lane++)
            {
                Horse horse = _horses[lane];
                if (horse == null) continue;

                int currentIndex = _horseTrackIndices[lane];
                int trackCount = (_trackLanes.Count > lane && _trackLanes[lane] != null) ? _trackLanes[lane].Count : 0;

                // Determine which region the horse is currently on and pick the correct values
                float moveChance;
                int moveDistance;

                if (trackCount == 0)
                {
                    // fallback to start values if no track info
                    moveChance = horse.MoveChance;
                    moveDistance = horse.StartMoveDistance;
                }
                else if (currentIndex < _startingTrackAmount)
                {
                    // Start region
                    moveChance = horse.MoveChance; // horse.MoveChance maps to start move chance in Horse.cs
                    moveDistance = horse.StartMoveDistance;
                }
                else if (currentIndex >= trackCount - _finishTrackAmount)
                {
                    // Finish region
                    moveChance = horse.FinishMoveChance;
                    moveDistance = horse.FinishMoveDistance;
                }
                else
                {
                    // Middle region
                    moveChance = horse.MiddleMoveChance;
                    moveDistance = horse.MiddleMoveDistance;
                }

                // decide move or wait using the region-specific chance and distance
                if (Random.value <= moveChance)
                {
                    int target = Mathf.Min(currentIndex + moveDistance, (trackCount > 0) ? trackCount - 2 : currentIndex);
                    int spacesMoved = Mathf.Abs(target - currentIndex);
                    _horseTrackIndices[lane] = target;

                    if (trackCount > 0)
                        horse.MoveTo(_trackLanes[lane][target].position);

                    // check for winner, track before finish line
                    if (trackCount > 0 && target >= trackCount - 2)
                    {
                        if (_canvasManager != null)
                        {
                            string horseName = !string.IsNullOrEmpty(horse.gameObject.name) ? horse.gameObject.name : $"Horse {lane + 1}";
                            _canvasManager.AnnounceWinner(lane, horseName, horse.DisplayColor);
                            _canvasManager.ShowRaceEnd();
                            _winnerPopup.ShowWinnerPopup(lane);
                        }
                        yield break; // stop the race
                    }
                }
            }
            yield return wait;
        }
    }

    // Draw gizmos for spawn origin, horse positions and track sections.
    void OnDrawGizmos()
    {
        // show origin
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(_spawnOrigin, _horseGizmoRadius * 0.75f);

        float trackWidth = (_trackHorizontalSpacing > 0f) ? _trackHorizontalSpacing : 1f;
        if (_trackSectionPrefab != null)
        {
            var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
            if (sr != null && _trackHorizontalSpacing <= 0f) trackWidth = sr.bounds.size.x;
        }

        // draw horses
        Gizmos.color = _horseGizmoColor;
        for (int i = 0; i < 4; i++)
        {
            Vector3 horsePos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);
            Gizmos.DrawSphere(horsePos, _horseGizmoRadius);
        }

        // draw track sections per lane using fixed colors per region (start/mid/finish)
        for (int lane = 0; lane < 4; lane++)
        {
            Vector3 laneStart = _spawnOrigin + new Vector3(0, -(lane * _trackVerticalSpacing), 0);

            // Use fixed colors for regions: start = green, middle = yellow, finish = red
            Color startColor = _startTrackGizmoColor;
            Color midColor = _trackGizmoColor;
            Color finishColor = _finishTrackGizmoColor;

            // starting region
            Gizmos.color = startColor;
            for (int i = 0; i < _startingTrackAmount; i++)
            {
                Vector3 spawnPos = laneStart + new Vector3(i * trackWidth, 0, 0);
                Gizmos.DrawWireCube(spawnPos, new Vector3(trackWidth * 0.9f, _trackVerticalSpacing * 0.8f, 0.1f));
            }

            // middle region (between start and finish)
            Gizmos.color = midColor;
            for (int i = _startingTrackAmount; i < _trackLength - _finishTrackAmount; i++)
            {
                Vector3 spawnPos = laneStart + new Vector3(i * trackWidth, 0, 0);
                Gizmos.DrawWireCube(spawnPos, new Vector3(trackWidth * 0.9f, _trackVerticalSpacing * 0.8f, 0.1f));
            }

            // finish region
            Gizmos.color = finishColor;
            for (int i = _trackLength - _finishTrackAmount; i < _trackLength; i++)
            {
                Vector3 spawnPos = laneStart + new Vector3(i * trackWidth, 0, 0);
                Gizmos.DrawWireCube(spawnPos, new Vector3(trackWidth * 0.9f, _trackVerticalSpacing * 0.8f, 0.1f));
            }
        }
    }

#if UNITY_EDITOR
    // Make spawn origin draggable in the Scene view when this object is selected.
    void OnDrawGizmosSelected()
    {
        // Position handle for adjusting the spawn origin
        EditorGUI.BeginChangeCheck();
        Vector3 newOrigin = Handles.PositionHandle(_spawnOrigin, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Move Spawn Origin");
            _spawnOrigin = newOrigin;
            EditorUtility.SetDirty(this);
        }

        // labels to clarify gizmos
        Handles.color = _horseGizmoColor;
        Handles.Label(_spawnOrigin + Vector3.up * 0.2f, "Spawn Origin");
        for (int i = 0; i < 4; i++)
        {
            Vector3 horsePos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);
            Handles.Label(horsePos + Vector3.right * 0.2f, $"Horse {i+1}");
        }
    }
#endif

    }
