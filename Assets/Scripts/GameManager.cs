using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        SFXManager.PostEvent("Start_BackgroundSubmarineMFX", gameObject);
    }

    private void CallMapGeneration() //Leave for future use, Random Map Generation.
    {
        
    }
}
