using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyDice.Board;
using TMPro;
using JetBrains.Annotations;
using MyDice.Players;

public class EnemyDiceValue : MonoBehaviour
{
    [HideInInspector]public Playermanager playermanager;
    [HideInInspector]public ElementNodeCreator creator;
    public TextMeshProUGUI diceValuesText; // Text 컴포넌트

    void Start()
    {
        playermanager = FindObjectOfType<Playermanager>();
        creator = FindObjectOfType<ElementNodeCreator>();
    }

    public void Update()
    {
        UIonoff();
        if (creator != null && diceValuesText != null)
        {
            // CPUdicesValues 배열을 문자열로 변환하여 UI 텍스트에 할당
            diceValuesText.text = " 적 주사위 수: " + string.Join(", ", creator.CPUdicesValues);
        }
    }
    public void UIonoff()
    {
        switch(playermanager.currentPlayerIndex)
        {
            case 0:
                diceValuesText.gameObject.SetActive(false);
                break;
            case 1:
                diceValuesText.gameObject.SetActive(true);
                break;
        }
    }
}
