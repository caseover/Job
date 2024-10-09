using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using System.Text.RegularExpressions;

// 퀘스트 관리
namespace Rito.InventorySystem.Quest
{
    public class QuestManager : MonoBehaviour
    {
        public Button QuestButton;
        public GameObject questImage;
        public TownUI townUI;
        public Button[] targetButtons; // 버튼 배열
        public TMP_Text questText; // 퀘스트 설명 텍스트 (Quest라는 이름을 가진 TMP_Text)
        public TMP_Text rewardText; // 퀘스트 보상 텍스트(Reward라는 이름을 가진 TMP_Text)
        public List<string> Quest_1st;
        public string Quest = null;
        public string Reward = null;
        public int totalReward;
        public GameMaterial material;
        public string randomKeyword;
        public string randomRwdKeyword;
        public int randomCntIndex;
        public int QuestIndex;
        public List<(string, int)> Reward_3rd;
        private ColorBlock NormalColor; // 원래 색상을 저장할 변수
        private List<(string, int)> itemName;
        private List<string> Quest_3rd_Reward_Keywords = new List<string>
        {
            "소형 체력 포션", "소형 마나 포션"
        };
        private Dictionary<string, int> rewardValues = new Dictionary<string, int>
        {
            { "사과", 3 },    // 사과 하나당 3 골드
            { "나무", 5 },    // 나무 하나당 5 골드
            { "버섯", 7 },    // 버섯 하나당 7 골드
        };
        private Dictionary<string, string> Quest_3rd = new Dictionary<string, string>
        {
            { "사과 3개 모으기","소형 체력 포션 1개" },
        };
        void Start() // questText에 들어갈 문장 지정
        {
            QuestButton.interactable = false;
            townUI = FindFirstObjectByType<TownUI>();
            NormalColor = targetButtons[0].colors;
            material = FindFirstObjectByType<GameMaterial>();
            Quest_1st = new List<string>
            {
                "사과 3개, 나무 2개 모으기", //"나무 5개 모으기"
            };
            InitializeButtons();
        }
        void InitializeButtons()
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                int index = i;
                targetButtons[i].onClick.AddListener(() => StartQuest(index));
            }
        }
        public void StartQuest(int questIndex)
        {
            QuestButton.interactable = true;
            QuestIndex = questIndex;
            questText = targetButtons[questIndex].transform.Find("Quest").GetComponent<TMP_Text>();
            rewardText = targetButtons[questIndex].transform.Find("Reward").GetComponent<TMP_Text>();

            if (questText != null)
            {
                Quest = questText.text; // Quest 변수에 텍스트 저장
                Reward = rewardText.text; // Reward 변수에 텍스트 저장
                var keywordsList = material.GetCombinedMaterials();
                string[] keywords = keywordsList.ToArray();
                // var keywords = new string[] { "사과", "나무", "버섯" };
                itemName = ExtractKeywordsAndCounts(Quest, keywords);
                Debug.Log("현재 퀘스트: " + Quest);
            }
            else
            {
                Debug.LogError("Quest라는 이름을 가진 텍스트 오브젝트를 찾을 수 없습니다.");
            }
            DisableOtherButtons(questIndex);
        }
        void DisableOtherButtons(int activeIndex)
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (i != activeIndex)
                {
                    // originalColor = targetButtons[i].colors.normalColor;
                    targetButtons[i].interactable = false;
                    targetButtons[i].gameObject.SetActive(false);
                    // ColorBlock colorBlock = targetButtons[i].colors;
                    // colorBlock.disabledColor = new Color(1.0f, 0.5f, 0.5f); // 연한 빨간색
                    // targetButtons[i].colors = colorBlock;
                }
                else
                {
                    // 클릭된 버튼은 활성화된 상태로 유지하되, 클릭할 수 없도록 함
                    targetButtons[i].interactable = false; // 클릭을 막기 위해 interactable을 false로 설정
                    ColorBlock colorBlock = targetButtons[i].colors;
                    // colorBlock.disabledColor = targetButtons[i].colors.normalColor; // 색상 유지
                    colorBlock.disabledColor = new Color(0.7f, 1.0f, 0.7f); // 연한 연두색
                    targetButtons[i].colors = colorBlock;
                }
            }
        }
        public void RestoreButtonColors()
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                targetButtons[i].gameObject.SetActive(true);
                targetButtons[i].interactable = true; // 버튼을 다시 활성화

                // 원래 저장된 색상으로 복원
                targetButtons[i].colors = NormalColor;
            }
        }
        /// <summary>
        /// 버튼을 눌러 길드에 입장할시 버튼마다 새로운 퀘스트를 부여함
        /// </summary>
        public void CreateQuest()
        {
            List<string> remainingTexts = new List<string>(Quest_1st);
            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (i == 0)
                {
                    if (targetButtons[i] == null)
                    {
                        Debug.LogError("Button not assigned.");
                        continue;
                    }
                    
                    TMP_Text questTextComponent = null;
                    TMP_Text rewardTextComponent = null;

                    TMP_Text[] buttonTexts = targetButtons[i].GetComponentsInChildren<TMP_Text>();
                    if (buttonTexts.Length == 0)
                    {
                        Debug.LogError("TMP_Text component not found on the button.");
                        continue;
                    }
                    // GameObject 이름이 "Quest" 와 "Reward" 인 TMP_Text를 찾아서 텍스트 설정
                    foreach (TMP_Text buttonText in buttonTexts)
                    {
                        if (buttonText.gameObject.name == "Quest")
                        {
                            questTextComponent = buttonText;
                        }
                        else if (buttonText.gameObject.name == "Reward")
                        {
                            rewardTextComponent = buttonText;
                        }
                    }
                    SetRandomText(questTextComponent, remainingTexts);

                    // 생성된 퀘스트 텍스트에서 키워드와 개수를 추출
                    var keywords = rewardValues.Keys.ToArray();
                    List<(string, int)> itemRequirements = ExtractKeywordsAndCounts(questTextComponent.text, keywords);

                    totalReward = CalculateReward(itemRequirements);
                    rewardTextComponent.text = $"보상 : {totalReward} Gold";
                }
                if (i == 1)
                {
                    var keywordsList = material.GetCombinedMaterials();
                    List<string> QuestKeyWords = keywordsList;
                    int randomMatIndex = UnityEngine.Random.Range(0, QuestKeyWords.Count);
                    //랜덤 퀘스트 키워드 고르기
                    randomKeyword = QuestKeyWords[randomMatIndex];
                    // 보상을 위해 퀘스트 키워드 제외하기 후 보상 설정하기
                    QuestKeyWords.RemoveAt(randomMatIndex);
                    int randomRwdIndex = UnityEngine.Random.Range(0, QuestKeyWords.Count);
                    randomRwdKeyword = QuestKeyWords[randomRwdIndex];
                    // 요구 개수 랜덤 지정
                    randomCntIndex = UnityEngine.Random.Range(1, 3);

                    TMP_Text questTextComponent = null;
                    TMP_Text rewardTextComponent = null;

                    TMP_Text[] buttonTexts = targetButtons[i].GetComponentsInChildren<TMP_Text>();
                    if (buttonTexts.Length == 0)
                    {
                        Debug.LogError("TMP_Text component not found on the button.");
                        continue;
                    }
                    // GameObject 이름이 "Quest" 와 "Reward" 인 TMP_Text를 찾아서 텍스트 설정
                    foreach (TMP_Text buttonText in buttonTexts)
                    {
                        if (buttonText.gameObject.name == "Quest")
                        {
                            questTextComponent = buttonText;
                        }
                        else if (buttonText.gameObject.name == "Reward")
                        {
                            rewardTextComponent = buttonText;
                        }
                    }
                    questTextComponent.text = $"{randomKeyword} {randomCntIndex}개 모으기";
                    rewardTextComponent.text = $"보상 : {randomRwdKeyword} {randomCntIndex}개";
                }
                if (i == 2)
                {
                    List<string> keys = new List<string>(Quest_3rd.Keys);
                    int randomIndex = UnityEngine.Random.Range(0, keys.Count);
                    string randomQuest = keys[randomIndex];
                    string randomReward = Quest_3rd[randomQuest];

                    TMP_Text questTextComponent = null;
                    TMP_Text rewardTextComponent = null;
                    TMP_Text[] buttonTexts = targetButtons[i].GetComponentsInChildren<TMP_Text>();

                    foreach (TMP_Text buttonText in buttonTexts)
                    {
                        if (buttonText.gameObject.name == "Quest")
                        {
                            questTextComponent = buttonText;
                        }
                        else if (buttonText.gameObject.name == "Reward")
                        {
                            rewardTextComponent = buttonText;
                        }
                    }
                    questTextComponent.text = $"{randomQuest}";
                    rewardTextComponent.text = $"보상 : {randomReward}";
                    var keywords = Quest_3rd_Reward_Keywords.ToArray();
                    Reward_3rd = ExtractKeywordsAndCounts(rewardTextComponent.text, keywords);
                }
            }
        }




        private int CalculateReward(List<(string, int)> items)
        {
            int totalReward = 0;

            foreach (var item in items)
            {
                if (rewardValues.ContainsKey(item.Item1))
                {
                    totalReward += rewardValues[item.Item1] * item.Item2;
                }
            }

            return totalReward;
        }
        void SetRandomText(TMP_Text textComponent, List<string> remainingTexts)
        {
            if (remainingTexts.Count == 0)
            {
                Debug.LogError("No more texts available to assign.");
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, remainingTexts.Count);
            textComponent.text = remainingTexts[randomIndex];

            // 선택된 텍스트를 리스트에서 제거
            // remainingTexts.RemoveAt(randomIndex);
        }
        public static List<(string, int)> ExtractKeywordsAndCounts(string text, params string[] keywords)
        {
            // 쉼표가 없는 경우, 전체 텍스트를 하나의 요소로 간주
            string[] segments = text.Contains(",") ? text.Split(',') : new string[] { text };

            // 결과를 저장할 리스트
            List<(string keyword, int count)> results = new List<(string, int)>();

            // 각 분리된 텍스트 조각에 대해 반복
            foreach (var segment in segments)
            {
                // 각 키워드에 대해 해당 키워드가 텍스트에 포함되어 있는지 확인
                foreach (var keyword in keywords)
                {
                    if (segment.Contains(keyword))
                    {
                        // 키워드가 포함되어 있다면, 해당 문자열에서 숫자 추출
                        var match = Regex.Match(segment, @"\d+");
                        if (match.Success)
                        {
                            // 숫자가 있으면, 결과 리스트에 추가
                            int count = int.Parse(match.Value);
                            results.Add((keyword, count));
                        }
                    }
                }
            }
            return results;
        }
        public List<(string, int)> GetItemName()
        {
            return itemName;
        }
        private int ExtractQuantity(string quest)
        {
            // 정규 표현식을 사용하여 숫자 부분을 추출
            Match match = Regex.Match(quest, @"\d+");
            if (match.Success)
            {
                return int.Parse(match.Value); // 숫자를 int로 변환하여 반환
            }
            return 0; // 숫자가 없는 경우 0 반환
        }
    }
}