using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TargetInfo
{
    public GameObject target;
    public float distance;
    public Vector3 direction;
    public Vector3 lastKnownPosition;
    public float timeLastSeen;
    // Tank component information
    public TankController tankController;
    public TurretData turretData;
    public float totalHP;
    public TargetInfo(GameObject targetObj, float dist, Vector3 dir)
    {
        target = targetObj;
        distance = dist;
        direction = dir;
        lastKnownPosition = targetObj.transform.position;
        timeLastSeen = Time.time;
        AnalyzeTarget();
    }
    private void AnalyzeTarget()
    {
        if (target == null) return;
        tankController = target.GetComponent<TankController>();
        totalHP = 100f; // Default value
    }
    public void UpdateInfo(float dist, Vector3 dir)
    {
        distance = dist;
        direction = dir;
        lastKnownPosition = target.transform.position;
        timeLastSeen = Time.time;
        AnalyzeTarget();
    }
}

public class TurretController : MonoBehaviour
{
    // Basic fields to avoid compile errors in TurretAIMaster
    public TargetInfo GetCurrentTarget() { return null; }
    public void FireAtTarget(TargetInfo target = null) { }
}
