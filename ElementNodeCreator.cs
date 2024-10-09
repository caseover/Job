using System.Collections.Generic;
using UnityEngine;
using MyDice.Helpers;
using MyDice.Players;
using UnityEngine.Events;
using MyDice.Board.DataStructure;
using System;
using UnityEditor;
using System.Xml;
using Unity.VisualScripting;

namespace MyDice.Board
{
    public class ElementNodeCreator : MonoBehaviour
    {
        #region enum
        public enum RoutingMode
        {
            Direct,
            Reverse
        }
        #endregion
        #region variable
        #region public
        #region HideInInspector
        #region data
        [HideInInspector] public List<Vector3> points = new List<Vector3>();
        [HideInInspector] public List<GameObject> nodes = new List<GameObject>();
        [HideInInspector] public List<PlayerHome> playerHomes = new List<PlayerHome>();
        #endregion
        #region UNITY_EDITOR
#if UNITY_EDITOR
        [HideInInspector] public bool eventsEnable = true;
        [HideInInspector] public bool parametersEnable = true;
        [HideInInspector] public bool editorSettingEnable = true;
        [HideInInspector] public bool infoEnable = true;
        [HideInInspector] public bool designEnable = true;
        [HideInInspector] public bool logicEnable = true;
        [HideInInspector][Range(0, 1)] public float scaleCoefficient = .1f;
#endif
        #endregion
        [HideInInspector] public int playerHomeIndex = -1;
        [HideInInspector] public List<GameObject> ghosts;
        [HideInInspector] public bool allowRolling = true;
        [HideInInspector] public ElementNodesManager elementNodesManager;
        [HideInInspector] public List<PlayerSkipTurn> playerSkipTurns;
        [HideInInspector] public ArrangingPiecesInElementNode arrangingPiecesInElementNode = new ArrangingPiecesInElementNode();
        [HideInInspector] public bool RequestIntrrupt;
        #region Shape Struct
        [HideInInspector] public CircleStruct circleStruct;
        [HideInInspector] public SquareStruct squareStruct;
        [HideInInspector] public EllipseStruct ellipseStruct;
        [HideInInspector] public DiamondStruct diamondStruct;
        [HideInInspector] public TriangleStruct triangleStruct;
        [HideInInspector] public PolygonStruct polygonStruct;
        [HideInInspector] public LineStruct lineStruct;
        #endregion
        #endregion
        //[Header("Settings")]
        public KeyCode rollingKeyCode = KeyCode.Space;
        //[Header("Editor settings")]
        public GameObject defaultPrefab;
        public float handlePointRadius = .1f;
        public float handleArrowSize = .2f;
        public float handlePlayerHomesRadius = .5f;
        public bool autoRotation = false;
        public bool showRedirectLines = true;
        //board
        public bool autoDetection_Board = true;
        public DiceManager diceManager;
        //routing
        public RoutingMode routingMode = RoutingMode.Direct;
        public bool addUniqueIndex;
        //events
        public UnityEvent[] onPlayerSkippingEvents;
        public UnityEvent[] onNextPlayerEvents;
        //Arrange the pisces 
        public bool arrangeThePisces;
        [Min(0f)]
        public float arrangeThePiscesRadius = .1f;
        #endregion
        #region Save / Load
        public const string SaveLoadKey = Globals.RootName + " " + Globals.PROJECT_NAME + " Save_Load_Key";
        private bool isSaveLoad = false;
        #endregion
        #region private
        private bool isIntrrupt = false;
        #endregion
        #endregion
        #region Functions
        void Awake()
        {
            structureManage = FindObjectOfType<StructureManage>();
            townUI = FindObjectOfType<TownUI>();
            Cltmanage = FindObjectOfType<CollectManager>();
            Atkmanage = FindObjectOfType<AttackManage>();
            turn = FindObjectOfType<Turn>();
            inventory = FindObjectOfType<Inventory>();
            material = FindObjectOfType<GameMaterial>();
            endturn = FindObjectOfType<EndTurn>();
            RollDice = FindObjectOfType<RollDice>();
            skipTurn = FindObjectOfType<SkipTurn>();
            ps = FindObjectOfType<PS>();
            initPlayers();
        }
        private void OnValidate()
        {
            elementNodesManager = new ElementNodesManager(ref nodes);
            elementNodesManager.checkToFixProblems();
        }
        private void Start()
        {
            UpdateBoardSpaces();
            if (cameraSwitcher == null)
            {
                cameraSwitcher = FindObjectOfType<CameraSwitcher>();
                if (cameraSwitcher == null)
                {
                    Debug.LogError("CameraSwitcher component not found in the scene.");
                }
            }
            if (autoDetection_Board)
            {
                diceManager = FindObjectOfType<DiceManager>();
            }
            if (diceManager == null
                && (diceManager = FindObjectOfType<DiceManager>()) == null)
            {
                Debug.LogError("Dice manager not found.");
                Extensions.Quit();
                return;
            }
            //if (checkForceToLoadOnStart()){Load();}
        }
        private void Update()
        {
            checkForHitPlayer();
            // 움직임이 끝나고 바로 node가 동작하지 않게 함
            if (isWaiting)
            {
                float t = .5f;
                delayTimer += Time.deltaTime;
                if (delayTimer >= t)
                {  // t초가 지난 후
                    continuationAction.Invoke();
                    isWaiting = false;  // 대기 상태 종료
                    initialActionsPerformed = false;  // 초기 상태로 리셋
                }
            }
            if (isIntrrupt || isSaveLoad) return;
            if (endturn.IsTurnEnd && cameraSwitcher.IsMainCameraActive && diceManager.getDiceState() == DiceState.Ready)
            {
                if (RequestIntrrupt)
                {
                    RequestIntrrupt = false;
                    isIntrrupt = true;
                    return;
                }
                switch (playerHomes[playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:
                        {
                            rolling();
                        }
                        break;
                    case PlayerMode.CPU:
                        {
                            Invoke("rolling", 0.5f);
                        }
                        break;
                }
                // rolling();
            }
            else if (diceManager.getDiceState() == DiceState.Finish)
            {
                switch (playerHomes[playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:
                        Player player = playerHomes[playerHomeIndex].getCandidatePlayer();
                        if (player == null)
                        {
                            checkForCandidate_Human(playerHomeIndex);
                            return;
                        }
                        updatePlayerGame_Human(player);
                        break;
                    case PlayerMode.CPU:
                        updatePlayerGame_CPU();
                        break;
                }
            }
        }
        #endregion
        #region functions
        #region updates

        public string targetTag;
        private void checkForHitPlayer()
        {
            RaycastHit hit = new RaycastHit();
            if (!mouseHit(ref hit)) return;
            if (hit.collider.gameObject == null) return;
            targetTag = hit.collider.gameObject.tag; 
            VC.VCamera.Instance.ShowPlayerView(targetTag);        // 나중에 태그마다 뷰 바뀌게 변경if로
        }
        private void updatePlayerGame_Human(Player player)
        {
            if (player.playerState == PlayerState.Idle)
            {
                RoutingMode routingMode = RoutingMode.Reverse;
                int[] new_DiceValues = new int[] {1, 0};
                // int[] new_DiceValues = diceManager.getDicesValues();
                //get dice
                player.diceValues = new_DiceValues;
                //calculate indexes
                player.CalculatePositionIndex(ref nodes, routingMode, addUniqueIndex);
                //Select Path
                if (player.hasPath())
                {
                    if (player.pathManager.Paths.Count > 1)
                    {
                        showPaths(ref player);
                        player.playerState = PlayerState.SelectPath;
                    }
                    else
                    {
                        player.GoTo_CalculatedIndexes(player.pathManager.Paths[0], ref points);
                    }
                }
                else
                {
                    Debug.Log("No path is exist.");
                    playerAndBoardReset(player);
                }
            }
            else if (player.playerState == PlayerState.MovingComplete)
            {
                Invoke("IncreaseFakePTurn", 1f);
                // Invoke("MakeRuins",1f);
                PlayerState_MovingComplete(player);
            }
            else if (player.playerState == PlayerState.SelectPath)
            {
                checkForHitGhost(player);
            }
        }
        private void updatePlayerGame_CPU()
        {
            if (playerHomes[playerHomeIndex].getCandidateIndex() < 0)
            {
                int[] new_DiceValues = new int[] {10,0};
                // int[] new_DiceValues = diceManager.getDicesValues();
                playerHomes[playerHomeIndex].chooseCandidateIndexByAI(new_DiceValues, ref nodes, routingMode, addUniqueIndex);
            }
            Player player = playerHomes[playerHomeIndex].getCandidatePlayer();
            if (player == null)
            {
                Debug.Log("No player is exist.");
                playerAndBoardReset(player);
                return;
            }
            if (player.playerState == PlayerState.Idle)
            {
                Path path;
                if (playerHomes[playerHomeIndex].targetIndex > -1)
                {
                    path = player.pathManager.getBestBenefitPath();
                }
                else
                {
                    path = player.pathManager.getRandomPath();
                }
                if (path == null)
                {
                    Debug.Log("No player is exist.");
                    playerAndBoardReset(player);
                    return;
                }
                // List<Vector3> positions = new List<Vector3>();
                // positions.Add(new Vector3(-6, 0, -1));
                // positions.Add(new Vector3(-7, 0, -1));
                // positions.Add(new Vector3(-8, 0, -1));
                // player.GoTo(positions);
                player.GoTo_CalculatedIndexes(path, ref points);
                // player.GoTo_CalculatedIndexes(path, ref positions);
            }
            else if (player.playerState == PlayerState.MovingComplete)
            {
                Atkmanage.CanAttack = false;
                Invoke("IncreseETurn", 1f);
                PlayerState_MovingComplete(player);
                Invoke("nextPlayer", 1f);
            }
        }
        public void PlayerState_MovingComplete(Player player)
        {
            if (!initialActionsPerformed)
            {
                ElementNode node = elementNodesManager.getNode(player.currentPositionIndex);
                if (node == null) return;

                node.InvokeEvents();
                player.UpdatePosition();
                Atkmanage.CalculateDistance();
                if (arrangeThePisces)
                {
                    var lastIndex = player.positionIndex.GetIndex(1);
                    arrangingPiecesInElementNode.addElementNode(elementNodesManager.getNode(lastIndex));
                    arrangingPiecesInElementNode.addElementNode(node);
                    arrangingPiecesInElementNode.findAndArrangePlayers(arrangeThePiscesRadius, ref playerHomes);
                }
                initialActionsPerformed = true;

                // 지연 후 실행할 로직을 람다 함수로 정의
                switch (playerHomes[playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:
                        continuationAction = () =>
                        {
                            P_ContinueExecution(node, player);
                        };
                        isWaiting = true;
                        delayTimer = 0;
                        break;
                    case PlayerMode.CPU:
                        continuationAction = () =>
                        {
                            E_ContinueExecution(node, player);
                        };
                        isWaiting = true;
                        delayTimer = 0;
                        endturn.IsTurnEnd = false;
                        // initialActionsPerformed = false; //위 5줄 메소드 주석처리 시 주석해제
                        break;
                }
                playerAndBoardReset(player);
            }
        }
        private void P_ContinueExecution(ElementNode node, Player player)
        {
            Atkmanage.CanAttack = true;
            if (node.redirectIndex != -1
            && node.redirectIndex != player.currentPositionIndex)
            {
                player.GoTo(new List<Vector3>() { points[player.currentPositionIndex], points[node.redirectIndex] });
                player.currentPositionIndex = node.redirectIndex;
                return;
            }
            switch (node.elementNodeType)
            {
            case ElementNodeType.ResetPoint:
                if (playerHomes[playerHomeIndex].startIndex != player.currentPositionIndex)
                {
                    player.GoTo(new List<Vector3>() {
                        points[player.currentPositionIndex],
                        points[playerHomes[playerHomeIndex].startIndex] });
                    player.currentPositionIndex = playerHomes[playerHomeIndex].startIndex;
                    endturn.IsTurnEnd = false;
                    return;
                }
                break;
            case ElementNodeType.InterruptPoint:
                isIntrrupt = true;
                endturn.IsTurnEnd = false;
                break;
            case ElementNodeType.Forest:
                Cltmanage.ActiveButton(node);
                endturn.IsTurnEnd = false;
                break;
            case ElementNodeType.Mine:
                Cltmanage.ActiveButton(node);
                endturn.IsTurnEnd = false;
                break;
            case ElementNodeType.BeforeBreak:
                nodeTag = node.tag;
                player.OtherPlayer.ps.ResetBreakpoint();
                ChangeNodeTypeForTag(nodeTag, ElementNodeType.Town);
                endturn.IsTurnEnd = false;
                break;
            case ElementNodeType.Town:
                UIPanel.SetActive(true);
                townUI.isUIOpen = true;
                endturn.IsTurnEnd = false;
                break;          
            case ElementNodeType.Ruins:
                endturn.IsTurnEnd = false;
                break;    
            case ElementNodeType.Event:
                endturn.IsTurnEnd = false;
                int randomIndex = UnityEngine.Random.Range(1, 2);
                switch(randomIndex)
                {
                    case 0:
                        // RollDiceEvent();
                        MoveMore(player, 1);
                        break;
                    // case 1:
                    //     int randomBuffIndex = UnityEngine.Random.Range(0, 2);
                    //     string randomBuff = null;
                    //     switch(randomBuffIndex)
                    //     {
                    //         case 0:
                    //         randomBuff = "공격력 증가";
                    //         break;
                    //         case 1:
                    //         randomBuff = "방어력 증가";
                    //         break;
                    //         case 2:
                    //         randomBuff = "";
                    //         break;
                    //         case 3:
                    //         randomBuff = "";
                    //         break;
                    //         case 4:
                    //         randomBuff = "";
                    //         break;
                    //     }
                    //     BufforDebuffPlayer(player,randomBuff, 1, 1);
                    //     break;
                    case 1:
                        GetGold(player, 1);
                        break;
                    case 2:
                        GetRandomMat();
                        break;
                }
                break;
            case ElementNodeType.Trap:
                nodeIndex = node.index;
                List<BossTrapmanager> activeTrapsCopy = new List<BossTrapmanager>(player.OtherPlayer.ps.activeTraps);

                foreach (BossTrapmanager trap in activeTrapsCopy)
                {
                    if (trap.NodeIndex == nodeIndex)
                    {
                        ElementNode node1 = trap.Elementnode;
                        ElementNodeType elementNodeType = trap.ElementnodeType;
                        int nodeIndex = trap.NodeIndex;
                        ChangeNodeTypeForIndex(node1, nodeIndex, elementNodeType);
                        player.OtherPlayer.ps.activeTraps.Remove(trap);
                    }
                }
                endturn.IsTurnEnd = false;
                playerTrapped = 2;
                break;        
            }
        }
        private void E_ContinueExecution(ElementNode node, Player player)
        {
            if (node.redirectIndex != -1
            && node.redirectIndex != player.currentPositionIndex)
            {
                player.GoTo(new List<Vector3>() { points[player.currentPositionIndex], points[node.redirectIndex] });
                player.currentPositionIndex = node.redirectIndex;
                return;
            }
            switch (node.elementNodeType)
            {
                case ElementNodeType.ResetPoint:
                    if (playerHomes[playerHomeIndex].startIndex != player.currentPositionIndex)
                    {
                        player.GoTo(new List<Vector3>() {
                                points[player.currentPositionIndex],
                                points[playerHomes[playerHomeIndex].startIndex] });
                        player.currentPositionIndex = playerHomes[playerHomeIndex].startIndex;
                    return;
                    }
                    break;
                case ElementNodeType.Town:
                    NewNode = node;
                    if (player.ps.breakpoint >= 1)
                    {
                        nodeTag = node.tag;
                        ChangeNodeTypeForTag(nodeTag, ElementNodeType.BeforeBreak);
                        player.ps.DecreseBreakpoint();
                    }
                    break;
                default:
                    if (restTrap <= 0)
                    {
                        nodeType = node.elementNodeType;
                        nodeIndex = node.index;
                        int a;
                        int totalIndex;
                        ElementNode elementNode;                    

                        do      
                        {
                            a = UnityEngine.Random.Range(0, 3);
                            totalIndex = nodeIndex + a;
                            if (totalIndex < 0)
                            {
                                totalIndex = player.totalBoardSpaces + totalIndex;
                            }

                            elementNode = nodes[totalIndex].GetComponent<ElementNode>();

                        } while (elementNode.elementNodeType == ElementNodeType.Trap);

                        // Trap이 아닌 노드가 선택되었으면 트랩을 설정
                        SetTrap(node, player, elementNode.elementNodeType, 2, totalIndex);
                        ChangeNodeTypeForIndex(node, totalIndex, ElementNodeType.Trap);
                        restTrap = 2;
                    }
                    break;
            }
        }
        private void playerAndBoardReset(Player player)
        {
            if (player != null)
            {
                player.playerState = PlayerState.Idle;
                //TODO: arranging
            }
            playerHomes[playerHomeIndex].Reset();
            diceManager.ResetDices();
            // nextPlayer();
            allowRolling = true;
        }
        #region checkForCandidate
        private void checkForCandidate_Human(int playerHomeIndex)
        {
            RaycastHit[] hits = mouseHits();
            if (hits == null) return;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.gameObject == null) continue;
                Player player = hit.collider.gameObject.GetComponent<Player>();
                if (player == null) continue;
                for (int j = 0; j < playerHomes[playerHomeIndex].players.Count; j++)
                {
                    if (player == playerHomes[playerHomeIndex].getPlayer(j))
                    {
                        playerHomes[playerHomeIndex].setCandidateIndex(j);
                        playerHomes[playerHomeIndex].getCandidatePlayer().playerState = PlayerState.Idle;
                        return;
                    }
                }
            }
        }
        #endregion
        private void checkForHitGhost(Player player)
        {
            RaycastHit hit = new RaycastHit();
            if (!mouseHit(ref hit)) return;

            PlayerGhost pg;
            if (hit.collider.gameObject == null
                || hit.collider.gameObject.transform.parent == null
                || (pg = hit.collider.gameObject.transform.parent.GetComponent<PlayerGhost>()) == null) return;
            if (pg.getPath() == null) return;
            player.GoTo_CalculatedIndexes(pg.getPath(), ref points);
            destroyGosts();
        }
        private bool mouseHit(ref RaycastHit hit)
        {
            if (!Input.GetMouseButtonDown(0)) return false;
            Vector3 mouse = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mouse);
            if (!Physics.Raycast(ray, out hit, Mathf.Infinity)) return false;
            return true;
        }
        private RaycastHit[] mouseHits()
        {
            if (!Input.GetMouseButtonDown(0)) return null;
            Vector3 mouse = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mouse);
            return Physics.RaycastAll(ray, Mathf.Infinity);
        }
        #endregion
        #region Points
        public int getPointsCount()
        {
            return points == null ? -1 : points.Count;
        }
        public Vector3 getPoint(int index)
        {
            if (index < 0 || index >= points.Count) return Vector3.zero;
            return points[index];
        }
        public bool getPoint(int index, out Vector3 position)
        {
            if (index < 0 || index >= points.Count) { position = default; return false; }
            position = points[index];
            return true;
        }
        public Quaternion getPointRotation(int from, int to)
        {
            Vector3 v = getPoint(to) - getPoint(from);
            if (v == Vector3.zero) return Quaternion.identity;
            return Quaternion.LookRotation(v);
        }
        public void setPoint(int index, Vector3 position, bool keepY = false)
        {
            if (points == null || index < 0 || index >= points.Count) return;
            var p = position;
            if (keepY)
            {
                p.y = points[index].y;
            }
            points[index] = p;
            ElementNode node;
            if (nodes[index] == null)
            {
                initNode(index, p);
            }
            if ((node = elementNodesManager.getNode(index)) == null)
            {
                node = nodes[index].AddComponent<ElementNode>();
            }
            node.setPosition(p);
            if (autoRotation)
            {
                //node.setRotation(getPointRotation(index));
            }
        }
        public void checkPointsForRemovedNodes()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                {
                    initNode(i, points[i]);
                }
            }
        }
        public void insertPoint(int index, Vector3 position)
        {
            insertNode(index, position);
            points.Insert(index, position);
        }
        private void insertNode(int index, Vector3 position)
        {
            long id = System.DateTime.Now.ToFileTimeUtc();
            GameObject go = new GameObject("_Home " + id.ToString());
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Insert node: " + id.ToString());
#endif
            go.transform.SetParent(this.transform);
            ElementNode node = go.AddComponent<ElementNode>();
            node.point = position;
            node.index = index;
            node.ID = id;
            node.UpdatePrefab(defaultPrefab);
            nodes.Insert(index, go);
        }
        #region connections
        public void FixConnections(out int totalConnections)
        {
            ElementNode node;
            totalConnections = 0;
            for (int i = 0; i < elementNodesManager.getNodesSize(); i++)
            {
                if ((node = elementNodesManager.getNode(i)) == null) continue;
                node.IncomingConnectionsReset();
            }
            for (int i = 0; i < elementNodesManager.getNodesSize(); i++)
            {
                if ((node = elementNodesManager.getNode(i)) == null) continue;
                for (int j = 0; j < node.getConnectionsSize(); j++)
                {
                    var n = elementNodesManager.getNode(node.connections[j]);
                    if (n == null)
                    {
                        node.RemoveConnectionByIndex(j--);
                    }
                    else
                    {
                        totalConnections++;
                        n.AddIncomingConnection(i);
                    }
                }
            }
        }
        public void nodeConnection(int node1Index, int node2Index)
        {
            if (node1Index < 0 || node1Index >= nodes.Count) return;
            if (node2Index < 0 || node2Index >= nodes.Count) return;
            if (node1Index == node2Index) return;
            ElementNode node1 = elementNodesManager.getNode(node1Index);
            ElementNode node2 = elementNodesManager.getNode(node2Index);
            if (node1 != null && node2 != null)
            {
                node1.AddConnection(node2Index);
                node2.AddIncomingConnection(node1Index);
            }
        }
        #endregion
        public void initNode(int index, Vector3 position)
        {
            long id = System.DateTime.Now.ToFileTimeUtc();
            GameObject go = new GameObject(id.ToString());
            go.transform.SetParent(this.transform);
            ElementNode node = go.AddComponent<ElementNode>();
            node.point = position;
            node.index = index;
            node.ID = id;
            nodes[index] = go;
        }
        public bool RemovePoint(int index)
        {
            ElementNode dNode = elementNodesManager.getNode(index);
            if (dNode == null) return false;

            ElementNode pNode = elementNodesManager.getNode((index - 1) % nodes.Count);
            if (pNode != null)
            {
                pNode.AddConnection(dNode.connections);
            }

            UnityEngine.Object.DestroyImmediate(nodes[index].gameObject);
            //Delete connections
            for (int i = 0; i < dNode.getConnectionsSize(); i++)
            {
                var n = elementNodesManager.getNode(dNode.connections[i]);
                if (n == null) continue;
                n.RemoveIncomingConnectionByValue(index);
            }
            //delete all incoming connections
            for (int i = 0; i < dNode.getIncomingConnectionsSize(); i++)
            {
                var n = elementNodesManager.getNode(dNode.incomingConnections[i]);
                if (n == null) continue;
                n.RemoveConnectionByValue(index);
            }
            //fix all node(s)
            for (int i = 0; i < nodes.Count; i++)
            {
                ElementNode n;
                if (i == index || (n = elementNodesManager.getNode(i)) == null) continue;
                if (n.index > index) n.index--;
                n.DecreaseConnectionValues(index);
                if (n.redirectIndex == index)
                {
                    n.setRedirectIndex(-1);
                }
            }

            points.RemoveAt(index);
            nodes.RemoveAt(index);

            return true;
        }
        public void PointsClear()
        {
            foreach (var node in nodes)
            {
                var f = node.GetComponent<ElementNode>();
                if (f != null) f.OnDestroy();
                UnityEngine.Object.DestroyImmediate(node);
            }
            nodes.Clear();
            points.Clear();
        }
        public void ResetMovementsY(float defaultY)
        {
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    p.y = defaultY;
                    setPoint(i, p);
                }
            }
            if (playerHomes != null)
            {
                for (int i = 0; i < playerHomes.Count; i++)
                {
                    var p = playerHomes[i].center;
                    p.y = defaultY;
                    setPlayerHome(i, p);
                }
            }
        }
        #endregion
        #region interrupt
        public bool hasInterrupt()
        {
            return isIntrrupt;
        }
        public void resetInterrupt()
        {
            isIntrrupt = false;
        }
        #endregion
        #region player
        #region SkipTurn
        public int SkipTurnCount(Player player)
        {
            if (player == null) return 0;
            return SkipTurnCount(player.playerHomeIndex);
        }
        public int SkipTurnCount(int playerIndex)
        {
            if (playerSkipTurns == null) return 0;
            for (int i = 0; i < playerSkipTurns.Count; i++)
            {
                if (playerSkipTurns[i] == null)
                {
                    playerSkipTurns.RemoveAt(i--);
                    continue;
                }
                if (playerSkipTurns[i].PlayerIndex == playerIndex)
                {
                    return playerSkipTurns[i].SkipRound;
                }
            }
            return 0;
        }
        public void SkipTurn(Player player, int round = 1)
        {
            if (player == null) return;
            SkipTurn(new PlayerSkipTurn(player.playerHomeIndex, round));
        }
        public void SkipTurn(int playerIndex, int round = 1)
        {
            SkipTurn(new PlayerSkipTurn(playerIndex, round));
        }
        public void SkipTurn(PlayerSkipTurn node)
        {
            if (node == null) return;
            if (playerSkipTurns == null)
            {
                playerSkipTurns = new List<PlayerSkipTurn>();
            }
            for (int i = 0; i < playerSkipTurns.Count; i++)
            {
                if (playerSkipTurns[i] == null)
                {
                    playerSkipTurns.RemoveAt(i--);
                    continue;
                }
                if (playerSkipTurns[i].MergeIfPlayerIndexEquals(node))
                {
                    return;
                }
            }
            playerSkipTurns.Add(node);
            if (playerHomeIndex == node.PlayerIndex)
            {
                nextPlayer();
            }
        }
        private bool playerSkipping(int playerIndex)
        {
            if (playerSkipTurns != null)
            {
                for (int i = 0; i < playerSkipTurns.Count; i++)
                {
                    if (playerSkipTurns[i].PlayerIndex == playerHomeIndex)
                    {
                        Debug.Log("Player skip info: " + playerSkipTurns[i].ToString());
                        if (playerSkipTurns[i].Skipping())
                        {
                            InvokeEvents(onPlayerSkippingEvents);
                            return true;
                        }
                        else
                        {
                            playerSkipTurns.RemoveAt(i--);
                            continue;
                        }
                    }
                }
            }
            return false;
        }
        #endregion
        public int PlayerHomeIndex { get { return playerHomeIndex; } }
        public int playerHomeCandidateIndex { get { return playerHomes[playerHomeIndex].candidateIndex; } }
        private void destroyGosts()
        {
            if (ghosts == null) return;
            foreach (GameObject node in ghosts)
            {
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
        private void showPaths(ref Player player)
        {
            ghosts = new List<GameObject>();
            for (int i = 0; i < player.pathManager.Paths.Count; i++)
            {
                int[] hits = player.pathManager.Paths[i].getHitIndex();
                GameObject gost = new GameObject("gost" + i);
                gost.transform.SetParent(this.transform);
                PlayerGhost pg = gost.AddComponent<PlayerGhost>();
                pg.setPath(player.pathManager.Paths[i]);
                if (player.gostPrefab == null)
                {
                    var gp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pg.setPrefab(gp);
                    UnityEngine.Object.DestroyImmediate(gp);
                }
                else
                {
                    pg.setPrefab(player.gostPrefab);
                }
                pg.updatePosition(getPoint(hits[hits.Length - 1]));
                ghosts.Add(gost);
            }
        }
        public void nextPlayer()
        {

            Playermanager.Instance.NextPlayer();
            playerHomeIndex = (playerHomeIndex + 1) % playerHomes.Count;
            if (playerHomes[playerHomeIndex].playerMode == PlayerMode.Human && playerTrapped == 1)
            {
                skipTurn.TrappedSkip();
            }
            else if (playerTrapped <= 0)
            {
                switch (playerHomes[playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:          
                        RollDice.rollDiceButton.interactable = true;
                        break;
                    case PlayerMode.CPU:
                        RollDice.rollDiceButton.interactable = false;
                        break;
                }
                switch (playerHomes[playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:
                        if (turn.Real_P_turn == 1)
                        {
                            skipTurn.skipTurnButton.interactable = false;
                        }
                        else
                        {
                            skipTurn.skipTurnButton.interactable = true;
                        }
                        break;
                    case PlayerMode.CPU:
                        skipTurn.skipTurnButton.interactable = false;
                        break;
                }
                if (playerSkipping(playerHomeIndex)) { nextPlayer(); }
                else
                {
                    InvokeEvents(onNextPlayerEvents);
                }
            }
            endturn.IsTurnEnd = true;
        }
        private void initPlayers()
        {
            if (playerHomes == null || playerHomes.Count < 1)
            {
                Debug.LogError("No player home found.");
                Extensions.Quit();
                return;
            }
            foreach (PlayerHome node in playerHomes)
            {
                node.Reset();
                node.initPlayers();
            }
            playerHomeIndex = 0;
        }
        #endregion
        #region PlayerHome
        public void addPlayerHome(Vector3 position)
        {
            addPlayerHome(position, 0);
        }
        public void addPlayerHome(Vector3 position, int pointIndex)
        {
            PlayerHome node = new PlayerHome(this.transform);
            node.setCenter(position);
            node.index = playerHomes.Count;
            node.startIndex = pointIndex;
            node.updatePositions(handlePlayerHomesRadius);
            playerHomes.Add(node);
        }
        public void removePlayerHome(PlayerHome item)
        {
            removePlayerHome(playerHomes.IndexOf(item));
        }
        public void removePlayerHome(int index)
        {
            if (index < 0 || index >= playerHomes.Count) return;
            for (int i = index + 1; i < playerHomes.Count; i++)
            {
                playerHomes[i].index--;
            }
            playerHomes[index].onDestroy();
            playerHomes.RemoveAt(index);
        }
        public void playerHomeReset()
        {
            for (int i = 0; i < playerHomes.Count; i++)
            {
                playerHomes[i].onDestroy();
            }
            playerHomes.Clear();
        }
        public void setPlayerHome(int index, Vector3 position, bool keepY = false)
        {
            if (index < 0 || index >= playerHomes.Count) return;
            if (playerHomes[index] == null)
            {
                return;
            }
            var p = position;
            if (keepY)
            {
                p.y = playerHomes[index].center.y;
            }
            playerHomes[index].center = position;
            playerHomes[index].updatePositions(handlePlayerHomesRadius);
        }
        public PlayerHome getPlayerHome(int index)
        {
            return playerHomes[index];
        }
        #region position update
        public bool PlayerPosition_GOTO(int playerHomeIndex, int[] playerIndex, int nodeIndex)
        {
            if (playerHomes == null || nodes == null || playerIndex == null || playerIndex.Length < 1) return false;
            for (int i = 0; i < playerHomes.Count; i++)
            {
                if (playerHomes[i].index == playerHomeIndex)
                {
                    return PlayerPosition_GOTO(playerHomes[i], playerIndex, nodeIndex);
                }
            }
            return false;
        }
        private bool PlayerPosition_GOTO(PlayerHome home, int[] playerIndex, int nodeIndex)
        {
            if (nodes == null || playerIndex == null || playerIndex.Length < 1) return false;
            bool result = false;
            ElementNode node;
            for (int i = 0; i < nodes.Count; i++)
            {
                if ((node = elementNodesManager.getNode(i)) == null
                    || node.index != nodeIndex) continue;
                for (int j = 0; j < playerIndex.Length; j++)
                {
                    result |= PlayerPosition_GOTO(home, playerIndex[j], node.point);
                }
            }
            return result;
        }
        private bool PlayerPosition_GOTO(PlayerHome home, int playerIndex, Vector3 position)
        {
            if (home == null) return false;
            Player p = home.getPlayer(playerIndex);
            if (p == null) return false;
            p.GoTo_Immediately(position);
            return true;
        }
        #endregion
        #endregion
        #region shape
        public void CircleShape(CircleStruct cs)
        {
            if (points.Count < 1) return;
            cs.center = points.ToArray().ToCenter();
            CircleMaker c = new CircleMaker();
            points = c.CreateCircle(cs.radius, cs.center, points.Count);
            updatePrefabs();
        }
        public void SquareShape(SquareStruct cs)
        {
            if (points.Count < 4) return;
            cs.center = points.ToArray().ToCenter();
            cs.b = cs.a;
            SquareMaker s = new SquareMaker();
            points = s.CreateSquare(cs, points.Count);
            updatePrefabs();
        }
        public void PolygonShape(PolygonStruct ps)
        {
            if (points.Count < 2 || ps.edges < 3 || points.Count < ps.edges) return;
            ps.center = points.ToArray().ToCenter();
            PolygonMaker s = new PolygonMaker();
            points = s.CreatePolygon(ps, points.Count);
            updatePrefabs();
        }
        public void RectangleShape(SquareStruct cs)
        {
            if (points.Count < 4) return;
            cs.center = points.ToArray().ToCenter();
            SquareMaker s = new SquareMaker();
            points = s.CreateSquare(cs, points.Count);
            updatePrefabs();
        }
        public void TriangleShape(TriangleStruct cs)
        {
            if (points.Count < 3) return;
            cs.center = points.ToArray().ToCenter();
            TriangleMaker s = new TriangleMaker();
            points = s.CreateTriangle(cs, points.Count);
            updatePrefabs();
        }
        public void EllipseShape(EllipseStruct es)
        {
            if (points.Count < 1) return;
            es.center = points.ToArray().ToCenter();
            EllipseMaker e = new EllipseMaker();
            points = e.CreateEllipse(es, points.Count);
            updatePrefabs();
        }
        public void DiamondShape(DiamondStruct ds)
        {
            if (points.Count < 4) return;
            ds.center = points.ToArray().ToCenter();
            DiamondMaker d = new DiamondMaker();
            points = d.CreateDiamond(ds, points.Count);
            updatePrefabs();
        }
        public void LineShape(LineStruct ls)
        {
            if (points.Count < 2) return;
            ls.center = points.ToArray().ToCenter();
            LineMaker d = new LineMaker();
            points = d.CreateLine(ls, points.Count);
            updatePrefabs();
        }
        public void SetNewCenter(Vector3 position)
        {
            this.gameObject.transform.position = position;
            Vector3 oldCenter = points.ToArray().ToCenter();
            int i;
            oldCenter.y = 0;
            for (i = 0; i < points.Count; i++)
            {
                Vector3 d = (points[i] - oldCenter);
                setPoint(i, d + position);
            }
            for (i = 0; i < playerHomes.Count; i++)
            {
                Vector3 d = (playerHomes[i].center - oldCenter);
                playerHomes[i].center = d + position;
            }
        }
        public void ChangeNodesScale(float deltaX)
        {
            Vector3 center = points.ToArray().ToCenter();
            int i;
            for (i = 0; i < points.Count; i++)
            {
                Vector3 d = (points[i] - center);
                float distance = Vector3.Distance(points[i], center) + deltaX;
                if (distance <= 0) continue;
                Vector3 newPosition = d.normalized * (distance) + center;
                setPoint(i, newPosition);
            }
            for (i = 0; i < playerHomes.Count; i++)
            {
                Vector3 d = (playerHomes[i].center - center);
                float distance = Vector3.Distance(playerHomes[i].center, center) + deltaX;
                if (distance <= 0) continue;
                playerHomes[i].center = d.normalized * (distance) + center;
            }
        }
        public void AssignPrefabForAllNodes(int index)
        {
            ElementNode node;
            if ((node = elementNodesManager.getNode(index)) == null)
            {
                node = nodes[index].AddComponent<ElementNode>();
            }
            var obj = node.prefab;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == index) continue;
                if (nodes[i] == null) initNode(i, points[i]);
                if ((node = elementNodesManager.getNode(i)) == null)
                {
                    node = nodes[i].AddComponent<ElementNode>();
                }
                node.UpdatePrefab(obj);
            }
        }
        public void updatePrefabs()
        {
            ElementNode node;
            for (int i = 0; i < points.Count; i++)
            {
                if ((node = elementNodesManager.getNode(i)) == null) continue;
                node.setPosition(points[i]);
                if (autoRotation)
                {
                    //node.setRotation(getPointRotation(i));
                }
            }
        }
        public void updatePrefabs(Quaternion r)
        {
            ElementNode node;
            for (int i = 0; i < points.Count; i++)
            {
                if ((node = elementNodesManager.getNode(i)) == null) continue;
                node.setPosition(points[i]);
                node.setRotation(r);
            }
        }
        #endregion
        #region controller
        private void rolling()
        {
            if (!allowRolling) return;
            switch (playerHomes[playerHomeIndex].playerMode)
            {
                case PlayerMode.Human:
                    // if (Input.GetKeyDown(rollingKeyCode))
                    // {
                    //     diceManager.setFlag(true);
                    //     allowRolling = false;
                    // }
                    if (RollDice.CanRoll)
                    {
                        diceManager.setFlag(true);
                        allowRolling = false;
                        RollDice.CanRoll = false;
                    }
                    break;
                case PlayerMode.CPU:
                    ranint();
                    break;
            }
        }
        public void garbageCollector()
        {
            Transform[] ts = this.transform.GetComponentsInChildren<Transform>();
            if (ts == null || ts.Length < 1) return;
            List<GameObject> childs = new List<GameObject>();

            for (int i = 0; i < ts.Length; i++)
            {
                if (ts[i] == null || ts[i].gameObject == null || ts[i] == this.transform) continue;
                if (ts[i].gameObject.name.IndexOf("_Home ") == 0)
                {
                    childs.Add(ts[i].gameObject);
                }
            }
            ElementNode node;
            foreach (var item in childs)
            {
                if ((node = item.GetComponent<ElementNode>()) == null || nodes.IndexOf(item) < 0)
                {
                    DestroyImmediate(item);
                }
            }
        }
        #endregion
        #region save / load
        #region save
        public bool Save()
        {
            return Save(SaveLoadKey);
        }
        public bool Save(string key)
        {
            if (!saveLoadEnterCheck(key))
            {
                Debug.LogError("Save faild.");
                return false;
            }
            isSaveLoad = true;
            save(key);
            if (playerHomes != null)
            {
                for (int i = 0; i < playerHomes.Count; i++)
                {
                    if (playerHomes[i] == null) continue;
                    playerHomes[i].Save(key + i.ToString());
                }
            }
            Debug.Log("Save function complete.");
            return !(isSaveLoad = false);
        }
        private void save(string key)
        {
            PlayerPrefs.SetInt(getKey(key, "playerHomeIndex"), playerHomeIndex);
            PlayerPrefs.Save();
        }
        #endregion
        #region load
        public bool Load()
        {
            return Load(SaveLoadKey);
        }
        public bool Load(string key)
        {
            if (!saveLoadEnterCheck(key))
            {
                Debug.LogError("Load faild.");
                return false;
            }
            isSaveLoad = true;
            load(key);
            if (playerHomes != null)
            {
                for (int i = 0; i < playerHomes.Count; i++)
                {
                    if (playerHomes[i] == null) continue;
                    playerHomes[i].Load(key + i.ToString());
                }
            }
            Debug.Log("Load function complete.");
            return !(isSaveLoad = false);
        }
        private void load(string key)
        {
            var k1 = getKey(key, "playerHomeIndex");
            if (PlayerPrefs.HasKey(k1))
            {
                playerHomeIndex = PlayerPrefs.GetInt(k1, -1);
            }
        }
        #endregion
        #region ForceToLoadOnStart
        public static void ForceToLoadOnStart()
        {
            ForceToLoadOnStart(1);
        }
        private static void ForceToLoadOnStart(int val)
        {
            PlayerPrefs.SetInt(getKey(SaveLoadKey, "ForceToLoad"), val);
            PlayerPrefs.Save();
        }
        private bool checkForceToLoadOnStart()
        {
            var key = getKey(SaveLoadKey, "ForceToLoad");
            if (PlayerPrefs.HasKey(key))
            {
                ForceToLoadOnStart(0);
                return true;
            }
            return false;
        }
        #endregion
        private static string getKey(string key, string variableName)
        {
            return key + "_ElementNodeCreator_" + variableName;
        }
        private bool saveLoadEnterCheck(string key)
        {
            bool result = true;
            if (isSaveLoad)
            {
                result = false;
                Debug.LogWarning("Game currently is in Save/Load state.");
            }
            else if (diceManager == null)
            {
                result = false;
                Debug.LogWarning("Dice manager is null.");
            }
            else if (diceManager.getDiceState() != DiceState.Ready)
            {
                result = false;
                Debug.LogWarning("Game is not in Saveable/Loadable state. Save/Load state is only when dice(s) ready to rolling.");
            }
            ///
            if (string.IsNullOrEmpty(key))
            {
                result = false;
                Debug.LogWarning("Save/Load key is null or empty.");
            }
            return result;
        }
        #endregion
        #region events
        private void InvokeEvents(UnityEvent[] events)
        {
            if (events == null) return;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i] == null) continue;
                events[i].Invoke();
            }
        }
        #endregion
        #endregion
        /***************************************************
                        추가된 함수 및 변수들
        ***************************************************/

        public int playerTrapped;
        public ElementNodeType nodeType;
        public int nodeIndex;
        public EndTurn endturn;
        public CameraSwitcher cameraSwitcher;
        public PS ps;
        public bool isWaiting = false;
        private float delayTimer = 0;
        private Action continuationAction;
        private bool initialActionsPerformed = false;
        public int[] CPUdicesValues;
        private Inventory inventory;
        private GameMaterial material;
        private Turn turn;
        private AttackManage Atkmanage;
        private CollectManager Cltmanage;
        private GameObject UIPanel;
        private TownUI townUI;
        public ElementNodePrefabMapper prefabMapper;
        private ElementNode NewNode;
        private string nodeTag;
        public StructureManage structureManage;
        public RollDice RollDice;
        public SkipTurn skipTurn;
        public int restTrap;
        public void UpdateBoardSpaces()
        {
            Player[] players = FindObjectsOfType<Player>();
            int totalSpaces = nodes.Count; // nodes 리스트의 길이 추출
            foreach (Player player in players)
            {
                player.totalBoardSpaces = totalSpaces; // 각 플레이어에게 totalSpaces 할당
            }
        }

        public int[] ranint()
        {
            int diceValue = UnityEngine.Random.Range(10, 10);
            CPUdicesValues = new int[] { diceValue }; // 주사위 값 설정
            diceManager.setFlag(true);
            allowRolling = false;
            return CPUdicesValues;
        }

        private void IncreseETurn()
        {
            turn.IncreaseE_Turn();
        }
        private void IncreaseFakePTurn()
        {
            turn.IncreaseFakeP_Turn();
        }

        public void Setpanel(GameObject UIpanel)
        {
            UIPanel = UIpanel;
        }
        private void ChangeNodeTypeForTag(string tag, ElementNodeType newType)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                if (obj == null)  // 오브젝트가 파괴되었는지 체크
                {
                    continue;
                }
                if (obj.GetComponent<ElementNode>() != null)
                {
                    ElementNode elementNode = obj.GetComponent<ElementNode>();
                    if (elementNode != null)
                    {
                        elementNode.setElementNodeType(newType);
                    }
                }
            }
            if (tag == "Town1")
            {
                CheckAndSwapObjects(tag);
            }
            else if (tag == "Town2")
            {
                CheckAndSwapObjects(tag);
            }
            else if (tag == "Town3")
            {
                CheckAndSwapObjects(tag);
            }
            else if (tag == "Town4")
            {
                CheckAndSwapObjects(tag);
            }
        }
        public void ChangeNodeTypeForIndex(ElementNode node, int index, ElementNodeType newType)
        {
            if (node == null)  // 오브젝트가 파괴되었는지 체크
            {
                
            }
            if (node.GetComponent<ElementNode>() != null)
            {
                ElementNode elementNode = nodes[index].GetComponent<ElementNode>();
                if (elementNode != null)
                {
                    elementNode.setElementNodeType(newType);
                }
            }
        }
        private void CheckAndSwapObjects(string tag)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            GameObject n = null;
            if (tag == "Town1")
            {
                n = structureManage.Town1;
            }
            else if (tag == "Town2")
            {
                n = structureManage.Town2;
            }
            else if (tag == "Town3")
            {
                n = structureManage.Town3;
            }
            else if (tag == "Town4")
            {
                n = structureManage.Town4;
            }
            foreach (GameObject obj in taggedObjects)
            {
                ElementNode elementNode = obj.GetComponent<ElementNode>();
                if (elementNode != null && elementNode.getElementNodeType() == ElementNodeType.Ruins)
                {
                    SwapTownWithRuin(n);
                    break; // 조건을 만족하는 첫 번째 노드만 변경하고 종료
                }
            }
        }
        private void SwapTownWithRuin(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Vector3 position = gameObject.transform.position;
                Quaternion rotation = gameObject.transform.rotation;
                Destroy(gameObject); // Town 오브젝트를 파괴
                Instantiate(structureManage.Ruin, position, rotation); // Ruin 오브젝트를 생성
            }
        }
        public void MakeRuins()
        {
            if (NewNode != null)
            {
                if (NewNode.elementNodeType == ElementNodeType.BeforeBreak)
                {
                    ChangeNodeTypeForTag(nodeTag, ElementNodeType.Ruins);
                }
            }
        }
        public void RollDiceEvent()
        {
            Debug.Log("ReRoll!!");
            Atkmanage.CanAttack = false;
            endturn.IsTurnEnd = true;
            RollDice.CanRoll = true;
            rolling();
        }
        /// <summary>
        /// 플레이어에게 버프를 주는 함수
        /// <para>버프/디버프 이름 리스트</para>
        /// <para>- "공격력 증가"</para>
        /// <para>- "방어력 증가"</para>
        /// <para>- 버프 이름 리스트</para>
        /// <para>- 버프 이름 리스트</para>
        /// </summary>
        /// <param name="player">플레이어 지정</param>
        /// <param name="buffName">버프 이름 지정</param>
        /// <param name="buffturn">버프 턴수 지정</param>
        /// <param name="buffstat">버프 증가치 지정</param>
        public void BufforDebuffPlayer(Player player, string buffName, int buffturn, int buffstat)
        {
            Debug.Log($"{buffName}!!");
            player.ps.AddBuff(buffName, buffstat, buffturn + 1); // 버프 추가
            endturn.IsTurnEnd = false;
        }
        public void SetTrap(ElementNode elementNode, Player player, ElementNodeType elementNodeType, int trapDuration, int nodeIndex)
        {
            player.ps.AddTrap(elementNode, elementNodeType, trapDuration, nodeIndex);
            endturn.IsTurnEnd = false;
        }
        public void GetGold(Player player, int Gold)
        {
            Debug.Log("Get Gold!!");
            player.ps.GetGoldCion(Gold);
            endturn.IsTurnEnd = false;
        }
        public string GetRandomMat()
        {
            List<string> combinedList = new List<string>(material.forest);
            combinedList.AddRange(material.mine);

            int randomIndex = UnityEngine.Random.Range(0, combinedList.Count);
            endturn.IsTurnEnd = false;
            return combinedList[randomIndex];
        }
        /// <summary>
        /// 플레이어를 앞 혹은 뒤로 움직이게 하는 함수
        /// </summary>
        /// <param name="player">이동시킬 플레이어 오브젝트</param>
        /// <param name="movecount">플레이어가 움직일 칸 수</param>
        private void MoveMore(Player player, int movecount)
        {
            RoutingMode routingMode1 = RoutingMode.Direct;
            int randomrouting = UnityEngine.Random.Range(1, 2); // 앞으로 갈지 뒤로갈지 결정
            if (randomrouting == 0)
            {
                routingMode1 = RoutingMode.Direct;
            }
            else if (randomrouting == 1)
            {
                routingMode1 = RoutingMode.Reverse;
            }
            Atkmanage.CanAttack = false;
            endturn.IsTurnEnd = true;
            //get dice
            player.diceValues = new int[]{movecount};//diceManager.getDicesValues();
            //calculate indexes
            player.CalculatePositionIndex(ref nodes, routingMode1, addUniqueIndex);
            //Select Path
            if (player.hasPath())
            {
                if (player.pathManager.Paths.Count > 1)
                {
                    showPaths(ref player);
                    player.playerState = PlayerState.SelectPath;
                }
                else
                {
                    player.GoTo_CalculatedIndexes(player.pathManager.Paths[0], ref points);
                }
            }
            else
            {
                Debug.Log("No path is exist.");
                playerAndBoardReset(player);
            }
            StartCoroutine(Call(player, movecount * 0.3f));
            if (player.playerState == PlayerState.SelectPath)
            {
                checkForHitGhost(player);
            }
        }
        /// <summary>
        /// 플레이어 강제 이동
        /// </summary>
        /// <param name="player"></param>
        /// <param name="movecount"></param>
        private void ForceMoveMore(Player player, int movecount)
        {
            RoutingMode routingMode1 = RoutingMode.Direct;
            int randomrouting = UnityEngine.Random.Range(0, 1); // 앞으로 갈지 뒤로갈지 결정
            if (randomrouting == 0)
            {
                routingMode1 = RoutingMode.Direct;
            }
            else if (randomrouting == 1)
            {
                routingMode1 = RoutingMode.Reverse;
            }
            //get dice
            player.diceValues = new int[]{movecount};//diceManager.getDicesValues();
            //calculate indexes
            player.CalculatePositionIndex(ref nodes, routingMode1, addUniqueIndex);
            //Select Path
            if (player.hasPath())
            {
                if (player.pathManager.Paths.Count > 1)
                {
                    showPaths(ref player);
                    player.playerState = PlayerState.SelectPath;
                }
                else
                {
                    player.GoTo_CalculatedIndexes(player.pathManager.Paths[0], ref points);
                }
            }
            else
            {
                Debug.Log("No path is exist.");
                playerAndBoardReset(player);
            }
        }
        private System.Collections.IEnumerator Call(Player player, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayerState_MovingComplete(player);
        }
    }
}