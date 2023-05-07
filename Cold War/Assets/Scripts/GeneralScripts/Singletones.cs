using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singletones<T> :MonoBehaviour where T : MonoBehaviour
{
    public static T Singletone;
    protected virtual void Awake()
    {
        if (Singletone == null)
            Singletone = this as T;
        else
            Destroy(gameObject);
    }
}
