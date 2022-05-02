// using Serilog;
// using Serilog.Sinks.Seq;
// using Serilog.Sinks.Unity3D;
using UnityEngine;

public class Logger : MonoBehaviour
{
    
    void OnEnable()
    {
        Application.logMessageReceived += Log.HandleUnityLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log.HandleUnityLog;
    }
    
    

    void Start()
    {
        // Default log
        Debug.Log("Default logger test");
        
        // Structured log
        var myObject = new { MyInt = 666, MyString = "number of the beast" };
        var myFloat = 12.34;
        Log.Logger.Information("Structured log with {@MyObject} object and {MyFloat} float.", myObject, myFloat);
    }
}