using System;
using UnityEngine.Events;

[Serializable]
public struct DepthStats
{
    public long count;
    public float mean;
    public float stdPop;
    public float stdSample;
}

[Serializable]
public class StatsEvent : UnityEvent<DepthStats> { }

