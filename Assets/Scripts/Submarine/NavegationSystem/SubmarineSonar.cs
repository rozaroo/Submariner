using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmarineSonar : MonoBehaviour, ISonarProvider
{
    [Header("Sonar Properties (World Space)")] 
    [SerializeField] private float outerDetectionRadius = 50f;
    [SerializeField] private float innerDetectionRadius = 25f;
    [SerializeField] private float timePerSonarCheck = 0.2f;
    
    private List<IWorldElement> _worldTargets = new List<IWorldElement>();
    private List<IWorldElement> _insideOuterRadius = new List<IWorldElement>();
    private List<IWorldElement> _insideInnerRadius = new List<IWorldElement>();
    private Coroutine _sonarCoroutine;
    private Transform _selfTransform;
    public float OuterRadius => outerDetectionRadius;
    public float InnerRadius => innerDetectionRadius;

    private void Awake()
    {
        _selfTransform = transform;
    }

    public void InitializeSonarTargets(List<IWorldElement> targets)
    {
        _worldTargets = targets;
        if (_sonarCoroutine != null) StopCoroutine(_sonarCoroutine);
        _sonarCoroutine = StartCoroutine(CheckDistances3D());
    }

    private IEnumerator CheckDistances3D()
    {
        float sqrOuterRadius = outerDetectionRadius * outerDetectionRadius;
        float sqrInnerRadius = innerDetectionRadius * innerDetectionRadius;

        while (true)
        {
            _worldTargets.RemoveAll(t => t == null);
            _insideOuterRadius.RemoveAll(t => t == null);
            _insideInnerRadius.RemoveAll(t => t == null);

            for (int i = 0; i < _worldTargets.Count; i++)
            {
                IWorldElement target = _worldTargets[i];
                SonarDetectionMode interactionMode = target.sonarDetectionMode;

                if (interactionMode == SonarDetectionMode.None) continue;

                float sqrDistance = (target.position - _selfTransform.position).sqrMagnitude;

                //Exterior
                bool isWithinOuter = sqrDistance <= sqrOuterRadius &&
                                     (interactionMode == SonarDetectionMode.Both ||
                                      interactionMode == SonarDetectionMode.OuterOnly);

                if (isWithinOuter && !_insideOuterRadius.Contains(target))
                {
                    _insideOuterRadius.Add(target);
                    GameEventChannel<OnSonarElementsDetection>.RaiseEvent(new OnSonarElementsDetection(target, SonarDetectionMode.OuterOnly, true));
                }
                else if (!isWithinOuter && _insideOuterRadius.Contains(target))
                {
                    _insideOuterRadius.Remove(target);
                    GameEventChannel<OnSonarElementsDetection>.RaiseEvent(new OnSonarElementsDetection(target, SonarDetectionMode.OuterOnly, false));
                }

                //Interior
                if (interactionMode == SonarDetectionMode.Both || interactionMode == SonarDetectionMode.InnerOnly)
                {
                    bool isWithinInner = sqrDistance <= sqrInnerRadius;

                    if (isWithinInner && !_insideInnerRadius.Contains(target))
                    {
                        _insideInnerRadius.Add(target);
                        GameEventChannel<OnSonarElementsDetection>.RaiseEvent(new OnSonarElementsDetection(target, SonarDetectionMode.InnerOnly, true));
                    }
                    else if (!isWithinInner && _insideInnerRadius.Contains(target))
                    {
                        _insideInnerRadius.Remove(target);
                        GameEventChannel<OnSonarElementsDetection>.RaiseEvent(new OnSonarElementsDetection(target, SonarDetectionMode.InnerOnly, false));
                    }
                }
            }
            yield return new WaitForSeconds(timePerSonarCheck);
        }
    }
    
    #region Testing

    [ContextMenu("Check Coroutines")]
    public void CheckCoroutines()
    {
        if (_sonarCoroutine != null)
        {
            Log.Info("[MapUIManager] Sync Coroutine is active.");
        }
        else
        {
            Log.Info("[MapUIManager] Sync Coroutine is not active.");
        }
    }
    
    #endregion
}