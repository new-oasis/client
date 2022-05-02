using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oasis.UI
{
    
    public class UI : MonoBehaviour
    {
        private static UI _instance;
        public static UI Instance => _instance;
        
        public GameObject menuGo;
        public GameObject dashboardGo;
        public GameObject hudGo;
        public GameObject debugGo;
        private VisualElement _menu;
        private VisualElement _dashboard;
        private VisualElement _debug;
        private VisualElement _hud;


        void Awake()
        {
            _instance = this;
            _menu = menuGo.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("menu");
            _dashboard = dashboardGo.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("dashboard");
            _debug = debugGo.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("debug");
            _hud = hudGo.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("hud");
        }
    
        void Start()
        {
            _menu.style.display = DisplayStyle.Flex;
            _hud.style.display = DisplayStyle.Flex;
            _dashboard.style.display = DisplayStyle.None;
            _debug.style.display = DisplayStyle.None;
        }

        void Update()
        {
        }

        public void ToggleDebug()
        {
            _debug.style.display = _debug.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ToggleDashboard()
        {
            _hud.style.display = _hud.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
            _dashboard.style.display = _dashboard.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ToggleMenu()
        {
            _menu.style.display = _menu.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        
    }
}