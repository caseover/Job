using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestSound : MonoBehaviour
{
    public AudioSource audioSource; // AudioSource 컴포넌트
    public AudioClip buttonClickSound; // 버튼 클릭 사운드 클립
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    public void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound); // 사운드 재생
        }
        else
        {
            Debug.LogWarning("AudioSource 또는 AudioClip이 설정되지 않았습니다.");
        }
    }
}
