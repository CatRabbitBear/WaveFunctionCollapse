using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// In abstract terms, the algorithm calculates on each pass how many possible tiles (prefabs and their rotations)
// can legally fit every spot. The grid position with the fewest possible options is next to 'collapse', if multiple 
// options (prefabs) exist then one is picked at random. This is done in a while loop until all spots in the grid are filled.
public class WaveFuncCollapse : MonoBehaviour
{
    // gridSize is width and height, gridSize x gridSize grid
    public int gridSize = 8;
    // 2D List of Tile instances
    public Tile[,] Grid;
    // Flat List of all Tiles, refs to same Tile's as in 2D version
    // Needed so can sort by how many possible TilePlaceholders can fit in its position
    public List<Tile> AllTiles;
    // tileset is original un-rotated prefabs encodings.
    public List<PrefabPlaceholder> Tileset = new List<PrefabPlaceholder>();
    // Method 'CalculateValidRotations' works out all valid rotations from original 'tileset' List
    // and stores them in 'tilesetWithRotions' This is the 'palette' from which 
    // the algorithm picks new tiles.
    public List<PrefabPlaceholder> TilesetWithRotations;

    private void Awake()
    {
        AllTiles = new List<Tile>();
        Grid = new Tile[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Tile newTile = new Tile(i, j);
                Grid[i, j] = newTile;
                AllTiles.Add(newTile);
            }
        }
        // Hardcoded mappings to prefabs in Unity, a bit fiddly but works. 
        // The List of 0's and 1's are {Up, Right, Down, Left}
        // In unity, {+z, +x, -z, -x}
        // 0 means no connection, 1 means connection to tile on that edge.
        Tileset.Add(new PrefabPlaceholder(new List<int>{0,0,0,0}, 0, "Empty"));
        Tileset.Add(new PrefabPlaceholder(new List<int>{1,0,1,0}, 0, "Straight"));
        Tileset.Add(new PrefabPlaceholder(new List<int>{0,1,1,0}, 0, "Bend"));
        Tileset.Add(new PrefabPlaceholder(new List<int>{0,1,1,1}, 0, "ThreeWay"));
        Tileset.Add(new PrefabPlaceholder(new List<int>{1,1,1,1}, 0, "FourWay"));
        
        // I need a PrefabPlaceholder for a room, but at this point I dont want to include it in the tileset
        // so I can manually choose where to place rooms.
        PrefabPlaceholder roomUp = new PrefabPlaceholder(new List<int> { 1, 0, 0, 0 }, 0, "Room");
        PrefabPlaceholder roomRight = new PrefabPlaceholder(new List<int> { 0, 1, 0, 0 }, 90, "Room");
        PrefabPlaceholder roomDown = new PrefabPlaceholder(new List<int> { 0, 0, 1, 0 }, 180, "Room");
        PrefabPlaceholder roomLeft = new PrefabPlaceholder(new List<int> { 0, 0, 0, 1 }, 270, "Room");
        
        //Manually placing some rooms
        UpdateTilePlacement(Grid[0,1], roomDown);
        UpdateTilePlacement(Grid[4,0], roomRight);

