using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public string BuffName { get; private set; }   // 버프 이름
    public int BuffStat { get; private set; }      // 버프가 증가시키는 스탯
    public int RemainingTurns { get; private set; }// 남은 턴 수
    public BuffManager(string buffName, int buffStat, int duration)
    {
        BuffName = buffName;
        BuffStat = buffStat;
        RemainingTurns = duration;
    }

    // 턴을 갱신하고 남은 턴이 0 이하인지 확인
    public bool UpdateBuff()
    {
        RemainingTurns--;
        return RemainingTurns <= 0; // 턴이 끝나면 true 반환
    }
}
