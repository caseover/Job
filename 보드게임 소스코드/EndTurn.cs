using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyDice.Board;
using MyDice.Players;

public class EndTurn : MonoBehaviour
{
    public PS ps;
    public Turn turn;
    public Player enemy; // 적(Enemy)
    public TownUI townUI;
    public CollectManager Cltmanage;
    public Button EndButton;
    public ElementNodeCreator creator;
    public bool IsTurnEnd = true;
    void Awake()
    {
        ps = FindFirstObjectByType<PS>();
        turn = FindObjectOfType<Turn>();
        townUI = FindObjectOfType<TownUI>();
        Cltmanage = FindObjectOfType<CollectManager>();
        creator = FindObjectOfType<ElementNodeCreator>();
    }
    public void End_Turn()
    {   
        Cltmanage.DeActiveButton(); // 버튼 비활성화
        turn.IncreaseRealP_Turn(); // 턴 수 증가
        // ps.UpdateBuffs();
        if (creator.restTrap > 0)
        {
            creator.restTrap --;
        }
        if (creator.playerTrapped > 0)
        {
            creator.playerTrapped --;
        }
        enemy.ps.UpdateTraps();
        // 사이클 포인트가 6이면 다음 플레이어로 넘어가지 않도록 조건 추가
        if (enemy.cycle != 6) // enemy.cycle가 6이 아닐 때만 다음 플레이어로 전환
        {
            creator.nextPlayer(); // 다음 플레이어로 전환
        }
        else
        {
            Debug.Log("사이클 포인트가 6이므로 다음 플레이어로 넘어가지 않습니다."); // 조건이 충족되지 않았을 때의 메시지
        }

        creator.MakeRuins(); // 폐허 만들기
        Debug.Log("click"); // 클릭 로그 출력
        Setup(false); // UI 설정 업데이트
    }
    void Start()
    {
        Setup(false);
    }
    void Update()
    {
        if(IsTurnEnd == false)
        {
            Setup(true);
        }
    }
    public void Setup(bool isActive)
    {
        switch(creator.playerHomes[creator.playerHomeIndex].playerMode)
        {
            case PlayerMode.Human:
                if (EndButton != null && creator.isWaiting == false && townUI.isUIOpen == false)
                {
                    // Button의 interactable 속성을 false로 설정하여 비활성화합니다.
                    EndButton.interactable = isActive;
                }
                break;
            case PlayerMode.CPU:
                EndButton.interactable = false;
                break;
        }
    }
}