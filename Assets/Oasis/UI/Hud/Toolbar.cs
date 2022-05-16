using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Oasis.Grpc;
using UnityEngine.Serialization;
using Texture = UnityEngine.Texture;

public class Toolbar : MonoBehaviour
{
    private static Toolbar _instance;
    public static Toolbar Instance => _instance;

    public DomainName[] domainNames;

    private VisualElement root;
    private VisualElement toolbar;
    private UQueryBuilder<VisualElement> slots;
    public int selectedItem;
    

    void Awake()
    {
        _instance = this;
    }
    
    void Start()
    {
        root = gameObject.GetComponent<UIDocument>().rootVisualElement;
        toolbar = root.Q<VisualElement>("toolbar");
        slots = toolbar.Query<VisualElement>("toolbarSlot");
        domainNames = new Oasis.Grpc.DomainName[10];
        SetActiveToolbarItem(0);
    }

    public void SetToolbarItem(int slotIndex, DomainName domainName, Texture blockImage)
    {
        slotIndex = slotIndex - 1;
        if (slotIndex == -1)
            slotIndex = 9;
        
        domainNames[slotIndex] = domainName;
        
        var slot = slots.AtIndex(slotIndex);
        var image = new Image();
        image.image = blockImage;
        if (slot.childCount > 0)
            slot.RemoveAt(0);
        slot.Add(image);
    }

    public void SetActiveToolbarItem(int slotIndex)
    {
        selectedItem = slotIndex;
        for (int i = 0; i < 10; i++)
            slots.AtIndex(i).RemoveFromClassList("toolbarSlotActive");
        slots.AtIndex(slotIndex).AddToClassList("toolbarSlotActive");
    }
}
