using UnityEngine;
// using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private static MenuController _instance;
    public static MenuController Instance => _instance;
    
    private bool initialized;
    
    private void Awake()
    {
        _instance = this;
    }
    
    public void Hide() {
    }
    
    public void Show()
    {
    }
    
}
