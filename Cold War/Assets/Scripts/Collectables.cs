using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName ="newCollectable",menuName = "Collectables")]
public class Collectables : ScriptableObject
{
    public new string name;
    public Sprite sprite;
    public float PowerAmount,Speed,StayTime;

}
