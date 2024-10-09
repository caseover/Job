using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopupText : MonoBehaviour
{
    // public TextMeshProUGUI popupTextPrefab; // 원래의 텍스트 프리팹
    public Transform popupParentQuest;      // 텍스트 팝업을 생성할 부모 트랜스폼
    public Transform popupParentMain;
    public Transform Inven;
    // public float fadeDuration = 1.2f;       // 서서히 사라지는 시간
    // public float moveDistance = 150f;       // 위로 이동하는 거리
    // public float popupDuration = 1.2f;      // 텍스트가 완전히 사라지기까지의 시간

    // public IEnumerator AnimatePopup(string message,Transform popupParent)
    // {
    //     // 텍스트 프리팹을 복사하여 새로운 텍스트 오브젝트 생성
    //     TextMeshProUGUI newPopupText = Instantiate(popupTextPrefab, popupParent);
    //     newPopupText.text = message;  // 새 텍스트에 메시지 설정

    //     CanvasGroup canvasGroup = newPopupText.GetComponent<CanvasGroup>();
    //     // 팝업을 처음 보이게 하고 투명도를 1로 설정
    //     canvasGroup.alpha = 1f;
    //     RectTransform rectTransform = newPopupText.rectTransform;
    //     Vector3 originalPosition = rectTransform.localPosition;

    //     float elapsedTime = 0f;
    //     Vector3 startPos = originalPosition;
    //     Vector3 endPos = startPos + new Vector3(0, moveDistance, 0); // 위로 이동할 위치

    //     while (elapsedTime < popupDuration)
    //     {
    //         elapsedTime += Time.deltaTime;

    //         // 텍스트의 위치를 서서히 위로 이동
    //         rectTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / popupDuration);

    //         // 서서히 투명해지게 만듦
    //         canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

    //         yield return null;
    //     }
    //     Destroy(newPopupText.gameObject);
    // }
    // public void CantClear()
    // {
    //     StartCoroutine(AnimatePopup("아이템이 충분하지 않습니다", popupParentQuest));
    // }
    // public void QuestCleared()
    // {
    //     StartCoroutine(AnimatePopup("퀘스트를 완료하였습니다!", popupParentMain));
    // }
    [Header("Notification Panel Settings")]
    public GameObject notificationPrefab; // 알림 패널 프리팹
    public float fadeDuration = 0.5f; // 투명해지거나 나타나는 데 걸리는 시간
    public float displayDuration = 2f; // 알림이 표시되는 시간

    public void ShowNotification(string message, Transform popupParent)
    {
        StartCoroutine(AnimateNotification(message, popupParent)); // 메시지를 코루틴에 전달
    }

    private IEnumerator AnimateNotification(string message, Transform popupParent)
    {
        // 프리팹을 인스턴스화하여 알림 패널 생성
        GameObject notificationPanel = Instantiate(notificationPrefab, popupParent);

        // RectTransform 설정 (여기서 크기를 지정)
        RectTransform rectTransform = notificationPanel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1224, 190); // 원하는 크기로 설정

        CanvasGroup canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        TextMeshProUGUI notificationText = notificationPanel.GetComponentInChildren<TextMeshProUGUI>();

        // 알림 메시지 설정
        notificationText.text = message;

        // 패널을 즉시 보이게 하고 투명도를 1로 설정
        canvasGroup.alpha = 1f; // 즉시 나타나도록 설정

        // 일정 시간 대기
        yield return new WaitForSeconds(displayDuration);

        float elapsedTime = 0f;

        // 패널이 서서히 사라지도록 애니메이션
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        // 패널 비활성화
        Destroy(notificationPanel); // 인스턴스화한 패널 제거
    }

    public void CantClear()
    {
        ShowNotification("아이템이 충분하지 않습니다", popupParentQuest);
    }

    public void QuestCleared()
    {
        ShowNotification("퀘스트를 완료하였습니다!", popupParentMain);
    }

    public void Test()
    {
        ShowNotification("테스트입니다", Inven);
    }

    public void NeedMoney()
    {
        ShowNotification("돈이 부족합니다!", Inven);
    }

    public void CheckInven()
    {
        ShowNotification("인벤토리를 확인해 주세요!", Inven);
    }

    public void SellEquip()
    {
        ShowNotification("장비 아이템은 판매 할 수 없습니다.", Inven);
    }

    public void ThrowEquip()
    {
        ShowNotification("장비 아이템은 버릴 수 없습니다.", Inven);
    }

    public void SortClick()
    {
        ShowNotification("인벤토리를 정리하였습니다.", Inven);
    }
}
