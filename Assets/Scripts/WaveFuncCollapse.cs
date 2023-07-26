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
    // grid size is _gridWidth x _gridHeight
    private int _gridWidth;
    private int _gridHeight;
    
    public List<PrefabAdapter> TilesetWithRotations;

    private void Start()
    {
        TilesetWithRotations = Tileset.Instance.tilesetWithRotations;
        
        // I need a PrefabAdapter for a room, but at this point I dont want to include it in the tileset
        // so I can manually choose where to place rooms.
        // PrefabAdapter roomUp = new PrefabAdapter(new List<int> { 1, 0, 0, 0 }, 0, "Room");
        // PrefabAdapter roomRight = new PrefabAdapter(new List<int> { 0, 1, 0, 0 }, 90, "Room");
        // PrefabAdapter roomDown = new PrefabAdapter(new List<int> { 0, 0, 1, 0 }, 180, "Room");
        // PrefabAdapter roomLeft = new PrefabAdapter(new List<int> { 0, 0, 0, 1 }, 270, "Room");
        
        //Manually placing some rooms
        // UpdateTilePlacement(Grid[0,1], roomDown);
        // UpdateTilePlacement(Grid[4,0], roomRight);

        //TilesetWithRotations = new List<PrefabAdapter>();
        //CalculateValidRotations();
        //CreateMap();
    }

    // Two goofy error handling going on that needs work. 'safetyNet' is a loop counter that breaks the loop
    // if appears to be infinite loop. And nextTile in initialised as null and is only assigned a Tile if 
    // there exists a Tile with more than 0 possibles. It works here because the tileset is complete (every
    // permutation exists), this wont always be the case, so needs a more robust handling of impossible combinations
    // of tiles.
    public Tile[,] CreateMap(Tile[,] grid, List<Tile> allTiles)
    {
        _gridWidth = Tileset.Instance.gridWidth;
        _gridHeight = Tileset.Instance.gridHeight;
        Tile lastTileCollapsed = null;
        int uncollapsed = 0;
        int safetyNet = 0;
        foreach (var tile in allTiles)
        {
            if (!tile.Collapsed) uncollapsed++;
        }

        while (uncollapsed > 0)
        {
            safetyNet++;
            if (lastTileCollapsed == null)
            {
                UpdateSignaturesAndIndexes(grid);
            }
            else
            {
                UpdateSignaturesAndIndexes(grid, lastTileCollapsed);
            }
            // UpdateSignaturesAndIndexes(grid);
            allTiles.Sort();
            Tile nextTile = null;
            foreach (var t in allTiles)
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
                lastTileCollapsed = new Tile(nextTile.Row, nextTile.Col);
                Debug.Log($"{nextTile.Row} : {nextTile.Col}");
            }
            else
            {
                UpdateTilePlacement(nextTile, TilesetWithRotations[nextTile.PossiblesIndexes[0]]);
                lastTileCollapsed = new Tile(nextTile.Row, nextTile.Col);
                Debug.Log($"{nextTile.Row} : {nextTile.Col}");
            }
            int countUncollapsed = 0;
            foreach (var tile in allTiles)
            {
                if (!tile.Collapsed) countUncollapsed++;
            }

            uncollapsed = countUncollapsed;
            if (safetyNet > _gridWidth * _gridHeight + _gridHeight)
            {
                Debug.Log("Safety net needed - possible infinite loop");
                break;
            }
        }

        for (int i = 0; i < _gridHeight; i++)
        {
            string rowString = "";
            for (int j = 0; j < _gridWidth; j++)
            {
                rowString += grid[i, j].PrintTrueConnections() + " , ";
            }
            Debug.Log(rowString);
        }

        return grid;
    }
    
    // Flags a Tile instance in 2d and 1d Lists as collapsed (prefab assigned)
    // updates fields on Tile with information about prefab.
    public void UpdateTilePlacement(Tile tile, PrefabAdapter adapter)
    {
        // 'adapter' is a useless name that needs refactoring when adapter is worked on.
        tile.Adapter = adapter;
        tile.Collapsed = true;
        // 'trueConnections' are just the final connections that prefab has.
        tile.TrueConnections = adapter.Connections;
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
    List<int> CalculatePossibilitySignature(Tile tile, Tile[,] grid)
    {
        List<int> possiblesSignature = new List<int>(){0,0,0,0};
        if (tile.Row > 0)
        {
            var tileUp = grid[tile.Row - 1, tile.Col];
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

        if (tile.Row == _gridHeight - 1)
        {
            possiblesSignature[2] = 0;
        }
        else
        {
            var tileDown = grid[tile.Row + 1, tile.Col];
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
            var tileLeft = grid[tile.Row, tile.Col - 1];
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

        if (tile.Col == _gridWidth - 1)
        {
            possiblesSignature[1] = 0;
        }
        else
        {
            var tileRight = grid[tile.Row, tile.Col + 1];
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

    void UpdateSingleTileSignatureAndIndex(Tile[,] grid, Tile tile)
    {
        tile.ConnectionsNeeded = CalculatePossibilitySignature(tile, grid);
        List<int> possibleIndexes = CalculatePossibilityIndexes(tile);
        tile.PossiblesIndexes = possibleIndexes;
        tile.Possibles = possibleIndexes.Count;
    }

    // Loops through grid and updates what that tile needs as connections 
    // and stores the list of indexes from 'tilesetWithRotations' that satisfy those needs.
    void UpdateSignaturesAndIndexes(Tile[,] grid)
    {
        for (int i = 0; i < _gridHeight; i++)
        {
            for (int j = 0; j < _gridWidth; j++)
            {
                Tile currentTile = grid[i, j];
                // Only update if tile in not collapsed
                if (!currentTile.Collapsed)
                {
                    UpdateSingleTileSignatureAndIndex(grid, currentTile);
                }
            }
        }
    }

    // Overloaded method that takes a Tile as second parameter and only
    // re-evaluates its neighbours to make algorithm more efficient.
    void UpdateSignaturesAndIndexes(Tile[,] grid, Tile tile)
    {
        if (!tile.Collapsed)
        {
            UpdateSingleTileSignatureAndIndex(grid, tile);
        }

        if (tile.Row > 0)
        {
            Tile tileAbove = grid[tile.Row - 1, tile.Col];
            if (!tileAbove.Collapsed) UpdateSingleTileSignatureAndIndex(grid, tileAbove);
        }

        if (tile.Row < _gridHeight - 1)
        {
            Tile tileBelow = grid[tile.Row + 1, tile.Col];
            if (!tileBelow.Collapsed) UpdateSingleTileSignatureAndIndex(grid, tileBelow);
        }

        if (tile.Col > 0)
        {
            Tile tileLeft = grid[tile.Row, tile.Col - 1];
            if (!tileLeft.Collapsed) UpdateSingleTileSignatureAndIndex(grid, tileLeft);
        }

        if (tile.Col < _gridWidth - 1)
        {
            Tile tileRight = grid[tile.Row, tile.Col + 1];
            if (!tileRight.Collapsed) UpdateSingleTileSignatureAndIndex(grid, tileRight);
        }
    }
}

// --- Glossary ---
//
// uncollapsed : a Tile with collapsed == false. This means no prefab has been assigned to that spot yet.
// possiblesIndexes : indexes of positions in 'tilesetWithRotations' that could legally fill a grid location.
// possiblesSignature : A four digit list of 1's or 0's that show what connections are needed on that edge
//                      -1 is used as 'any', so {-1,-1,-1,-1} is the signature that any prefab can fit in that spot
