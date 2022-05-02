using System.Collections;
using System.Globalization;
using Google.Protobuf.Collections;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using BovineLabs.Event.Systems;
using Unity.Mathematics;
using DomainName = Oasis.Grpc.DomainName;
using Place = Oasis.Grpc.Place;

public class DashboardPlacesController : MonoBehaviour
{
    private PlaceSystem placeSystem;
    
    private VisualElement root;
    private VisualElement places;

    public VisualTreeAsset slotAsset;

    private static DashboardPlacesController _instance;
    public static DashboardPlacesController Instance => _instance;
    
    private bool initialized;
    
    VisualElement highlightedElement;
    int highlightedId;
    
    private EntityManager em;
    RepeatedField<Place> defaults;
    RepeatedField<Place> currentPlaces;


    private void Awake()
    {
        _instance = this;
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        currentPlaces = new RepeatedField<Place>();
    }
    
    IEnumerator Start()
    {
        placeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PlaceSystem>();
        root = gameObject.GetComponent<UIDocument>().rootVisualElement;
        places = root.Q<VisualElement>("places", "panel").Q<VisualElement>("contents");
        yield return LoadDefaults();
        UpdateGrid(defaults);
        
        var search = root.Q<VisualElement>("search-places");
        search.RegisterCallback<ChangeEvent<string>>(OnSearch);
    }

    void Update()
    {
    }

    
    public void Hide() {
        places.style.display = DisplayStyle.None;
    }
    
    public void Show()
    {
    }
    
    void UpdateGrid(RepeatedField<Place> ids)
    {
        currentPlaces = ids;
        places.Clear();
                
        for (int i = 0; i < ids.Count; i++)
        {
            var gPlace = new Place()
            {
                Realm = ids[i].Realm
            };
            
            VisualElement slot = slotAsset.CloneTree();
            var image = new Image();
            slot.Q<VisualElement>("image").Add(image);
            TextInfo textInfo = new CultureInfo("en-US",false).TextInfo;
            string name = ids[i].Realm.Name;
            slot.Q<Label>("title").text = textInfo.ToTitleCase(name);

            places.Add(slot);
            var button = slot.Q<Button>("slot");

            button.RegisterCallback<MouseEnterEvent, int>(OnMouseEnter, i);
            button.RegisterCallback<MouseLeaveEvent, int>(OnMouseLeave, i);
            button.RegisterCallback<ClickEvent, int>(OnClick, i);
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
    void OnClick(ClickEvent evt, int id)
    {
        // Set PlayerRealm
        var playerRealmSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PlayerRealmSystem>();
        var realm = new Oasis.Core.DomainName()
        {
            domain = currentPlaces[id].Realm.Domain,
            name = currentPlaces[id].Realm.Name
        };
        playerRealmSystem.SetRealm(realm);
        
        // Move Player
        var playerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PlayerSystem>();
        playerSystem.Move(new int3().FromInt3(currentPlaces[id].Xyz));
    }
    
    void OnSearch(ChangeEvent<string> evt)
    {
        if (evt.newValue == "")
            UpdateGrid(defaults);
        else
        {
            var realm = new DomainName {Domain = "minecraft", Name = "*"};
            var request = new PlaceRequest()
            {
                Realm = realm,
                Name = evt.newValue
            };
            var ids = Client.Instance.client.SearchPlaces(request, Client.Instance.Metadata).Value;
            UpdateGrid(ids);
        }
    }

    public void Reset()
    {
        UpdateGrid(defaults);
    }
    
    IEnumerator LoadDefaults()
    {
        if (defaults != null)
            yield return null;

        var domainName = new DomainName() { };
        var placeRequest = new PlaceRequest()
        {
            Realm = domainName,
            Name = ""
        };
        defaults = Client.Instance.client.SearchPlaces(placeRequest, Client.Instance.Metadata).Value;
    }
            
}