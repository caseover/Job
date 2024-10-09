using System.Collections;
using System.Collections.Generic;
using MyDice.Board;
using UnityEngine;
using MyDice.Players;
using UnityEngine.UI;

public class SkipTurn : MonoBehaviour
{
    public Player player;
    public ElementNodeCreator creator;
    public Button skipTurnButton;
    public RollDice rollDice;
    public Turn turn;
    void Awake()
    {
        turn = FindObjectOfType<Turn>();
        rollDice = FindObjectOfType<RollDice>();
        skipTurnButton.interactable = false;
        creator = FindObjectOfType<ElementNodeCreator>();
    }
    public void SkipTurnButton()
    {
        player.diceValues = new int[] {0,0};
        Player player1 = creator.playerHomes[creator.playerHomeIndex].getCandidatePlayer();
        creator.PlayerState_MovingComplete(player1);
        rollDice.rollDiceButton.interactable = false;
        skipTurnButton.interactable = false;
        turn.IncreaseFakeP_Turn();
    }
    public void TrappedSkip()
    {
        player.diceValues = new int[] {0,0};
        Player player1 = creator.playerHomes[creator.playerHomeIndex].getCandidatePlayer();
        creator.PlayerState_MovingComplete(player1);
        rollDice.rollDiceButton.interactable = false;
        skipTurnButton.interactable = false;
    }
}
