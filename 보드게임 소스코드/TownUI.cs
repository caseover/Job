using System;
using System.Collections;
using System.Collections.Generic;
using MyDice.Board;
using UnityEngine;
using UnityEngine.UI;
using Rito.InventorySystem.Quest;

public class TownUI : MonoBehaviour
{
    public QuestManager questManager;
    public Button rollDiceButton;
    public Button skipTurnButton;
    public ElementNodeCreator creator;
    public GameObject Selectpanel;
    public GameObject Shoppanel;
    public GameObject Guildpanel;
    public GameObject Churchpanel;
    public Button ExitButton;
    [HideInInspector] public AttackManage attackManage;
    [HideInInspector] public CollectManager collectManager;
    [HideInInspector] public EndTurn endTurn;

    public bool isUIOpen = false;
    public AudioClip buttonClickSound; // 버튼 클릭 시 재생할 오디오 클립
    public AudioClip exitButtonClickSound; // Exit 버튼 클릭 시 재생할 다른 오디오 클립
    [SerializeField]
    private AudioSource audioSource;

    void Awake()
    {
        questManager = FindObjectOfType<QuestManager>();
        creator = FindObjectOfType<ElementNodeCreator>();
        endTurn = FindObjectOfType<EndTurn>();
        collectManager = FindObjectOfType<CollectManager>();
        attackManage = FindObjectOfType<AttackManage>();
        creator.Setpanel(Selectpanel);
        Selectpanel.SetActive(false);
        Shoppanel.SetActive(false);
        Guildpanel.SetActive(false);
        Churchpanel.SetActive(false);
        ExitButton.gameObject.SetActive(false);

        // 오디오 소스 컴포넌트 추가
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void CloseUI() // 아니오를 눌렀을 때
    {
        Selectpanel.SetActive(false);
        isUIOpen = false;
    }

    public void MoveToTownUI() // 예를 눌렀을 때
    {
        if (questManager.questText == null)
        {
            Debug.Log("퀘스트 생성됨");
            questManager.CreateQuest();
        }
        Selectpanel.SetActive(false);
        rollDiceButton.gameObject.SetActive(false);
        skipTurnButton.gameObject.SetActive(false);
        Shoppanel.SetActive(true);
        Guildpanel.SetActive(true);
        Churchpanel.SetActive(true);
        ExitButton.gameObject.SetActive(true);
        attackManage.attackButton.gameObject.SetActive(false);
        collectManager.collectButton.gameObject.SetActive(false);
        endTurn.EndButton.gameObject.SetActive(false);
        questManager.QuestButton.gameObject.SetActive(false);
    }

    public void CloseTownUI() // Exit버튼을 눌렀을 때
    {
        Shoppanel.SetActive(false);
        Guildpanel.SetActive(false);
        Churchpanel.SetActive(false);
        rollDiceButton.gameObject.SetActive(true);
        skipTurnButton.gameObject.SetActive(true);
        ExitButton.gameObject.SetActive(false);
        attackManage.attackButton.gameObject.SetActive(true);
        collectManager.collectButton.gameObject.SetActive(true);
        endTurn.EndButton.gameObject.SetActive(true);
        questManager.QuestButton.gameObject.SetActive(true);
        isUIOpen = false;

        // Exit 버튼을 눌렀을 때 다른 소리를 재생합니다.
        PlayDifferentButtonClickSound();
    }

    // 예 버튼을 클릭하면 버튼 클릭 소리를 재생
    public void PlayButtonClickSound()
    {
        if (buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    // Exit 버튼을 눌렀을 때 다른 소리를 재생하는 함수
    public void PlayDifferentButtonClickSound()
    {
        if (exitButtonClickSound != null)
        {
            audioSource.PlayOneShot(exitButtonClickSound);
        }
    }
}