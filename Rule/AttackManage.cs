using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDice.Players;
using System;
using UnityEngine.UI;
using MyDice.Board;

public class AttackManage : MonoBehaviour
{
    public bool CanAttack = false;
    public Player p;
    public Button attackButton;
    private ElementNodeCreator creator;
    public TownUI townUI;
    private int minDistance;
    void Awake()
    {
        attackButton.interactable = false;
        CanAttack = false;
        townUI = FindObjectOfType<TownUI>();
        creator = FindObjectOfType<ElementNodeCreator>();
    }
    void Update()
    {
        // 매 프레임마다 버튼의 활성화 상태를 업데이트
        UpdateButtonState();
    }
    public void Attack()
    {
        // 거리가 3 이하일 때만 공격
        if (minDistance <= 3)
        {
            int damage = Math.Max(0, p.ps.Atk - p.OtherPlayer.ps.Def);
            p.OtherPlayer.ps.HP -= damage;
            Debug.Log($"Attacked {p.OtherPlayer.name} for {damage} damage. {p.OtherPlayer.name} now has {p.OtherPlayer.ps.HP} HP.");
            p.ps.AP -= 1;
            p.ps.Atkchance -= 1;
        }
        
    }
    public void CalculateDistance()
    {
        int currentIndex = p.currentPositionIndex;
        int otherIndex = p.OtherPlayer.currentPositionIndex;

        int forwardDistance = (otherIndex - currentIndex + p.totalBoardSpaces) % p.totalBoardSpaces;
        int backwardDistance = (currentIndex - otherIndex + p.totalBoardSpaces) % p.totalBoardSpaces;

        // 두 방향 중 더 짧은 거리를 선택
        minDistance = Math.Min(forwardDistance, backwardDistance);
    }
    private void UpdateButtonState()
    {
        if (CanAttack == true && townUI.isUIOpen == false)
        {
            if (minDistance <= 3)
            {
                switch(creator.playerHomes[creator.playerHomeIndex].playerMode)
                {
                    case PlayerMode.Human:
                        if (p.ps.AP >= 1 && p.ps.Atkchance >= 1)
                        {
                            attackButton.interactable = true;
                        }
                        else
                        {
                            attackButton.interactable = false;
                        }
                        break;
                    case PlayerMode.CPU:
                        attackButton.interactable = false;
                        break;
                }
            }
            else
            {
                attackButton.interactable = false;
            }       
        }
    }
}