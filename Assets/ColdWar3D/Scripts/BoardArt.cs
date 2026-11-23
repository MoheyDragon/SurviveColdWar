using UnityEngine;

// Everything the board needs at runtime. Lives in Resources so builds include these references.
public class BoardArt : ScriptableObject
{
    public const string ResourcePath = "BoardArt";

    public BoardData data;
    public Material boardMaterial;
    public Material particleAlpha;
    public Material particleAdditive;
    public Material iconMaterial;
    public GameObject[] factories;
    public GameObject[] blueCity;
    public GameObject[] redCity;
    public GameObject missile;
    public GameObject drone;
    // Unit models from asset packs, one per hidden level (index 0 = lowest). When a list is empty the
    // built-in shape is used instead.
    public GameObject[] tankModels;
    public GameObject[] shipModels;
    public GameObject[] satelliteModels;
    // The spy drone's texture with its shell recolored blue or red (the rest keeps the pack's colors).
    public Texture2D droneBlue, droneRed;
    // The rocket that stands on the launch pad during research.
    public GameObject researchRocket;
}
