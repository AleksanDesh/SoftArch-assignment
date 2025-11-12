using System.Diagnostics;
using UnityEngine;

public class ActiveChangeLogger : MonoBehaviour
{
    void OnEnable()
    {
        // Print backtrace quickly and name, so we see who enabled it
        var st = new System.Diagnostics.StackTrace(1, true);
        UnityEngine.Debug.Log($"[ACTIVE] {gameObject.name} OnEnable() called. stack:\n{st}");
    }

    void OnDisable()
    {
        UnityEngine.Debug.Log($"[ACTIVE] {gameObject.name} OnDisable() called.");
    }
}
