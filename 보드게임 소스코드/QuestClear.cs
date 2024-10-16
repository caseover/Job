using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rito.InventorySystem;
using System;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UI;

// 퀘스트 클리어 여부
namespace Rito.InventorySystem.Quest
{
    public class QuestClear : MonoBehaviour
    {
        public Button clearButton;
        public QuestSound questSound;
        private PopupText popupText;
        private TMP_Text CurrentQuest = null;
        private TMP_Text Reward = null;
        private TMP_Text QuestCount1 = null;
        private TMP_Text QuestCount2 = null;
        public TownUI townUI;
        [SerializeField]
        private InventoryTester inventoryTester;
        [SerializeField]
        private PS ps;
        [SerializeField]
        private Inventory inventory;
        [SerializeField]
        private List<ItemSlotUI> itemSlots;
        [SerializeField]
        private CameraSwitcher cameraSwitcher;
        public List<(string,int)> itemnames;
        public InventoryUI inventoryUI;
        public QuestManager questManager;
        public string quest_item_name1 = null;
        public string quest_item_name2 = null;
        public int quest_item_count1 = 0;
        public int quest_item_count2 = 0;
        public int quest_item_needed1 = 0;
        public int quest_item_needed2 = 0;
        /// <summary>
        /// 이 딕셔너리에는 보상 아이템의 이름과 그 아이템의 ID를 넣으면 됨.
        /// <para>아이템의 아이디는 InventoryTester.cs 에 _itemDataArray 을 참조.</para>
        /// </summary> 
        private Dictionary<string,int> ItemId = new Dictionary<string, int>
        {
            {"사과",20},
            {"버섯",21},
            {"나무",22},
            {"가죽",23},
            {"석탄",15},
            {"동광석",16},
            {"은광석",17},
            {"금광석",18},
            {"소형 체력 포션",4}
        };
        private void Awake()
        {
            clearButton.onClick.AddListener(QuestC1ear);
            questSound= FindFirstObjectByType<QuestSound>();
            popupText = FindFirstObjectByType<PopupText>();
            townUI = FindFirstObjectByType<TownUI>();
            inventoryTester = FindFirstObjectByType<InventoryTester>();
            GameObject playerObject = GameObject.Find("플레이어");
            ps = playerObject.GetComponentInChildren<PS>();
            cameraSwitcher = FindObjectOfType<CameraSwitcher>();
            inventoryUI = FindObjectOfType<InventoryUI>();
            questManager = FindObjectOfType<QuestManager>();

            if (inventoryUI != null)
            {
                itemSlots = inventoryUI.GetAllSlots();
                inventory = inventoryUI.GetInventory();
            }
            else
            {
                Debug.LogError("InventoryUI를 찾을 수 없습니다.");
            }
        }
        public void 현재퀘스트정보()
        {
            cameraSwitcher.CurrentQuestUI();
            itemnames = questManager.GetItemName();
            ScanForItem(itemnames);
            CurrentQuest = null;
            Reward = null;
            QuestCount1 = null;
            QuestCount2 = null;
            TMP_Text[] imageTexts = questManager.questImage.GetComponentsInChildren<TMP_Text>();

            foreach(TMP_Text imagetext in imageTexts)
            {
                switch(imagetext.gameObject.name)
                {
                    case "CurrentQuest":
                        CurrentQuest = imagetext;
                    break;
                    case "Reward":
                        Reward = imagetext;
                    break;
                    case "QuestCount1":
                        QuestCount1 = imagetext;
                    break;
                    case "QuestCount2":
                        QuestCount2 = imagetext;
                    break;
                }
            }
            CurrentQuest.text = questManager.Quest;
            Reward.text = questManager.Reward;
            QuestCount1.text = $"{quest_item_name1} ({quest_item_count1}/{quest_item_needed1})";
            if (quest_item_name2 != "")
            {
                QuestCount2.text = $"{quest_item_name2} ({quest_item_count2}/{quest_item_needed2})";
            }
            else
            {
                QuestCount2.text = "";
            }
        }
        public void QuestC1ear()
        {
            itemnames = questManager.GetItemName();
            QuestC1ear(itemnames);
        }
        public void ScanForItem(List<(string, int)> itemNames)
        {   
            if (questManager.questText == null)
            {
                Debug.Log("No Quest");
                return;
            }
              
            // itemNames 리스트를 순회  
            for (int j= 0; j < itemNames.Count; j++)          
            // foreach (var itemName in itemNames)
            {
                int currentAmount = 0;
                bool itemFound = false; // 각 itemName에 대한 아이템 찾기 플래그

                // 인벤토리의 슬롯들을 순회하여 아이템이 존재하는지 확인
                for (int i = 0; i < itemSlots.Count; i++)
                {
                    string currentItemName = inventory.GetItemName(itemSlots[i].Index);

                    // 아이템 이름이 찾고자 하는 이름과 일치하는지 확인
                    if (currentItemName == itemNames[j].Item1)
                    {
                        itemFound = true;
                        // 아이템의 개수 확인
                        currentAmount = inventory.GetCurrentAmount(itemSlots[i].Index);
                        
                        // 해당 슬롯의 아이템 개수가 필요한 개수 이상인지 확인
                        if (currentAmount >= itemNames[j].Item2)
                        {
                            Debug.Log($"아이템 '{itemNames[j].Item1}'이(가) {itemSlots[i].Index + 1} 번째 슬롯에 있으며, {currentAmount}개 소지하고 있습니다.");
                        }
                        else
                        {
                            Debug.Log($"아이템 '{itemNames[j].Item1}'이(가) {itemSlots[i].Index + 1} 번째 슬롯에 있으나, 소지한 개수는 {currentAmount}개로, 필요한 개수 {itemNames[j].Item2}개보다 적습니다.");
                        }
                        break;
                    }
                }
                if (j == 0)
                {
                    quest_item_name1 = itemNames[j].Item1;
                    quest_item_needed1 = itemNames[j].Item2;
                    quest_item_count1 = currentAmount;
                }
                if (j == 1)
                {
                    quest_item_name2 = itemNames[j].Item1;
                    quest_item_needed2 = itemNames[j].Item2;
                    quest_item_count2 = currentAmount;
                }
                // 인벤토리의 모든 슬롯을 순회한 후에도 itemFound가 false면, 아이템을 찾지 못한 것
                if (!itemFound)
                {
                    Debug.Log($"아이템 '{itemNames[j].Item1}'을(를) 인벤토리에서 찾을 수 없습니다.");
                }
            }
            Debug.Log("검사가 완료되었습니다.");
        }
        public void QuestC1ear(List<(string, int)> itemNames)
        {         
            if (questManager.questText == null)
            {
                Debug.Log("No Quest");
                return;
            }
            Item[] items = inventory.GetItems();
            bool allItemsSufficient = true;
            int a = 0;
            int b = 0; 

            // 먼저 itemNames 리스트를 순회하여 각각의 아이템을 검사
            foreach (var itemName in itemNames)
            {
                bool itemFound = false; // 현재 아이템이 인벤토리에 있는지 여부를 추적

                // 슬롯들을 순회하며 아이템 이름을 확인
                for (int i = 0; i < itemSlots.Count; i++)
                {
                    string currentItemName = inventory.GetItemName(itemSlots[i].Index);

                    // 인벤토리에 해당 아이템이 있는 경우
                    if (currentItemName == itemName.Item1)
                    {
                        itemFound = true; // 아이템을 찾았음을 표시
                        int currentAmount = inventory.GetCurrentAmount(itemSlots[i].Index);

                        // 개수가 부족하면 함수 종료
                        if (currentAmount < itemName.Item2)
                        {
                            allItemsSufficient = false;
                            Debug.Log("아이템이 충분하지 않습니다.");
                            return;
                        }
                        if (a == 0)
                        {
                            a = i;
                        }else if(b == 0){
                            b = i;
                        }
                    }
                }

                // 아이템을 찾지 못한 경우 함수 종료
                if (!itemFound)
                {
                    Debug.Log("아이템이 충분하지 않습니다.");
                    return;
                }
            }
            List<int> SlotIndexs = new List<int> {a,b};

            // 모든 아이템이 충분할 경우에만 차감
            if (allItemsSufficient)
            {
                foreach (var itemName in itemNames)
                {
                    // for (int i = 0; i < itemSlots.Count; i++)
                    foreach(int i in SlotIndexs)
                    {
                        string currentItemName = inventory.GetItemName(itemSlots[i].Index);

                        if (currentItemName == itemName.Item1)
                        {
                            int currentAmount = inventory.GetCurrentAmount(itemSlots[i].Index);
                            int remainingAmount = currentAmount - itemName.Item2;

                            // SetItemAmount 메서드를 사용하여 남은 개수를 설정
                            CountableItem ci = items[i] as CountableItem;
                            ci.SetAmount(remainingAmount);
                            inventory.UpdateSlot(itemSlots[i].Index);

                            // 남은 개수가 0 이하일 경우 아이템을 제거
                            if (remainingAmount <= 0)
                            {
                                inventory.Remove(itemSlots[i].Index);
                            }

                            Debug.Log($"아이템 '{itemName.Item1}'의 {itemName.Item2}개를 퀘스트 완료를 위해 소모하였습니다. 남은 개수: {remainingAmount}개.");
                        }
                    }
                }
                switch (questManager.QuestIndex)
                {
                    case 0:
                        ps.GetGoldCion(questManager.totalReward);
                        questManager.questText = null;
                        questManager.RestoreButtonColors();
                        questManager.remainingTexts.Remove(questManager.remainingTexts[questManager.randomTextIndex]);
                    break;
                    case 1:
                        if (ItemId.ContainsKey(questManager.randomRwdKeyword))
                        {
                            int value = ItemId[questManager.randomRwdKeyword]; // 키에 해당하는 값 반환
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[value],questManager.randomCntIndex);
                            questManager.questText = null;
                            questManager.RestoreButtonColors();
                        }                    
                        else
                        {
                            Debug.Log("No KeyWord");
                        }
                    break;
                    case 2:
                        if (ItemId.ContainsKey(questManager.Reward_3rd[0].Item1))
                        {
                            int value = ItemId[questManager.Reward_3rd[0].Item1]; // 키에 해당하는 값 반환
                            inventoryTester._inventory.Add(inventoryTester._itemDataArray[value],questManager.Reward_3rd[0].Item2);
                            questManager.questText = null;
                            questManager.RestoreButtonColors();
                        }
                    break;
                }
                
                Debug.Log("퀘스트 아이템 차감이 완료되었습니다.");
                questManager.QuestButton.interactable = false;
                CurrentQuest.text = "현재 퀘스트";
                Reward.text = "보상";
                QuestCount1.text = "소지 개수 1";
                QuestCount2.text = "소지 개수 2";
                popupText.QuestCleared();
                cameraSwitcher.ExitCurrentQuestUI();
            }
            else
            {
                popupText.CantClear();
                Debug.Log("아이템이 충분하지 않습니다.");
            }
        }
    }
}