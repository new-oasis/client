using Google.Protobuf.Collections;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BlockState = Oasis.Core.BlockState;
using BlockType = Oasis.Core.BlockType;
using DomainName = Oasis.Grpc.DomainName;
using TextureType = Oasis.Core.TextureType;

namespace Oasis.Examples
{
    public class ExampleModel : MonoBehaviour
    {
        
        [Header("BlockState")]
        public string domain;
        public new string name;
        public TextureType textureType;
        public BlockType blockType;


        [Header("gRPC Model Element")]
        public float3 from;
        public float3 to;
        
        [Header("gRPC Model Faces")]
        public string texture;
        
        private async void Start()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var blockStateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BlockStateSystem>();
            var textureSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<TextureSystem>();
            textureSystem.Load(new DomainName { Domain = domain, Name = texture });
            
            
            // 1. Create BlockState
            var blockStateEntity = em.CreateEntity();
            em.AddComponentData(blockStateEntity, new BlockState()
            {
                domainName = new Core.DomainName(domain, name),
                textureType = textureType,
                blockType = blockType
            });
            var gBlockState = new Grpc.BlockState {Block = new Oasis.Grpc.DomainName {Domain = domain, Name = name}};
            blockStateSystem.entities[gBlockState] = blockStateEntity;

            // 2. Create ModelRecord
            var element = new Model.Types.Element();
            element.From.AddRange(new float[]{from[0], from[1], from[2]});
            element.To.AddRange(new float[]{to[0], to[1], to[2]});
            element.Faces["south"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            element.Faces["north"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            element.Faces["east"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            element.Faces["west"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            element.Faces["up"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            element.Faces["down"] = new Model.Types.Face{Texture = "#all", Uv = { 0, 0, 16, 16 }};
            var gModel = new Model(){Domain = domain};
            gModel.Textures["all"] = texture;
            gModel.Elements.Add(element);
            em.AddSharedComponentData(blockStateEntity, new ModelRecord { Value = gModel });
            
            
            // 3. Create ModelInstance
            blockStateSystem.Create(gBlockState, false);
                
            
            // BlockStateSystem#Create creates ModelInstance
            // ModelElementSystem creates Elements and Faces from ModelRecord
        }

    }
}
