using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
// using Unity.UI;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class DashboardMenuController : MonoBehaviour
{
    private VisualElement root;

    public enum Tab
    {
        Home,
        Blocks,
        Places,
        Creator
    }
    public Dictionary<Tab, VisualElement> tabs;
    public Dictionary<Tab, VisualElement> panels;
    
    private void Start()
    {
        tabs = new Dictionary<Tab, VisualElement>();
        panels = new Dictionary<Tab, VisualElement>();
        root = gameObject.GetComponent<UIDocument>().rootVisualElement;
        
        var mainMenu = root.Q<VisualElement>("menu");
        tabs[Tab.Home]     = mainMenu.Q<VisualElement>("home");
        tabs[Tab.Blocks]   = mainMenu.Q<VisualElement>("blocks");
        tabs[Tab.Places]   = mainMenu.Q<VisualElement>("places");
        tabs[Tab.Creator]  = mainMenu.Q<VisualElement>("creator");
        
        var panelsElement = root.Q<VisualElement>("panels");
        panels[Tab.Home]     = panelsElement.Q<VisualElement>("home");
        panels[Tab.Blocks]   = panelsElement.Q<VisualElement>("blocks");
        panels[Tab.Places]   = panelsElement.Q<VisualElement>("places");
        panels[Tab.Creator]  = panelsElement.Q<VisualElement>("creator");
        
        foreach (Tab menuItem in Enum.GetValues(typeof(Tab)))
            tabs[menuItem].RegisterCallback<ClickEvent, Tab>(HandleClick, menuItem);
    }
    
    void HandleClick(ClickEvent ce, Tab tab)
    {
        // Set all button inactive
        foreach(KeyValuePair<Tab, VisualElement> t in tabs)
            t.Value.RemoveFromClassList("selected");
        
        // Set selected button to active
        tabs[tab].AddToClassList("selected");
        
        // Hide all tab containers
        foreach (KeyValuePair<Tab, VisualElement> kvp in panels)
            kvp.Value.style.display = DisplayStyle.None;
        
        // Show selected tab container
        panels[tab].style.display = DisplayStyle.Flex;

        // if (tab == Tab.Blocks)
            // DashboardBlocksController.Instance.Init();
    }
    
    
}

