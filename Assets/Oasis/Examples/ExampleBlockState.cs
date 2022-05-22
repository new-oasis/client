using System;
using Oasis.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using BlockState = Oasis.Grpc.BlockState;
using DomainName = Oasis.Grpc.DomainName;

[Serializable]
public struct State {
    public string key;
    public string value;
}

public class ExampleBlockState : MonoBehaviour
{
    private static ExampleBlockState _instance;
    public static ExampleBlockState Instance { get { return _instance; } }
        
    public string version;
    public string domain;
    public new string name;
    public bool lit;
    
    public State[] states;

    async void Start()
    {
        var blockStateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BlockStateSystem>();
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var gDomainName = new DomainName() {Version = version, Domain = domain, Name = name};
        var gBlockState = new BlockState()
        {
            Block = gDomainName,
        };
        foreach (var state in states)
            gBlockState.State[state.key] = state.value;

        blockStateSystem.Create(gBlockState, false);
    }

}
