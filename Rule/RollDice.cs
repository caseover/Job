using System.Collections;
using System.Collections.Generic;
using MyDice.Board;
using UnityEngine;
using UnityEngine.UI;

public class RollDice : MonoBehaviour
{
    public bool CanRoll = false;
    public Button rollDiceButton;
    public ElementNodeCreator creator;
    public SkipTurn skipTurn;
    void Awake()
    {
        skipTurn = FindObjectOfType<SkipTurn>();
        rollDiceButton.interactable = false;
        creator = FindObjectOfType<ElementNodeCreator>();
    }
    void Update()
    {
        
    }
    public void RollDiceButton()
    {
        skipTurn.skipTurnButton.interactable = false;
        CanRoll = true;
        rollDiceButton.interactable = false;
    }
}
