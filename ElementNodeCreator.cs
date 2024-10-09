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
        [HideInInspector] public int playerHomeIndex = -1;
        [HideInInspector] public bool allowRolling = true;
        [HideInInspector] public ElementNodesManager elementNodesManager;
        #endregion
        //board
        public DiceManager diceManager;
        public bool addUniqueIndex;
        public UnityEvent[] onNextPlayerEvents;
        #endregion
        #endregion
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
        private void Update()
        {
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
            if (endturn.IsTurnEnd && cameraSwitcher.IsMainCameraActive && diceManager.getDiceState() == DiceState.Ready)
            {
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
            switch (node.elementNodeType)
            {
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
            switch (node.elementNodeType)
            {
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