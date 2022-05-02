using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using Oasis.Core;
using Oasis.UI;
using Grpc.Core;
using Oasis.Grpc;
using Unity.Mathematics;

namespace Oasis.Game
{
    using UI = Oasis.UI.UI;
    
    public class Actions : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private EntityManager _em;
        private FirstPersonPlayerSystem _playerSystem;

        private void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _playerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FirstPersonPlayerSystem>();
        }

        private void LateUpdate()
        {
            // TODO move below to inputActions to enable other controlSchemes
            if (_playerInput.currentActionMap.name == "Game")
            {
                _playerSystem.moveInput = float2.zero;
                _playerSystem.moveInput.y += Keyboard.current[Key.W].isPressed ? 1f : 0f;
                _playerSystem.moveInput.y += Keyboard.current[Key.S].isPressed ? -1f : 0f;
                _playerSystem.moveInput.x += Keyboard.current[Key.D].isPressed ? 1f : 0f;
                _playerSystem.moveInput.x += Keyboard.current[Key.A].isPressed ? -1f : 0f;
                _playerSystem.jumpInput = Keyboard.current[Key.Space].isPressed;

                _playerSystem.lookInput = new float2(
                    Mouse.current.delta.x.ReadValue() * Time.deltaTime,
                    Mouse.current.delta.y.ReadValue() * Time.deltaTime
                );
            }
        }
        
        // Game
        void OnDashboard()
        {
            UI.Instance.ToggleDashboard();
            _playerInput.SwitchCurrentActionMap("Dashboard");
        }
    
        void OnMenu()
        {
            UI.Instance.ToggleMenu();
            _playerInput.SwitchCurrentActionMap("Menu");
        }

        void OnDebug()
        {
            UI.Instance.ToggleDebug();
        }

        private void OnPlace()
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<Highlight>());
            var highlight = query.GetSingleton<Highlight>();
            
            var voxelChange = new VoxelChange
            {
                Voxel = new Int3{X = highlight.VoxelAdjacent.x, Y = highlight.VoxelAdjacent.y, Z = highlight.VoxelAdjacent.z},
                BlockState = new Grpc.BlockState()
                {
                    Block = Toolbar.Instance.domainNames[Toolbar.Instance.selectedItem],
                }
            };
            var feedRequest = new FeedRequest() {VoxelChange = voxelChange};
            Client.Instance.feedRequest.WriteAsync(feedRequest);
        }

        void OnToolbar1()
        {
            Toolbar.Instance.SetActiveToolbarItem(0);
        }
        void OnToolbar2()
        {
            Toolbar.Instance.SetActiveToolbarItem(1);
        }
        void OnToolbar3()
        {
            Toolbar.Instance.SetActiveToolbarItem(2);
        }
        void OnToolbar4()
        {
            Toolbar.Instance.SetActiveToolbarItem(3);
        }
        void OnToolbar5()
        {
            Toolbar.Instance.SetActiveToolbarItem(4);
        }
        void OnToolbar6()
        {
            Toolbar.Instance.SetActiveToolbarItem(5);
        }
        void OnToolbar7()
        {
            Toolbar.Instance.SetActiveToolbarItem(6);
        }
        void OnToolbar8()
        {
            Toolbar.Instance.SetActiveToolbarItem(7);
        }
        void OnToolbar9()
        {
            Toolbar.Instance.SetActiveToolbarItem(8);
        }
        void OnToolbar0()
        {
            Toolbar.Instance.SetActiveToolbarItem(9);
        }
        
        // Dashboard
        void OnDashboardExit()
        {
            UI.Instance.ToggleDashboard();
            _playerInput.SwitchCurrentActionMap("Game");
        }
        
    
        // Menu
        void OnMenuExit()
        {
            UI.Instance.ToggleMenu();
            _playerInput.SwitchCurrentActionMap("Game");
        }
    


    }
}