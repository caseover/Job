using System.Collections;
using System.Collections.Generic;
using MyDice.Board;
using MyDice.Players;
using UnityEngine;
using UnityEngine.UI;
using Rito.InventorySystem;

public class CollectManager : MonoBehaviour
{
    [SerializeField]
    private InventoryTester inventoryTester;
    [SerializeField]
    private ElementNodeCreator creator;
    private GameMaterial material;
    private PS ps;
    public Player player;
    public ElementNode currentNode;
    public Button collectButton;
    public AudioClip forestSound;
    public AudioClip caveSound;
    public AudioClip eventSound; // Event 소리를 위한 AudioClip 추가
    private AudioSource audioSource;

    [HideInInspector]
    public ElementNodesManager elementNodesManager;

    void Awake()
    {
        ps = FindObjectOfType<PS>();
        material = FindFirstObjectByType<GameMaterial>();
        audioSource = player.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = player.gameObject.AddComponent<AudioSource>();
        }

        collectButton.interactable = false;
    }

    void Update()
    {
        UpdateButtonState();
    }

    public void ActiveButton(ElementNode node)
    {
        currentNode = node;
        collectButton.interactable = true;
    }

    public void DeActiveButton()
    {
        collectButton.interactable = false;
    }

    public void CollectMaterial()
    {
        if (currentNode != null)
        {
            switch (currentNode.elementNodeType)
            {
                case ElementNodeType.Forest:
                    int forestIndex = Random.Range(0, material.forest.Count);
                    string forestItem = material.forest[forestIndex];
                    switch (forestItem)
                    {
                        case "달걀":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[19]);
                        break;
                        case "사과":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[20]);
                        break;
                        case "버섯":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[21]);
                        break;
                        case "나무":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[22]);
                        break;
                        case "가죽":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[23]);
                        break;
                    }
                    Debug.Log(forestItem + " 을 획득하였습니다");
                    break;
                    
                case ElementNodeType.Mine:
                    int mineIndex = Random.Range(0, material.mine.Count);
                    string mineItem = material.mine[mineIndex];
                    switch (mineItem)
                    {
                        case "석탄":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[15]);
                        break;
                        case "동광석":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[16]);
                        break;
                        case "은광석":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[17]);
                        break;
                        case "금광석":
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[18]);
                        break;
                    }
                    Debug.Log( mineItem + " 을 획득하였습니다");
                    break;
            }
            player.ps.AP -= 1;
        }
        else
        {
            Debug.Log("버그");
        }
    }

    // 버튼을 클릭할 때 호출되는 함수
    public void OnButtonClick()
    {
        if (currentNode != null)
        {
            Debug.Log("Current node tag: " + currentNode.gameObject.tag);
            switch (currentNode.gameObject.tag)
            {
                case "Forest":
                    PlaySound(forestSound);
                    break;
                case "Cave":
                    // Cave 태그인 경우 소리를 3번 재생
                    StartCoroutine(PlaySoundMultipleTimes(caveSound, 3));
                    break;
                case "Event":
                    // Event 태그인 경우 소리를 재생
                    PlaySound(eventSound);
                    break;
                default:
                    Debug.LogWarning("No sound assigned for the tag: " + currentNode.gameObject.tag);
                    break;
            }
        }
        else
        {
            Debug.LogWarning("Current node is null.");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Audio clip is not assigned.");
        }
    }

    private IEnumerator PlaySoundMultipleTimes(AudioClip clip, int times)
    {
        for (int i = 0; i < times; i++)
        {
            PlaySound(clip);
            yield return new WaitForSeconds(clip.length);
        }
    }

    private void UpdateButtonState()
    {
        switch (creator.playerHomes[creator.playerHomeIndex].playerMode)
        {
            case PlayerMode.Human:
                if (player.ps.AP < 1)
                {
                    collectButton.interactable = false;
                }
                break;
            case PlayerMode.CPU:
                collectButton.interactable = false;
                break;
        }
    }
}
