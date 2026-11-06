using System;
using UnityEngine;

[Serializable]
public struct BoardTile
{
    public Vector2 position;

    // The tile is Red while GameManager.Com is above this value.
    public float redThreshold;
    public float elevation;
}

// Baked by ColdWarBaker from the game's world map sprites.
public class BoardData : ScriptableObject
{
    public float hexRadius;
    public Vector2 boardSize;
    public BoardTile[] land;
    public Vector2[] sea;
    public Vector2 blueCapital;
    public Vector2 redCapital;
    public Vector2 blueSilos;
    public Vector2 redSilos;
}
