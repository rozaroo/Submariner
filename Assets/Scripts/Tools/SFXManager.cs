using System;
using UnityEngine;

public static class SFXManager //Wrapper For Wwise
{
    public static void LoadBank(string sbName)
    {
        AkSoundEngine.LoadBank(sbName, out _);
    }
    
    public static void UnloadBank(string sbName)
    {
        AkSoundEngine.UnloadBank(sbName, IntPtr.Zero);
    }
    
    public static void PostEvent(string eventName, GameObject gameObject)
    {
        AkSoundEngine.PostEvent(eventName, gameObject);
    }

    public static void SetRtpcValue(string rtpcName, float value, GameObject gameObject)
    {
        AkSoundEngine.SetRTPCValue(rtpcName, value, gameObject);
    }

    public static void SetRtpcValue(string rtpcName, float value)
    {
        AkSoundEngine.SetRTPCValue(rtpcName, value);
    }

    public static void SetState(string stateCollection, string stateName)
    {
        AkSoundEngine.SetState(stateName, stateCollection);
    }

    public static void SetSwitch(string switchName, string switchValue, GameObject gameObject)
    {
        AkSoundEngine.SetSwitch(switchName, switchValue, gameObject);
    }
}
