using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDice.Players;
using UnityEngine.UI;
using MyDice;

public class InnManager : MonoBehaviour
{
    public Turn turn;
    public Button InnButton;
    public Player Player;
    public DiceManager diceManager;
    private bool DidUseInn = false;
    void Update()
    {
        if (Player.ps.Gold_Coin < 5 || Player.ps.HP >= Player.ps.Max_HP || DidUseInn)
        {
            InnButton.interactable = false;
        }
        else
        {
            InnButton.interactable = true;
        }
    }
    private void EnterInn()
    {  
        Player.ps.IncreaseHP(10);
        Player.ps.LoseGoldCion(5);
        Debug.Log("체력이 10 회복되었습니다.");
        IsInnAvailable(true);
    }
    public bool IsInnAvailable(bool YesNo)
    {
        DidUseInn = YesNo;
        return DidUseInn;
    } 
}
