using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
// using UnityEngine.UI;
using UnityEngine.UIElements;
using Oasis.Core;
using Random = UnityEngine.Random;
using Texture = UnityEngine.Texture;
using Debug = UnityEngine.Debug;

public class DashboardBlocksController : MonoBehaviour
{
    private BlockStateSystem _blockStateSystem;
    
    private VisualElement root;
    private VisualElement blocks;

    public VisualTreeAsset slotAsset;

    private static DashboardBlocksController _instance;
    public static DashboardBlocksController Instance => _instance;
    
    private bool initialized;
    
    VisualElement highlightedElement;
    int highlightedId;
    
    private EntityManager em;
    Google.Protobuf.Collections.RepeatedField<Oasis.Grpc.Block> defaults;
    Google.Protobuf.Collections.RepeatedField<Oasis.Grpc.Block> currentBlocks;


    private void Awake()
    {
        _instance = this;
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        currentBlocks = new Google.Protobuf.Collections.RepeatedField<Oasis.Grpc.Block>();
    }
    
    IEnumerator Start()
    {
        _blockStateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BlockStateSystem>(); 
        root = gameObject.GetComponent<UIDocument>().rootVisualElement;
        blocks = root.Q<VisualElement>("blocks", "panel").Q<VisualElement>("contents");
        yield return LoadDefaults();
        yield return UpdateGrid(defaults);
        
        // Temp set first toolbar item
        highlightedId = 1;
        Toolbar.Instance.SetToolbarItem(highlightedId, currentBlocks[0].DomainName, blocks[0].Q<Image>().image);
        
        var search = root.Q<VisualElement>("search-blocks");
        search.RegisterCallback<ChangeEvent<string>>(OnSearch);
    }

    void Update()
    {
        HandleInputs();
    }

    void HandleInputs()
    {
        // Add block to toolbar
        for ( int i = 0; i < 10; ++i )
        {
            if (highlightedElement != null && Input.GetKeyDown( "" + i ) )
            {
                Texture image = highlightedElement.Q<Image>().image;
                var currentBlock = currentBlocks[highlightedId];
                Toolbar.Instance.SetToolbarItem(i, currentBlock.DomainName, image);
            }
        }

        // Inspect block
        if (highlightedElement != null && (Input.GetKeyDown("i") || Input.GetMouseButtonDown(1)))
        {
            Hide();
            // DashboardBlocksInspect.Instance.Show("grass_block");
        }
    }
    
    public void Hide() {
        blocks.style.display = DisplayStyle.None;
    }
    
    public void Show()
    {
    }
    
    IEnumerator UpdateGrid(Google.Protobuf.Collections.RepeatedField<Oasis.Grpc.Block> ids)
    {
        currentBlocks = ids;
        blocks.Clear();
                
        for (int i = 0; i < ids.Count; i++)
        {
            var gBlockState = new Oasis.Grpc.BlockState()
            {
                Block = ids[i].DomainName
            };
            var task = _blockStateSystem.Create(gBlockState, false);
            yield return new WaitUntil(() => task.IsCompleted); // TODO Timeout
            Entity entity = task.Result;
            EntityHelpers.SetLayers(entity, "Render3DQueue");
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            
            em.AddComponentData<Rotation>(entity, new Rotation { Value = Quaternion.Euler(20.0f, 135.0f, -20.0f) });
            em.SetComponentData<Translation>(entity, new Translation { Value = new float3(0.0f, 0.0f, 1.0f) });
            yield return new WaitForEndOfFrame();
            
            VisualElement slot = slotAsset.CloneTree();
            var image = new Image();
            slot.Q<VisualElement>("image").Add(image);
            TextInfo textInfo = new CultureInfo("en-US",false).TextInfo;
            string name = ids[i].DomainName.Name.Replace("_", " ");
            slot.Q<Label>("title").text = textInfo.ToTitleCase(name);

            blocks.Add(slot);
            var button = slot.Q<Button>("slot");

            button.RegisterCallback<MouseEnterEvent, int>(OnMouseEnter, i);
            button.RegisterCallback<MouseLeaveEvent, int>(OnMouseLeave, i);
            button.RegisterCallback<ClickEvent, int>(OnClick, i);
        
            yield return Render3D.Instance.Snapshot(entity, (texture) => { image.image = texture; });
            EntityHelpers.DestroyWithChildren(entity);
        }
    }

    void OnMouseEnter(MouseEnterEvent evt, int id) {
        highlightedElement = evt.target as Button;
        highlightedId = id;
    }
    void OnMouseLeave(MouseLeaveEvent evt, int id) {
        highlightedElement = null;
        highlightedId = -1;
    }
    void OnClick(ClickEvent evt, int id) {
    }
    void OnSearch(ChangeEvent<string> evt)
    {
        if (evt.newValue == "")
            StartCoroutine(UpdateGrid(defaults));
        else
        {
            Oasis.Grpc.DomainName request = new Oasis.Grpc.DomainName {Domain = "*", Name = evt.newValue};
            var ids = Client.Instance.client.SearchBlocks(request, Client.Instance.Metadata).Value;
            StartCoroutine(UpdateGrid(ids));
        }
    }

    public void Reset()
    {
        StartCoroutine(UpdateGrid(defaults));
    }
    
    IEnumerator LoadDefaults()
    {
        if (defaults != null)
            yield return null;
            
        Oasis.Grpc.DomainName request = new Oasis.Grpc.DomainName {Domain = "*", Name = "_ore"};
        defaults = Client.Instance.client.SearchBlocks(request, Client.Instance.Metadata).Value;
    }
            
}
