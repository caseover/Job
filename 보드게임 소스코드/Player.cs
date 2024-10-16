using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDice.Board;
using UnityEngine.Events;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Linq;
using Unity.VisualScripting;
using System;
using System.Data;

namespace MyDice.Players
{
    public class Player : MonoBehaviour
    {
        #region variable
        #region public
        public const int MaxPlayerPositionIndex = 2;
        public float movementSpeed = 5;
        public float targetDistanceHit = 0.05f;
        public GameObject gostPrefab;
        public PlayerMovementType playerMovementType = PlayerMovementType.Direct;
        #region Events
        public UnityEvent onIdleModeEnter;
        public UnityEvent onMovingModeEnter;

        public float jumpHeight = 1f; // 포물선의 높이
        public float jumpDuration = 1f; // 포물선 이동에 걸리는 시간
        public float rotationSpeed = 10f; // 회전 속도
        private Queue<Vector3> targetPositions = new Queue<Vector3>(); // 이동할 목표 위치 큐
        private bool isMoving = false; // 이동 중 여부
        public AudioClip movementSound; // 포물선 이동 시 효과음
        private AudioSource audioSource; // 오디오 소스

        #endregion
        //public int positionIndexSize = 2;
        #region PlayerPositionIndex
        [HideInInspector] public int[] hitIndex;
        public int currentPositionIndex { get { return positionIndex.GetIndex(0); } set { positionIndex.AddIndex(value); } }
        #endregion
        [HideInInspector] public int TouchCount { get; private set; }
        [HideInInspector] public int playerHomeIndex;
        [HideInInspector] public float deltaTime;
        [HideInInspector] public int[] diceValues;
        [HideInInspector] public PathManager pathManager;
        public PlayerState playerState
        {
            get { return _playerState; }
            set
            {
                switch (value)
                {
                    case PlayerState.Idle:
                        if (_playerState != PlayerState.Idle)
                        {
                            if (onIdleModeEnter != null) onIdleModeEnter.Invoke();
                            if (onMovingModeEnter != null) onIdleModeEnter.RemoveAllListeners();
                        }
                        break;
                    case PlayerState.Moving:
                        if (_playerState != PlayerState.Moving)
                        {
                            if (onMovingModeEnter != null) onMovingModeEnter.Invoke();
                            if (onIdleModeEnter != null) onIdleModeEnter.RemoveAllListeners();
                        }
                        break;
                    case PlayerState.ArrangeMoving:
                    case PlayerState.ArrangeComplete:
                        break;
                    default:
                        if (onIdleModeEnter != null) onIdleModeEnter.RemoveAllListeners();
                        if (onMovingModeEnter != null) onIdleModeEnter.RemoveAllListeners();
                        break;
                }
                _playerState = value;
            }
        }
        #endregion
        #region protected
        [HideInInspector] public PlayerPositionIndex positionIndex;// = new PlayerPositionIndex(MaxPlayerPositionIndex);
        [HideInInspector] public PlayerState _playerState = PlayerState.Null;
        #endregion
        #region private
        [HideInInspector] public List<Vector3> targets;
        #endregion
        #endregion
        #region Functions
        private void OnValidate()
        {
            positionIndex = new PlayerPositionIndex(MaxPlayerPositionIndex);
        }
        void Start()
        {
            if (deltaTime == 0)
                deltaTime = Time.fixedDeltaTime;
            //if (positionIndexSize < 1) positionIndexSize = 1;
            //positionIndex = new PlayerPositionIndex(MaxPlayerPositionIndex);
            //positionIndex.AddIndex(startIndex);
            targets = new List<Vector3>();
            playerState = PlayerState.Idle;
            // 오디오 소스 설정
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        void Update()
        {
            if(OtherPlayer != null)
            {
                if (newCycle > cycle) // 새로운 사이클이 이전 사이클보다 클 경우
                {
                    cycle = newCycle;
                    if(this.gameObject.tag == "Enemy")
                    {
                        switch(cycle)
                        {
                            case 2:
                                IncreasePlayersStats();
                                UpdatePlayersMesh(1);
                                UpdatePlayersMat(1);
                                ps.ResetBreakpoint();
                                ps.bossPhase = 2;
                                break;
                            case 4:
                                IncreasePlayersStats();
                                UpdatePlayersMesh(2);
                                UpdatePlayersMat(2);
                                ps.ResetBreakpoint();
                                ps.bossPhase = 3;
                                break;
                        }
                    }
                }
            }
        }
        private void FixedUpdate()
        {
            updateMovementPosition();
        }
        #endregion
        #region functions
        #region prefab

        #endregion
        #region values
        public int getTotalValues()
        {
            int sum = 0;
            if (diceValues == null) return sum;
            for (int i = 0; i < diceValues.Length; i++)
                sum += diceValues[i];
            return sum;
        }
        #endregion
        #region movement
        #region GOTO
        public void GoTo_CalculatedIndexes(Path p, ref List<Vector3> nodes)
        {
            currentPositionIndex = p.getIndex(p.getIndexSize() - 1);
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < p.getIndexSize(); i++)
            {
                positions.Add(nodes[p.getIndex(i)]);
            }
            GoTo(positions);
        }

