// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Linq;
// using System.Threading.Tasks;
// using Oasis.Controllers;
// using Oasis.Grpc;
// using Unity.Entities;
// using Unity.Mathematics;
// // using Unity.UI;
// using Unity.Transforms;
// using UnityEditor;
// using UnityEditor.PackageManager;
// using UnityEngine;
// // using UnityEngine.UI;
// using UnityEngine.UIElements;
// using Oasis.Core;
// using Random = UnityEngine.Random;
// using Texture = UnityEngine.Texture;
// using Block = Oasis.Core.Block;
// using Debug = UnityEngine.Debug;
//
// public class DashboardBlocksInspect : MonoBehaviour
// {
//     private static DashboardBlocksInspect _instance;
//     public static DashboardBlocksInspect Instance => _instance;
//     
//     private VisualElement root;
//     private VisualElement blocksPanel;
//     private VisualElement blockInspect;
//
//     // private bool initialized;
//     //
//     // VisualElement highlightedElement;
//     // int highlightedId;
//     // private string highlightedBlock;
//     
//     private EntityManager em;
//
//     private void Awake()
//     {
//         _instance = this;
//         em = World.DefaultGameObjectInjectionWorld.EntityManager;
//     }
//     
//     void Start()
//     {
//         root = gameObject.GetComponent<UIDocument>().rootVisualElement;
//         var panels = root.Q<VisualElement>("panels");
//         blocksPanel = root.Q<VisualElement>("blocks", "panel").Q<VisualElement>("contents");
//         blockInspect = panels.Q<VisualElement>("inspect-block");
//     }
//     
//     void Update()
//     {
//         // if (highlightedElement != null && (Input.GetKeyDown("i") || Input.GetMouseButtonDown(1)))
//         // {
//         //    UnityEngine.Debug.Log($"Got inspect");
//         //    Inspect(highlightedBlock);
//         // }
//     }
//  
//     public void Hide() {
//         blocksPanel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
//     }
//     
//     public void Show(string blockname)
//     {
//         UnityEngine.Debug.Log($"DashboardBlocksInspect#Show {blockname}");
//         blocksPanel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
//     }
//     
// }