        TilesetWithRotations = new List<PrefabPlaceholder>();
        CalculateValidRotations();
        CreateMap();
    }

    // Two goofy error handling going on that needs work. 'safetNet' is a loop counter that breaks the loop
    // if appears to be infinite loop. And nextTile in initialised as null and is only assigned a Tile if 
    // there exists a Tile with more than 0 possibles. It works here because the tileset is complete (every
    // permutation exists), this wont always be the case, so needs a more robust handling of impossible combinations
    // of tiles.
    void CreateMap()
    {
        int uncollapsed = 0;
        int safetyNet = 0;
        foreach (var tile in AllTiles)
        {
            if (!tile.Collapsed) uncollapsed++;
        }

        while (uncollapsed > 0)
        {
            safetyNet++;
            UpdateSignaturesAndIndexes();
            AllTiles.Sort();
            Tile nextTile = null;
            foreach (var t in AllTiles)
            {
                if (t.Possibles > 0)
                {
                    nextTile = t;
                    break;
                }
            }

            if (nextTile == null)
            {
                Debug.Log("Next tile is null!!!");
                break;
            }
            if (nextTile.Possibles > 1)
            {
                int possiblesCount = nextTile.PossiblesIndexes.Count;
                int indexOfNextTile = Random.Range(0, possiblesCount);
                UpdateTilePlacement(nextTile, TilesetWithRotations[nextTile.PossiblesIndexes[indexOfNextTile]]);
            }
            else
            {
                UpdateTilePlacement(nextTile, TilesetWithRotations[nextTile.PossiblesIndexes[0]]);
            }
            int countUncollapsed = 0;
            foreach (var tile in AllTiles)
            {
                if (!tile.Collapsed) countUncollapsed++;
            }

            uncollapsed = countUncollapsed;
            if (safetyNet > gridSize * gridSize + gridSize)
            {
                Debug.Log("Safety net needed - possible infinite loop");
                break;
            }
        }

        for (int i = 0; i < gridSize; i++)
        {
            string rowString = "";
            for (int j = 0; j < gridSize; j++)
            {
                rowString += Grid[i, j].PrintTrueConnections() + " , ";
            }
            Debug.Log(rowString);
        }
    }

    // Method that iterates over a copy of the connections. popping last item and appending it
    // to the 0th position rotates its connections by 90 degrees.
    // Linq method SequenceEqual checks if two lists have the same elements in the same place,
    // isDuplicate flag is used to ensure symmetrical rotations are not added to tilesetWithRotations
    void CalculateValidRotations()
    {
        foreach (var tilePlaceholder in Tileset)
        {
            List<List<int>> added = new List<List<int>> { tilePlaceholder.Connections };
            TilesetWithRotations.Add(tilePlaceholder);
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
                    PrefabPlaceholder prefabVariant = new PrefabPlaceholder(new List<int>(connectionsCopy), rotation, tilePlaceholder.PrefabType);
                    added.Add(new List<int>(connectionsCopy));
                    TilesetWithRotations.Add(prefabVariant);
                }
            }
        }
    }

    // Flags a Tile instance in 2d and 1d Lists as collapsed (prefab assigned)
    // updates fields on Tile with information about prefab.
    void UpdateTilePlacement(Tile tile, PrefabPlaceholder placeholder)
    {
        // 'placeholder' is a useless name that needs refactoring when adapter is worked on.
        tile.Placeholder = placeholder;
        tile.Collapsed = true;
        // 'trueConnections' are just the final connections that prefab has.
        tile.TrueConnections = placeholder.Connections;
        // possibles set to 0, need to handle this carefully in CreatMap() because 
        // potential to bump these to front of List and get re-evaluated.
        tile.Possibles = 0;
    }

    // Some careful edge detection has to happen here. Currently if an edge is detected,
    // i.e row number is 0 means an edge 'above', then a zero is assigned.
    // This isn't a necessary requirement for a wave function collapse, but if creating a map
    // this prevents deadends.
    //
    // What is happening here is it checks 'is this an edge?', 'does a neighbouring tile have a prefab
    // assigned?' If there is a prefab assigned in a neighbouring tile it needs to match its connection.
    // So if a neighbour has a connection on the 'left', my signature needs to connect on the 'right'.
    List<int> CalculatePossibilitySignature(Tile tile)
    {
        List<int> possiblesSignature = new List<int>(){0,0,0,0};
        if (tile.Row > 0)
        {
            var tileUp = Grid[tile.Row - 1, tile.Col];
            if (tileUp.Collapsed)
            {
                possiblesSignature[0] = tileUp.TrueConnections[2];
            }
            else
            {
                possiblesSignature[0] = -1;
            }
        }
        else
        {
            possiblesSignature[0] = 0;
        }

        if (tile.Row == gridSize - 1)
        {
            possiblesSignature[2] = 0;
        }
        else
        {
            var tileDown = Grid[tile.Row + 1, tile.Col];
            if (tileDown.Collapsed)
            {
                possiblesSignature[2] = tileDown.TrueConnections[0];
            }
            else
            {
                possiblesSignature[2] = -1;
            }
        }

        if (tile.Col > 0)
        {
            var tileLeft = Grid[tile.Row, tile.Col - 1];
            if (tileLeft.Collapsed)
            {
                possiblesSignature[3] = tileLeft.TrueConnections[1];
            }
            else
            {
                possiblesSignature[3] = -1;
            }
        }
        else
        {
            possiblesSignature[3] = 0;
        }

        if (tile.Col == gridSize - 1)
        {
            possiblesSignature[1] = 0;
        }
        else
        {
            var tileRight = Grid[tile.Row, tile.Col + 1];
            if (tileRight.Collapsed)
            {
                possiblesSignature[1] = tileRight.TrueConnections[3];
            }
            else
            {
                possiblesSignature[1] = -1;
            }
        }

        return possiblesSignature;
    }

    // I'm sure there exists a better way to work out if a tile in the tilesetWithRotations
    // satisfies a possiblesSignature but for now this works (but is ugly code).
    List<int> CalculatePossibilityIndexes(Tile tile)
    {
        List<int> possibleIndexes = new List<int>();
        List<int> possiblesSignature = tile.ConnectionsNeeded;
        for (int i = 0; i < TilesetWithRotations.Count; i++)
        {
            var currentTileset = TilesetWithRotations[i].Connections;
            if (possiblesSignature[0] == -1 || possiblesSignature[0] == currentTileset[0])
            {
                if (possiblesSignature[1] == -1 || possiblesSignature[1] == currentTileset[1])
                {
                    if (possiblesSignature[2] == -1 || possiblesSignature[2] == currentTileset[2])
                    {
                        if (possiblesSignature[3] == -1 || possiblesSignature[3] == currentTileset[3])
                        {
                            possibleIndexes.Add(i);
                        }
                    }
                }
            }
        }

        if (possibleIndexes.Count == 0)
        {
            Debug.Log("Possible indexes 0!");
        }
        return possibleIndexes;
    }

    // Loops through grid and updates what that tile needs as connections 
    // and stores the list of indexes from 'tilesetWithRotations' that satisfy those needs.
    void UpdateSignaturesAndIndexes()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Tile currentTile = Grid[i, j];
                // Only update if tile in not collapsed
                if (!currentTile.Collapsed)
                {
                    currentTile.ConnectionsNeeded = CalculatePossibilitySignature(currentTile);
                    List<int> possibleIndexes = CalculatePossibilityIndexes(currentTile);
                    currentTile.PossiblesIndexes = possibleIndexes;
                    currentTile.Possibles = possibleIndexes.Count;
                }

            }
        }
    }
}

// --- Glossary ---
//
// uncollapsed : a Tile with collapsed == false. This means no prefab has been assigned to that spot yet.
// possiblesIndexes : indexes of positions in 'tilesetWithRotations' that could legally fill a grid location.
// possiblesSignature : A four digit list of 1's or 0's that show what connections are needed on that edge
//                      -1 is used as 'any', so {-1,-1,-1,-1} is the signature that any prefab can fit in that spot