        public void GoTo(List<Vector3> positions)
        {
            GoTo_Immediately(positions[0]);
            
            switch (playerMovementType)
            {
                /* case PlayerMovementType.Circle:
                     CircleMaker c = new CircleMaker();
                     for (int i = 0; i < positions.Count - 1; i++)
                     {
                         var list = c.CreateHalfCircle(positions[i], positions[i + 1], 5);
                         if (list == null) continue;
                         for (int j = 0; j < list.Count; j++)
                             targets.Add(list[j]);
                     }
                     break;*/
                default:
                    for (int i = 1; i < positions.Count; i++)
                    {
                        targetPositions.Enqueue(positions[i]);
                    }

                    if (!isMoving)
                    {
                        // 이동 중이 아닌 경우에만 이동을 시작
                        StartCoroutine(MoveWithArc());
                    }
                    break;
            }
            // GoTo_Immediately(positions[0]);
            TouchCount_Increase();
            playerState = PlayerState.Moving;
        }


        private IEnumerator MoveWithArc()
        {
            playerState = PlayerState.Moving;

            while (targetPositions.Count > 0)
            {
                Vector3 targetPosition = targetPositions.Dequeue();
                Vector3 startPosition = transform.position;
                Quaternion startRotation = transform.rotation;
                float timeElapsed = 0f;

                // 효과음 재생
                if (movementSound != null)
                {
                    audioSource.PlayOneShot(movementSound);
                }

                // 포물선 이동 시작
                while (timeElapsed < jumpDuration)
                {
                    float normalizedTime = timeElapsed / jumpDuration;
                    float yOffset = Mathf.Sin(Mathf.PI * normalizedTime) * jumpHeight/3; // 포물선 높이 계산

                    // 포물선 이동 경로 계산
                    Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, normalizedTime) + Vector3.up * yOffset;
                    transform.position = nextPosition;

                    // 플레이어가 목표 지점을 바라보도록 회전
                    Vector3 direction = (targetPosition - startPosition).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, normalizedTime);

                    timeElapsed += Time.deltaTime;
                    yield return null;
                }

