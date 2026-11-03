using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives modal station objects, such as tutorial sketchbooks, first refusal on
/// a pointer click before the owning station performs its own interaction.
/// </summary>
public static class StationClickBlocker
{
    private static readonly List<TutorialSketchbook> RegisteredSketchbooks = new();
    private static Camera _lastConsumedCamera;
    private static int _lastConsumedFrame = -1;

    public static void Register(TutorialSketchbook sketchbook)
    {
        if (sketchbook != null && !RegisteredSketchbooks.Contains(sketchbook))
        {
            RegisteredSketchbooks.Add(sketchbook);
        }
    }

    public static void Unregister(TutorialSketchbook sketchbook)
    {
        RegisteredSketchbooks.Remove(sketchbook);
    }

    public static bool TryConsumeClick(Camera stationCamera)
    {
        if (stationCamera == null)
        {
            return false;
        }

        if (_lastConsumedFrame == Time.frameCount && _lastConsumedCamera == stationCamera)
        {
            return true;
        }

        for (int i = RegisteredSketchbooks.Count - 1; i >= 0; i--)
        {
            TutorialSketchbook sketchbook = RegisteredSketchbooks[i];
            if (sketchbook == null)
            {
                RegisteredSketchbooks.RemoveAt(i);
                continue;
            }

            if (!sketchbook.TryConsumeStationClick(stationCamera))
            {
                continue;
            }

            _lastConsumedCamera = stationCamera;
            _lastConsumedFrame = Time.frameCount;
            return true;
        }

        return false;
    }
}
