using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MavMovin
{
    public class GameManager : MonoBehaviour
    {
        [Header("Horse Settings")]
        [SerializeField] private Horse _horse01;
        [SerializeField] private Horse _horse02;
        [SerializeField] private Horse _horse03;
        [SerializeField] private Horse _horse04;

        [Header("Track Settings")]
        [SerializeField] private Transform _trackSectionPrefab;
        [SerializeField] private int _trackLength = 10;
        [SerializeField] private float _trackVerticalSpacing = 2.5f;

        [Header("Powerup Settings")]
        [SerializeField] private Powerup _powerupPrefab;
        // duration a powerup boost lasts (seconds)
        [SerializeField] private float _powerupDuration = 3f;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 _spawnOrigin = Vector3.zero;
        [SerializeField] private Color _horseGizmoColor = Color.cyan;
        [SerializeField] private Color _trackGizmoColor = Color.yellow;
        [SerializeField] private float _horseGizmoRadius = 0.25f;

        [Header("Game Settings")]
        [SerializeField] private float _tickTimeSeconds = 1f;

        // UI: canvas manager to display messages & statuses
        [Header("UI")]
        [SerializeField] private GameCanvasManager _canvasManager;

        // runtime
        private List<Horse> _horses = new List<Horse>();
        private List<List<Transform>> _trackLanes = new List<List<Transform>>();
        private List<int> _horseTrackIndices = new List<int>();
        private List<Powerup> _lanePowerups = new List<Powerup>();
        private List<int> _lanePowerupIndices = new List<int>();

        void Awake()
        {
            _horses.Clear();
            _trackLanes.Clear();
            _horseTrackIndices.Clear();
            _lanePowerups.Clear();
            _lanePowerupIndices.Clear();

            for (int i = 0; i < 4; i++)
            {
                Vector3 laneOrigin = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);

                // generate track and get references to sections
                var laneSections = GenerateTrack(laneOrigin);
                _trackLanes.Add(laneSections);

                // spawn 1 powerup at a random section on this lane (never on the starting section)
                if (_powerupPrefab != null && laneSections.Count > 1)
                {
                    // pick from 1..Count-1 so index 0 (start) is excluded
                    int puIndex = Random.Range(1, laneSections.Count);
                    Transform section = laneSections[puIndex];
                    Powerup puInstance = Instantiate(_powerupPrefab, section.position, Quaternion.identity);
                    _lanePowerups.Add(puInstance);
                    _lanePowerupIndices.Add(puIndex);
                }
                else
                {
                    // not enough sections or no prefab -> no powerup for this lane
                    _lanePowerups.Add(null);
                    _lanePowerupIndices.Add(-1);
                }

                // instantiate horse prefab and place at first section (index 0) if available
                Horse horsePrefab = GetHorseByIndex(i);
                if (horsePrefab != null)
                {
                    Horse horseInstance = Instantiate(horsePrefab);
                    Vector3 pos = (laneSections.Count > 0) ? laneSections[0].position : laneOrigin;
                    horseInstance.TeleportTo(pos);
                    _horses.Add(horseInstance);
                    _horseTrackIndices.Add(0);
                }
                else
                {
                    _horses.Add(null);
                    _horseTrackIndices.Add(0);
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
            StartCoroutine(TickLoop());
        }

        // Generate the track and return the list of spawned section transforms for this lane.
        private List<Transform> GenerateTrack(Vector3 startPosition)
        {
            List<Transform> sections = new List<Transform>();

            float trackWidth = 1f;
            if (_trackSectionPrefab != null)
            {
                var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) trackWidth = sr.bounds.size.x;
            }

            for (int i = 0; i < _trackLength; i++)
            {
                Vector3 spawnPosition = startPosition + new Vector3(i * trackWidth, 0, 0);
                Transform inst = null;
                if (_trackSectionPrefab != null)
                {
                    Transform t = Instantiate(_trackSectionPrefab, spawnPosition, Quaternion.identity);
                    inst = t;
                }
                // If no prefab provided, create an empty transform placeholder
                if (inst == null)
                {
                    GameObject go = new GameObject($"Track_{sections.Count}");
                    go.transform.position = spawnPosition;
                    inst = go.transform;
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
                for (int lane = 0; lane < _horses.Count; lane++)
                {
                    Horse horse = _horses[lane];
                    if (horse == null) continue;

                    int currentIndex = _horseTrackIndices[lane];
                    int trackCount = (_trackLanes.Count > lane && _trackLanes[lane] != null) ? _trackLanes[lane].Count : 0;
                    // decide move or wait using the horse's MoveChance
                    if (Random.value <= horse.MoveChance)
                    {
                        int target = Mathf.Min(currentIndex + horse.MoveDistance, (trackCount > 0) ? trackCount - 1 : currentIndex);
                        _horseTrackIndices[lane] = target;

                        if (trackCount > 0)
                            horse.TeleportTo(_trackLanes[lane][target].position);

                        // check powerup pickup
                        if (_lanePowerups[lane] != null && _lanePowerupIndices[lane] == target)
                        {
                            Powerup pu = _lanePowerups[lane];
                            if (pu != null)
                            {
                                horse.ApplyMovePercentBoost(pu.MovePercentBoost, _powerupDuration);
                                Destroy(pu.gameObject);
                                _lanePowerups[lane] = null;
                                _lanePowerupIndices[lane] = -1;
                            }
                        }

                        // check for winner
                        if (trackCount > 0 && target == trackCount - 1)
                        {
                            if (_canvasManager != null)
                            {
                                string horseName = !string.IsNullOrEmpty(horse.gameObject.name) ? horse.gameObject.name : $"Horse {lane + 1}";
                                _canvasManager.AnnounceWinner(lane, horseName, horse.DisplayColor);
                                _canvasManager.ShowRaceEnd();
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

            float trackWidth = 1f;
            if (_trackSectionPrefab != null)
            {
                var sr = _trackSectionPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) trackWidth = sr.bounds.size.x;
            }

            // draw horses
            Gizmos.color = _horseGizmoColor;
            for (int i = 0; i < 4; i++)
            {
                Vector3 horsePos = _spawnOrigin + new Vector3(0, -(i * _trackVerticalSpacing), 0);
                Gizmos.DrawSphere(horsePos, _horseGizmoRadius);
            }

            // draw track sections for each lane
            Gizmos.color = _trackGizmoColor;
            for (int lane = 0; lane < 4; lane++)
            {
                Vector3 laneStart = _spawnOrigin + new Vector3(0, -(lane * _trackVerticalSpacing), 0);
                for (int i = 0; i < _trackLength; i++)
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
}
