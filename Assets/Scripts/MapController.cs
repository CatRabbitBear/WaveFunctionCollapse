using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public WaveFuncCollapse waveFunc;
    public int gridSize;
    public int prefabWidth = 3;
    public Tile[,] Grid;
    public GameObject fourWay;
    public GameObject threeWay;
    public GameObject bend;
    public GameObject straight;
    public GameObject room;
    
    // The map (grid) is generated in WaveFuncCollapse script in its Awake method, this
    // means it is guaranteed to be ready to be accessed in this Start method
    //
    // This is the code that takes the information encoded in Grid and its Tile's
    // and Instantiates the correct prefabs in the correct orientation.
    void Start()
    {
        waveFunc = GameObject.Find("WaveFunction").GetComponent<WaveFuncCollapse>();
        gridSize = waveFunc.gridSize;
        Grid = waveFunc.Grid;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                string prefabType = Grid[i,j].Placeholder.PrefabType;
                if (prefabType == null) Debug.Log("prefabType is null!");
                
                // Check needed to prevent an attempt to instantiate nothing.
                if (prefabType == "Empty")
                {
                    continue;
                }
                else
                {
                    GameObject prefab = GetPrefabFromTile(prefabType);
                    Vector3 prefabPosition = new Vector3((j + 1) * prefabWidth, 0, -(i + 1) * prefabWidth);
                    Quaternion rotation = Quaternion.Euler(0f, Grid[i, j].Placeholder.Rotation, 0f);
                    Instantiate(prefab, prefabPosition, rotation );
                }
            }
        }
    }

    // Eventually this will be replaced by a more robust adapter in the Tile script.
    // Currently this maps prefabs to strings stored in PrefabPlaceholder instances.
    GameObject GetPrefabFromTile(string prefabType)
    {
        switch (prefabType)
        {
            case "Straight":
                return straight;
            case "Bend":
                return bend;
            case "ThreeWay":
                return threeWay;
            case "FourWay":
                return fourWay;
            case "Room":
                return room;
            default:
                Debug.Log("Default selected in GetPrefabFromTile");
                return fourWay;
        }
    }
}
