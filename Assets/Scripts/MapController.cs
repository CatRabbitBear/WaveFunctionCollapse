using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController Instance;
    public WaveFuncCollapse waveFunc;
    private int _gridWidth;
    private int _gridHeight;
    private int _prefabLength;
    private Dictionary<PrefabAdapter, Tuple<int, int>> _presetRooms;
    public Tile[,] Grid;
    public List<Tile> AllTiles;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // The map (grid) is generated in WaveFuncCollapse script in its Awake method, this
    // means it is guaranteed to be ready to be accessed in this Start method
    //
    // This is the code that takes the information encoded in Grid and its Tile's
    // and Instantiates the correct prefabs in the correct orientation.
    void Start()
    {
        waveFunc = GameObject.Find("WaveFunction").GetComponent<WaveFuncCollapse>();
        _gridWidth = Tileset.Instance.gridWidth;
        _gridHeight = Tileset.Instance.gridHeight;
        _prefabLength = Tileset.Instance.prefabLength;
        _presetRooms = Tileset.Instance.PresetRooms;
        
        CreateNewGrid();
        foreach (var kvp in _presetRooms)
        {
            var tile = Grid[kvp.Value.Item1, kvp.Value.Item2];
            waveFunc.UpdateTilePlacement(tile, kvp.Key);
        }
        Grid = waveFunc.CreateMap(Grid, AllTiles);
        CreateMapFromGrid();
    }
    
    void CreateNewGrid()
    {
        AllTiles = new List<Tile>();
        Grid = new Tile[_gridHeight, _gridWidth];
        for (int i = 0; i < _gridHeight; i++)
        {
            for (int j = 0; j < _gridWidth; j++)
            {
                Tile newTile = new Tile(i, j);
                Grid[i, j] = newTile;
                AllTiles.Add(newTile);
            }
        }
    }
    
    void CreateMapFromGrid()
    {
        for (int i = 0; i < _gridHeight; i++)
        {
            for (int j = 0; j < _gridWidth; j++)
            {
                GameObject prefabType = Grid[i,j].Adapter.PrefabType;
                if (prefabType == null) Debug.Log("prefabType is null!");
                
                // Check needed to prevent an attempt to instantiate nothing.
                if (prefabType.name == "empty")
                {
                    continue;
                }
                else
                {
                    //GameObject prefab = GetPrefabFromTile(prefabType);
                    Vector3 prefabPosition = new Vector3((j + 1) * _prefabLength, 0, -(i + 1) * _prefabLength);
                    Quaternion rotation = Quaternion.Euler(0f, Grid[i, j].Adapter.Rotation, 0f);
                    Instantiate(prefabType, prefabPosition, rotation );
                }
            }
        }
    }
    
}
