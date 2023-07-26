using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using System.Linq;


public class Tile : IComparable<Tile>
{
    private static int nextId = 0;
    public int ID;
    
    // row and col are the grid locations, stored internally so can sort instances in a 1d list
    // and still know what neighbours it has in the 2d grid.
    public readonly int Row;
    public readonly int Col;
    // How many tiles from 'tilesetWithRotations' can fit in this spot. Just possibleIndexes.Count
    // Used to sort the 1d list 
    public int Possibles;
    // List of indexes in 'tilesetWithRotations' of the actual tiles that could fit.
    public List<int> PossiblesIndexes;
    public bool Collapsed;
    // The two arrays below will have 4 values of either -1, 0, or 1.
    public List<int> ConnectionsNeeded = new List<int>();
    public List<int> TrueConnections = new List<int>();
    // Once a tile is placed, a ref to the PrefabPrefabPlaceholder is maintained
    // Which currently stores a string of the prefab needed and how it should be rotated.
    public PrefabAdapter Adapter;

    public Tile(int r, int c) // parameters int row, int column
    {
        Row = r;
        Col = c;
        Collapsed = false;
        Possibles = Int32.MaxValue; // This shouldn't matter, just initialises it greater than length of tileset.
        PossiblesIndexes = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            ConnectionsNeeded.Add(-1);
        }

        ID = nextId;
        nextId++;
    }
    
    // Debugging string
    public string PrintConnectionsNeeded()
    {
        string connectionsString = "";
        foreach (var i in ConnectionsNeeded)
        {
            connectionsString += i.ToString();
        }
        return connectionsString;
    }

    // Debugging string
    public string PrintTrueConnections()
    {
        string connections = "";
        foreach (var connection in TrueConnections)
        {
            connections += connection.ToString();
        }

        return connections;
    }

    // IComparable method that sorts by which Tile has fewest possibles.
    public int CompareTo(Tile other)
    {
        return this.Possibles.CompareTo(other.Possibles);
    }
}

// Below is the start of an adapter class
// that will bridge the gap between Unity prefabs and the 2d grid of tiles.
public class PrefabAdapter
{
    public readonly List<int> Connections;
    public readonly int Rotation;
    public readonly GameObject PrefabType;

    public PrefabAdapter(List<int> connections, int rotation, GameObject prefabType)
    {
        Connections = connections;
        Rotation = rotation;
        PrefabType = prefabType;
    }

    
    // Debugging method for printing out connections
    public string PrintConnections()
    {
        string connectionsString = "";
        foreach (var i in Connections)
        {
            connectionsString += i.ToString();
        }

        connectionsString += " Rotation: " + Rotation;
        return connectionsString;
    }
}
