using UnityEngine;

// Everything the board needs at runtime. Lives in Resources so builds include these references.
public class BoardArt : ScriptableObject
{
    public const string ResourcePath = "BoardArt";

    public BoardData data;
    public Material boardMaterial;
    public Material particleAlpha;
    public Material particleAdditive;
    public GameObject[] factories;
    public GameObject[] blueCity;
    public GameObject[] redCity;
    public GameObject missile;
    public GameObject drone;
}
