using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Tileset : MonoBehaviour
{
    public static Tileset Instance;

    public int gridHeight = 6;
    public int gridWidth = 6;
    public int prefabLength = 3;

    // literally an empty gameobject prefab to represent an empty space.
    public GameObject empty;
    
    public GameObject room;
    public GameObject bend;
    public GameObject straight;
    public GameObject threeWay;
    public GameObject fourWay;

    public Dictionary<GameObject, List<int>> SignatureMappingsDict;
    public List<PrefabAdapter> tileset;
    
    // Method 'CalculateValidRotations' works out all valid rotations from original 'tileset' List
    // and stores them in 'tilesetWithRotions' This is the 'palette' from which 
    // the algorithm picks new tiles.
    public List<PrefabAdapter> tilesetWithRotations;
    
    // All prefabs are needed to be able to Instantiate but it might not be prefferable
    // to have all prefabs included in the generation, for example if manual placement of 'rooms' is wanted.
    // In the editor, Add prefabs to the List below to exclude them from wave function collapse phase.
    public List<GameObject> exemptFromGeneration;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        SignatureMappingsDict = new Dictionary<GameObject, List<int>>
        {
            {empty, new List<int>{0,0,0,0}},
            {room, new List<int>{1,0,0,0}},
            {bend, new List<int>{0,1,1,0}},
            {straight, new List<int>{1,0,1,0}},
            {threeWay, new List<int>{0,1,1,1}},
            {fourWay , new List<int>{1,1,1,1}}
        };

        tileset = new List<PrefabAdapter>();
        foreach (var kvp in SignatureMappingsDict)
        {
            PrefabAdapter prefabAdapter = new PrefabAdapter(kvp.Value, 0, kvp.Key);
            tileset.Add(prefabAdapter);
        }

        tilesetWithRotations = new List<PrefabAdapter>();
        CalculateValidRotations();
    }
    
    // Method that iterates over a copy of the connections. popping last item and appending it
    // to the 0th position rotates its connections by 90 degrees.
    // Linq method SequenceEqual checks if two lists have the same elements in the same place,
    // isDuplicate flag is used to ensure symmetrical rotations are not added to tilesetWithRotations
    void CalculateValidRotations()
    {
        foreach (var tilePlaceholder in tileset)
        {
            if (exemptFromGeneration.Contains(tilePlaceholder.PrefabType)) continue;
            
            List<List<int>> added = new List<List<int>> { tilePlaceholder.Connections };
            tilesetWithRotations.Add(tilePlaceholder);
            List<int> connectionsCopy = new List<int>(tilePlaceholder.Connections);
            for (int i = 0; i < 3; i++)
            {
                bool isDuplicate = false;
                
                int lastIndex = connectionsCopy.Count - 1;
                int lastItem = connectionsCopy[lastIndex];
                connectionsCopy.RemoveAt(lastIndex);
                connectionsCopy.Insert(0, lastItem);
                foreach (var tileVariant in added)
                {
                    if (tileVariant.SequenceEqual(connectionsCopy))
                    {
                        isDuplicate = true;
                    }
                }

                if (!isDuplicate)
                {
                    int rotation = ((i + 1) * 90);
                    PrefabAdapter prefabVariant = new PrefabAdapter(new List<int>(connectionsCopy), rotation, tilePlaceholder.PrefabType);
                    added.Add(new List<int>(connectionsCopy));
                    tilesetWithRotations.Add(prefabVariant);
                }
            }
        }
    }
}
