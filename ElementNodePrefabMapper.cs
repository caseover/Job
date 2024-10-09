using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyDice.Board
{
    public class ElementNodePrefabMapper : MonoBehaviour
    {
        public GameObject NonePrefab;
        public GameObject ResetPointPrefab;
        public GameObject RedirectPointPrefab;
        public GameObject InterruptPointPrefab;
        public GameObject TownPrefab;
        public GameObject ForestPrefab;
        public GameObject MinePrefab;
        public GameObject RuinsPrefab;
        public GameObject EventPrefab;
        public GameObject BeforeBreakPrefab;
        public GameObject TrapPrefab;

        private Dictionary<ElementNodeType, GameObject> prefabMap;

        void Awake()
        {
            prefabMap = new Dictionary<ElementNodeType, GameObject>
            {
                { ElementNodeType.None, NonePrefab },
                { ElementNodeType.ResetPoint, ResetPointPrefab },
                { ElementNodeType.RedirectPoint, RedirectPointPrefab },
                { ElementNodeType.InterruptPoint, InterruptPointPrefab },
                { ElementNodeType.Town, TownPrefab },
                { ElementNodeType.Forest, ForestPrefab },
                { ElementNodeType.Mine, MinePrefab },
                { ElementNodeType.Ruins, RuinsPrefab },
                { ElementNodeType.Event, EventPrefab },
                { ElementNodeType.BeforeBreak, BeforeBreakPrefab },
                { ElementNodeType.Trap, TrapPrefab },
            };
        }

        public GameObject GetPrefab(ElementNodeType type)
        {
            if (prefabMap.TryGetValue(type, out GameObject prefab))
            {
                return prefab;
            }
            return NonePrefab; // 기본 프리팹을 반환
        }
    }
}