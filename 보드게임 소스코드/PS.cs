using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDice.Players;
using System.Linq;
using Unity.Mathematics;
using TMPro;
using MyDice.Board;
using Unity.VisualScripting;

public class PS : MonoBehaviour
{
    [SerializeField]public static List<PS> Instances = new List<PS>();
    public ElementNodeCreator elementNodeCreator;
    public ShopSystem shopSystem;
    public Turn turnManager;
    #region 스테이터스
    [Header("스테이터스")]
    public int Max_HP = 100;
    public int HP = 100;
    public int Atk = 10;
    public int Def = 5;
    #region 플레이어 전용 스탯
    [Header("플레이어 전용 스탯")]
    public int Gold_Coin = 0;
    public int Max_AP = 2;
    public int Max_Atkchance = 1;
    public int AP = 2; //활동 포인트
    public int Atkchance = 1; //공격 기회
    #endregion
    #region 적 전용 스탯
    [Header("적 전용 스탯")]
    public int breakpoint = 1;
    public int bossPhase = 1;
    #endregion
    #endregion
    [SerializeField]private List<BuffManager> activeBuffs = new List<BuffManager>(); // 활성화된 버프 목록
    public List<BossTrapmanager> activeTraps = new List<BossTrapmanager>();
    void Awake()
    {
        elementNodeCreator = FindObjectOfType<ElementNodeCreator>();
        turnManager = FindObjectOfType<Turn>();
        Instances.Add(this);
    }
    void Start()
    {
        // player = GetComponent<Player>();
        if (turnManager != null)
        {
            turnManager.onTurnChange += ResetAP;  // 이벤트 구독
            turnManager.onTurnChange += ResetAtkchance;  // 이벤트 구독
        }     
    }
    void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.onTurnChange -= ResetAP;  // 이벤트 구독 해제
            turnManager.onTurnChange -= ResetAtkchance;  // 이벤트 구독 해제
        }
    }
    public void DecreaseHP(int damage)
    {
        HP -= damage;
        HP = Mathf.Max(HP, 0);
    }

    public void IncreaseHP(int amount)
    {
        HP += amount;
        HP = Mathf.Min(HP, Max_HP);
    }
    
    public void IncreaseDef(int amount)
    {
        Def += amount;
    }
    public void DecreaseDef(int amount)
    {
        Def -= amount;
    }
    public void IncreaseAtk(int amount)
    {
        Atk += amount;
    }
    public void DecreaseAtk(int amount)
    {
        Atk -= amount;
    }
    public void IncreaseMax_HP(int amount)
    {
        Max_HP += amount;
        HP += amount;
    }
    public void DecreaseMax_HP(int amount)
    {
        Max_HP -= amount;
        HP -= amount;
    }
    public void GetGoldCion(int amount)
    {
        Gold_Coin += amount;
        shopSystem.ReturnMoney(Gold_Coin);
    }
    public void LoseGoldCion(int amount)
    {
        Gold_Coin -= amount;
        shopSystem.ReturnMoney(Gold_Coin);
    }
    public int GoldAmount()
    {
        return Gold_Coin;
    }
    public void ReturnGold(int money)
    {
        Gold_Coin = money;
    }
    public void IncreseMax_AP(int amount)
    {
        Max_AP += amount;
        AP += amount;
    }
    void ResetAP()
    {
        AP = Max_AP;  // AP를 초기 값으로 재설정
    }
    void ResetAtkchance()
    {
        Atkchance = Max_Atkchance;  // Atkchance를 초기 값으로 재설정
    }
    public void DecreseBreakpoint()
    {
        breakpoint--;  // breakpoint를 감소
        breakpoint = Mathf.Max(breakpoint, 0);
    }
    public void ResetBreakpoint()
    {
        breakpoint = 1;  // breakpoint를 초기 값으로 재설정
    }
    // 버프 추가
    public void AddBuff(string buffName, int buffStat, int duration)
    {
        BuffManager newBuff = new BuffManager(buffName, buffStat, duration);
        activeBuffs.Add(newBuff);
        
        switch(buffName)
        {
            case "공격력 증가":
            IncreaseAtk(buffStat); // 공격력 증가 적용
            break;
            case "방어력 증가":
            IncreaseDef(buffStat);
            break ;
        }
    }
    // 턴이 지나면 버프를 업데이트 (만료된 버프는 제거)
    public void UpdateBuffs()
    {
        List<BuffManager> expiredBuffs = new List<BuffManager>(); // 만료된 버프를 임시로 저장할 리스트

        foreach (BuffManager buff in activeBuffs)
        {
            if (buff.UpdateBuff()) // 버프가 만료되면 효과 제거
            {
                expiredBuffs.Add(buff);
                switch(buff.BuffName)
                {
                    case "공격력 증가":
                    DecreaseAtk(buff.BuffStat);
                    break;
                    case "방어력 증가":
                    DecreaseDef(buff.BuffStat);
                    break ;
                }
            }
        }

        // 만료된 버프 리스트에서 제거
        foreach (BuffManager expiredBuff in expiredBuffs)
        {
            activeBuffs.Remove(expiredBuff);
        }
    }
    public void AddTrap(ElementNode elementNode,ElementNodeType elementNodeType, int trapDuration, int nodeIndex)
    {
        BossTrapmanager newTrap = new BossTrapmanager(elementNode, elementNodeType, trapDuration, nodeIndex);
        activeTraps.Add(newTrap);
    }
    public void UpdateTraps()
    {
        List<BossTrapmanager> expiredTraps = new List<BossTrapmanager>(); // 만료된 덫를 임시로 저장할 리스트
        if (activeTraps.Count > 0)
        {
            foreach (BossTrapmanager Trap in activeTraps)
            {
                if (Trap.UpdateTrap()) // 덫이 만료되면 효과 제거
                {
                    expiredTraps.Add(Trap);
                }
            }
        }
        
        if (expiredTraps.Count > 0)
        {
            // 만료된 덫 리스트에서 제거
            foreach (BossTrapmanager expiredTrap in expiredTraps)
            {
                ElementNode node = expiredTrap.Elementnode;
                ElementNodeType elementNodeType = expiredTrap.ElementnodeType;
                int nodeIndex = expiredTrap.NodeIndex;
                if (activeTraps.Contains(expiredTrap))
                {
                    elementNodeCreator.ChangeNodeTypeForIndex(node, nodeIndex, elementNodeType);
                    activeTraps.Remove(expiredTrap); // 만료된 덫을 리스트에서 제거
                }
            }
        }
    }
}
