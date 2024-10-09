using System.Collections;
using System.Collections.Generic;
using MyDice.Board;
using UnityEngine;

public class BossTrapmanager : MonoBehaviour
{
    public ElementNode Elementnode{ get; private set; }
    public ElementNodeType ElementnodeType{ get; private set; }
    public int TrapDuration{ get; private set; }
    public int NodeIndex{ get; private set; }
    public BossTrapmanager(ElementNode elementNode, ElementNodeType elementNodeType, int trapDuration, int nodeIndex)
    {
        Elementnode = elementNode;
        ElementnodeType = elementNodeType;
        TrapDuration = trapDuration;
        NodeIndex = nodeIndex;
    }
    public bool UpdateTrap()
    {
        TrapDuration--;
        return TrapDuration <= 0; // 턴이 끝나면 true 반환
    }
}