                // 목표 지점에 도달한 후 정확한 위치로 이동
                transform.position = targetPosition;
            }
            playerState = PlayerState.MovingComplete;
        }


        public void GoTo_Immediately(Vector3 position)
        {
            this.transform.position = position;
        }
        #endregion
        public void TouchCount_Increase()
        {
            this.TouchCount++;
        }
        public bool hasPath()
        {
            return pathManager != null && pathManager.Paths.Count > 0;
        }
        public void CalculatePositionIndex(ref List<GameObject> nodes, ElementNodeCreator.RoutingMode routingMode,bool addUniqueIndex)
        {
            if (nodes == null || diceValues == null && diceValues.Length < 1) return;
            pathManager = new PathManager(currentPositionIndex, diceValues, routingMode);
            pathManager.FindPaths(ref nodes, addUniqueIndex);
        }
        public void CalculatePositionIndex(int size)
        {
            hitIndex = new int[diceValues.Length + 1];
            hitIndex[0] = currentPositionIndex;
            for (int i = 1; i < hitIndex.Length; i++)
            {
                hitIndex[i] = (hitIndex[i - 1] + diceValues[i - 1]) % size;
            }
            positionIndex.AddIndex((currentPositionIndex + getTotalValues()) % (size));
        }
        //private float smoothTime = 0.05f;

        private void updateMovementPosition()
        {
            if (targets == null || targets.Count < 1) return;
            if (Vector3.Distance(targets[0], this.transform.position) < targetDistanceHit)
            {
                targets.RemoveAt(0);
                if (targets.Count < 1)
                {
                    var r = this.transform.rotation;
                    r.x = r.z = 0f;
                    this.transform.rotation = r;
                    playerState = PlayerState.MovingComplete;
                }
                return;
            }
            Vector3 targetPosition = targets[0];
            float distance = Vector3.Distance(targetPosition, this.transform.position);

            this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, Time.deltaTime * movementSpeed);

            Vector3 dir = (targetPosition - this.transform.position);

            if (dir != Vector3.zero)
            {
                // this.transform.rotation = Quaternion.LookRotation(dir);
                // if (dir.y >= 0)
                // {
                //     this.transform.rotation = Quaternion.LookRotation(dir);
                // }
            }
            this.transform.position = Vector3.MoveTowards(this.transform.position, targets[0], (deltaTime * movementSpeed));
        }
        #endregion
        #region save / load
        #region save
        public void Save(string key)
        {
            ///
            var k = getKey(key, "position");
            PlayerPrefs.SetFloat(k + "_x", this.transform.position.x);
            PlayerPrefs.SetFloat(k + "_y", this.transform.position.y);
            PlayerPrefs.SetFloat(k + "_z", this.transform.position.z);
            ///
            k = getKey(key, "rotation");
            PlayerPrefs.SetFloat(k + "_x", this.transform.rotation.x);
            PlayerPrefs.SetFloat(k + "_y", this.transform.rotation.y);
            PlayerPrefs.SetFloat(k + "_z", this.transform.rotation.z);
            PlayerPrefs.SetFloat(k + "_w", this.transform.rotation.w);
            ///
            if (positionIndex != null)
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(PlayerPositionIndex));
                System.IO.MemoryStream msObj = new System.IO.MemoryStream();
                js.WriteObject(msObj, positionIndex);
                msObj.Position = 0;
                System.IO.StreamReader sr = new System.IO.StreamReader(msObj);
                string json = sr.ReadToEnd();
                sr.Close();
                msObj.Close();
                PlayerPrefs.SetString(getKey(key, "positionIndex"), json);
            }
            ///
            PlayerPrefs.Save();
        }
        #endregion
        #region load
        public void Load(string key)
        {
            var k = getKey(key, "position");
            var x = PlayerPrefs.GetFloat(k + "_x");
            var y = PlayerPrefs.GetFloat(k + "_y");
            var z = PlayerPrefs.GetFloat(k + "_z");
            this.transform.position = new Vector3(x, y, z);
            ///
            k = getKey(key, "rotation");
            x = PlayerPrefs.GetFloat(k + "_x");
            y = PlayerPrefs.GetFloat(k + "_y");
            z = PlayerPrefs.GetFloat(k + "_z");
            var w = PlayerPrefs.GetFloat(k + "_w");
            this.transform.rotation = new Quaternion(x, y, z, w);
            ///
            k = getKey(key, "positionIndex");
            if (PlayerPrefs.HasKey(k))
            {
                string json = PlayerPrefs.GetString(k);
                using (var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(PlayerPositionIndex));
                    positionIndex = (PlayerPositionIndex)deserializer.ReadObject(ms);
                }
            }
        }
        #endregion
        private string getKey(string key, string variableName)
        {
            return key + "_Player_" + variableName;
        }
        #endregion
        #endregion
        [HideInInspector]public PS ps;
        [HideInInspector]public static Player Instance { get; private set; }
        void Awake()
        {
            ps = GetComponent<PS>();
        }
        
        ///사이클 계산
        [Header("Cycle")]
        public int totalDistance = 0; // 플레이어의 총 이동 거리
        public int totalBoardSpaces; // 보드판의 총 칸 수
        public int cycle = 0; // 완료된 바퀴 수
        public Player OtherPlayer; // 다른 플레이어 지정
        public Material[] PlayerMaterials;
        public List<Mesh> meshes; //변경할 메시 지정
        public List<MeshRenderer> meshRenderers;
        [HideInInspector]public int newCycle = 0;

        public void UpdatePosition()
        {
            if (diceValues == null || diceValues.Length == 0)
            {
                Debug.LogError("No dice values available to process.");
                return;
            }

            int diceSum = 0;
            foreach (int value in diceValues)
            {
                diceSum += value; // 주사위 결과 합산
            }

            totalDistance += diceSum; // 총 이동 거리 업데이트
            newCycle = totalDistance / totalBoardSpaces;
        }

        void IncreasePlayersStats()
        {
            //원하는 스탯 증가
            ps.IncreaseDef(0);
        }

        public void UpdatePlayersMesh(int meshindex)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshes.Count > 0)
            {
                // 사이클에 따라 메시 인덱스 계산, 마지막 메시 유지
                // int meshIndex = Mathf.Min(OtherPlayer.cycle, PlayerMeshes.Length - 1);
                meshFilter.mesh = meshes[meshindex];
            }
        }
        public void UpdatePlayersMat(int matIndex)
        {
            MeshRenderer meshRenderer= GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderers.Count > 0) 
            {
                // int meshIndex = Mathf.Min(OtherPlayer.cycle, PlayerMaterials.Length - 1);
                meshRenderer = meshRenderers[matIndex];
            }
        }
    }
}