using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rito.InventorySystem.Quest;
using Unity.VisualScripting;

public class CameraSwitcher : MonoBehaviour
{
    public Camera mainCamera;
    public TownUI townUI;
    public Canvas MainCanvas;
    public Canvas GulidCanvas;
    public Canvas QuestCanvas;
    public QuestManager questmanager;
    public bool IsMainCameraActive => mainCamera.enabled = true;

    void Awake()
    {
        // shopManager = FindObjectOfType<ShopManager>();
        townUI = FindObjectOfType<TownUI>();
        questmanager = FindObjectOfType<QuestManager>();
        MainCanvas.gameObject.SetActive(true);
        GulidCanvas.gameObject.SetActive(false);
        QuestCanvas.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);
    }
    /// <summary>
    /// 상점 입장 버튼
    /// </summary>
    
    /// <summary>
    /// 상점 나가기 버튼
    /// </summary>
    
    /// <summary>
    /// 길드 UI 활성화 버튼
    /// </summary>
    public void SwitchToGulidUI()
    {
        MainCanvas.gameObject.SetActive(false);
        GulidCanvas.gameObject.SetActive(true);
    }
    /// <summary>
    /// 현재 퀘스트 확인 버튼
    /// </summary>
    public void GulidToTown()
    {
        MainCanvas.gameObject.SetActive(true);
        GulidCanvas.gameObject.SetActive(false);
    }
    public void CurrentQuestUI()
    {
        MainCanvas.gameObject.SetActive(false);
        QuestCanvas.gameObject.SetActive(true);
    }
    public void ExitCurrentQuestUI()
    {
        MainCanvas.gameObject.SetActive(true);
        QuestCanvas.gameObject.SetActive(false);
    }
}
