using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaterial : MonoBehaviour
{
    public List<string> forest = new List<string>() {"가죽", "사과", "버섯", "나무"};
    public List<string> mine = new List<string>() {"석탄","동광석", "은광석", "금광석"};

    public List<string> GetCombinedMaterials()
    {
        List<string> combinedMaterials = new List<string>(forest);
        combinedMaterials.AddRange(mine); // forest 리스트에 mine 리스트를 추가
        return combinedMaterials;
    }
}