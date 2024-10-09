using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turn : MonoBehaviour
{
    public int Real_P_turn = 1;
    public int Fake_P_turn = 1;
    public int E_turn = 1;
    public int Buffed_turn = 0;
    public delegate void OnTurnChange();
    public event OnTurnChange onTurnChange;

    // turn 값을 변경하는 메소드
    public void IncreaseE_Turn()
    {
        E_turn++;
    }
    public void IncreaseRealP_Turn()
    {
        Real_P_turn++;
        onTurnChange?.Invoke();  // 이벤트 발생
    }
    public void IncreaseFakeP_Turn()
    {
        Fake_P_turn++;
    }
}