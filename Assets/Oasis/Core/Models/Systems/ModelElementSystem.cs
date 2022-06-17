using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Oasis.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace Oasis.Core
{
    [UpdateInGroup(typeof(ModelsGroup))]
    public partial class ModelElementSystem : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;
        private TextureSystem _textureSystem;
        EntityManager _em;
        public NativeParallelHashMap<FixedString64Bytes, Entity> Data;

        public GameObject prefab;
        private static float3 offset = new float3(-0.5f, -0.5f, -0.5f); // Enables center pivot
        private static float3 pivot = new float3(0.5f, 0.5f, 0.5f); // center pivot

        protected override void OnCreate()
        {
            _textureSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<TextureSystem>();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            // Create modelInstance elements 
            Entities
                .WithAll<Translation>()
                .WithNone<Child>()
                .ForEach((Entity e, in ModelInstance modelInstance) =>
                {
                    Grpc.Model record = _em.GetSharedComponentData<ModelRecord>(modelInstance.blockState).Value;

                    var elements = new NativeList<Child>(Allocator.Temp);
                    foreach (Grpc.Model.Types.Element gElement in record.Elements)
                    {
                        var element = CreateElement(e, record, gElement, modelInstance.lit);
                        elements.Add(new Child {Value = element});
                    }

                    var children = EntityManager.AddBuffer<Child>(e);
                    children.AddRange(elements);
                })
                .WithoutBurst().WithStructuralChanges().Run();
        }

        private Entity CreateElement(Entity parent, Grpc.Model record, Grpc.Model.Types.Element gElement, bool lit)
        {
            var prefabModelElement = GetSingleton<PrefabModelElement>().Value;
            var elementEntity = EntityManager.Instantiate(prefabModelElement);
            EntityManager.AddComponentData(elementEntity, new Parent {Value = parent});
            EntityManager.AddComponentData(elementEntity,
                new LocalToParent() {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
            EntityManager.AddComponentData(elementEntity,
                new LocalToWorld() {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
            
            // Element data
            var element = new ModelElement()
            {
                from = new float3(gElement.From[0], gElement.From[1], gElement.From[2]),
                to = new float3(gElement.To[0], gElement.To[1], gElement.To[2]),
                rotation = new int3(),
                lit = lit
            };

            // Element rotation
            if (gElement.Rotation != null && gElement.Rotation.Axis == "y")
                EntityManager.AddComponentData(elementEntity,
                    new Rotation() {Value = quaternion.Euler(0, gElement.Rotation.Angle * Mathf.Deg2Rad, 0)});
            else if (gElement.Rotation != null && gElement.Rotation.Axis == "z")
                EntityManager.AddComponentData(elementEntity,
                    new Rotation() {Value = quaternion.Euler(0, 0, gElement.Rotation.Angle * Mathf.Deg2Rad)});

            // Faces
            if (gElement.Faces.ContainsKey("north"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["north"].Texture);
                element.north = CreateFace(elementEntity, lit, texture, Side.North);
                var rotation = new quaternion();
                var translation = new float3(0, 0, 0);
                RotateAround(ref translation, ref rotation, pivot, Vector3.up, 180f); // update rotation and translation
                var position = (new float3(0 - (16 - element.to[0]), element.from[1], element.to[2]) / 16f);
                var offset = new float3(-0.5f, -0.5f, -1.5f);
                EntityManager.SetComponentData(element.north, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.north, new Rotation {Value = rotation});
                EntityManager.SetComponentData(element.north,
                    new NonUniformScale {Value = (new float3(element.Width(), element.Height(), 16f)) / 16f});
                var uvs = gElement.Faces["north"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[0]);
                    uvs.Add((int) element.from[1]);
                    uvs.Add((int) element.to[0]);
                    uvs.Add((int) element.to[1]);
                }

                EntityManager.SetComponentData(element.north,
                    new ShaderFace {Value = new float4(uvs[2] - uvs[0], uvs[3] - uvs[1], uvs[0], uvs[1])});
                EntityManager.SetComponentData(element.north, new ShaderArrayIndex {Value = texture.index});
            }

            if (gElement.Faces.ContainsKey("east"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["east"].Texture);
                element.east = CreateFace(elementEntity, lit, texture, Side.East);
                var rotation = new quaternion();
                var translation = new float3(0, 0, 0);
                RotateAround(ref translation, ref rotation, pivot, Vector3.up, 270f);
                var position = (new float3(element.to[0], element.from[1], element.from[2]) / 16f);
                var offset = new float3(-1.5f, -0.5f, -0.5f);
                EntityManager.SetComponentData(element.east, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.east, new Rotation {Value = rotation});
                EntityManager.AddComponentData(element.east,
                    new NonUniformScale {Value = (new float3(element.Depth(), element.Height(), 16f)) / 16f});
                var uvs = gElement.Faces["east"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[2]);
                    uvs.Add((int) element.from[1]);
                    uvs.Add((int) element.to[2]);
                    uvs.Add((int) element.to[1]);
                }

                EntityManager.SetComponentData(element.east,
                    new ShaderFace {Value = new float4(uvs[2] - uvs[0], uvs[3] - uvs[1], uvs[0], uvs[1])});
                EntityManager.SetComponentData(element.east, new ShaderArrayIndex {Value = texture.index});
            }

            if (gElement.Faces.ContainsKey("south"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["south"].Texture);
                element.south = CreateFace(elementEntity, lit, texture, Side.South);
                var rotation = new quaternion(); // empty
                var translation = new float3(0, 0, 0); // empty
                RotateAround(ref translation, ref rotation, pivot, Vector3.up, 0f); // Face points South by default
                var position = (new float3((element.from[0]), element.from[1], element.from[2]) / 16f); // adjust for axis change
                EntityManager.SetComponentData(element.south, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.south, new Rotation {Value = rotation});
                EntityManager.SetComponentData(element.south,
                    new NonUniformScale {Value = new float3(element.Width(), element.Height(), element.Depth()) / 16f});
                var uvs = gElement.Faces["south"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[1]);
                    uvs.Add((int) element.from[2]);
                    uvs.Add((int) element.to[1]);
                    uvs.Add((int) element.to[2]);
                }

                EntityManager.SetComponentData(element.south,
                    new ShaderFace {Value = new float4(uvs[2] - uvs[0], uvs[3] - uvs[1], uvs[0], uvs[1])});
                EntityManager.SetComponentData(element.south, new ShaderArrayIndex {Value = texture.index});
            }

            if (gElement.Faces.ContainsKey("west"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["west"].Texture);
                element.west = CreateFace(elementEntity, lit, texture, Side.West);
                var translation = new float3(0, 0, 0);
                var rotation = new quaternion();
                RotateAround(ref translation, ref rotation, pivot, Vector3.up, 90f);
                var position = (new float3(element.from[0], element.from[1], element.from[2] + element.Depth())) / 16f;
                var offset = new float3(-0.5f, -0.5f, -1.5f);
                EntityManager.SetComponentData(element.west, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.west, new Rotation {Value = rotation});
                EntityManager.AddComponentData(element.west,
                    new NonUniformScale {Value = new float3(element.Depth(), element.Height(), element.Width()) / 16f});
                var uvs = gElement.Faces["west"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[2]);
                    uvs.Add((int) element.from[1]);
                    uvs.Add((int) element.to[2]);
                    uvs.Add((int) element.to[1]);
                }

                EntityManager.SetComponentData(element.west,
                    new ShaderFace {Value = new float4(uvs[2] - uvs[0], uvs[3] - uvs[1], uvs[0], uvs[1])});
                EntityManager.SetComponentData(element.west, new ShaderArrayIndex {Value = texture.index});
            }

            if (gElement.Faces.ContainsKey("up"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["up"].Texture);
                element.up = CreateFace(elementEntity, lit, texture, Side.Up);
                var translation = new float3(0, 0, 0);
                var rotation = new quaternion();
                RotateAround(ref translation, ref rotation, pivot, Vector3.right, 90f);
                var position = (new float3(element.from[0], -16f + element.to[1], element.from[2])) / 16f;
                var offset = new float3(-0.5f, -0.5f, -0.5f);
                EntityManager.SetComponentData(element.up, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.up, new Rotation {Value = rotation});
                EntityManager.AddComponentData(element.up,
                    new NonUniformScale {Value = new float3(element.Width(), element.Depth(), element.Width()) / 16f});
                var uvs = gElement.Faces["up"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[0]);
                    uvs.Add((int) element.from[2]);
                    uvs.Add((int) element.to[0]);
                    uvs.Add((int) element.to[2]);
                }
                EntityManager.SetComponentData(element.up,
                    new ShaderFace {Value = new float4(uvs[2] - uvs[0], uvs[3] - uvs[1], uvs[0], 16 - uvs[3])}); // flip y direction
                EntityManager.SetComponentData(element.up, new ShaderArrayIndex {Value = texture.index});
            }

            if (gElement.Faces.ContainsKey("down"))
            {
                var texture = _textureSystem.ComputeTexture(record, gElement.Faces["down"].Texture);
                element.down = CreateFace(elementEntity, lit, texture, Side.Down);
                var translation = new float3(0, 0, 0);
                var rotation = new quaternion();
                RotateAround(ref translation, ref rotation, pivot, Vector3.right, 270f);
                var position = (new float3(element.from[0], element.from[1], -16 + element.Depth() + element.@from[2])) / 16f;
                var offset = new float3(-0.5f, -0.5f, -0.5f);
                EntityManager.SetComponentData(element.down, new Translation {Value = translation + offset + position});
                EntityManager.SetComponentData(element.down, new Rotation {Value = rotation});
                EntityManager.AddComponentData(element.down,
                    new NonUniformScale {Value = new float3(element.Width(), element.Depth(), element.Width()) / 16f});
                var uvs = gElement.Faces["down"].Uv;
                if (uvs.Count == 0)
                {
                    uvs.Add((int) element.from[0]);
                    uvs.Add((int) element.from[2]);
                    uvs.Add((int) element.to[0]);
                    uvs.Add((int) element.to[2]);
                }
                EntityManager.SetComponentData(element.down,
                    new ShaderFace {Value = new float4(uvs[0] - uvs[2], uvs[1] - uvs[3], uvs[0], uvs[1])});
                EntityManager.SetComponentData(element.down, new ShaderArrayIndex {Value = texture.index});
            }

            // EntityManager.AddComponentData(elementEntity, element);
            return elementEntity;
        }

        private Entity CreateFace(Entity parent, bool lit, Texture texture, Side side)
        {
            var prefab = ComputeFacePrefab(lit, texture);
            var e = EntityManager.Instantiate(prefab);
            EntityManager.AddComponentData(e, new ModelElementFace() {side = side});
            EntityManager.AddComponentData(e, new ShaderIsModel() {Value = 1});
            EntityManager.AddComponentData(e, new ShaderFace() { });
            EntityManager.AddComponentData(e, new ShaderArrayIndex { });
            EntityManager.AddComponentData(e, new NonUniformScale() { });
            EntityManager.SetName(e, side.ToString());
            SetParent(parent, e);
            return e;
        }

        private void SetParent(Entity parent, Entity child)
        {
            EntityManager.AddComponentData(child, new Parent {Value = parent});
            EntityManager.AddComponentData(child,
                new LocalToParent() {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
        }

        private Entity ComputeFacePrefab(bool lit, Texture texture)
        {
            // Entity opaqueLit = GetSingleton<PrefabFaceOpaqueLit>().Value;
            var prefab = texture.type switch
            {
                TextureType.Opaque when lit => GetSingleton<PrefabFaceOpaqueLit>().Value,
                TextureType.Opaque when !lit => GetSingleton<PrefabFaceOpaqueUnlit>().Value,
                TextureType.AlphaClip when lit => GetSingleton<PrefabFaceAlphaClipLit>().Value,
                TextureType.AlphaClip when !lit => GetSingleton<PrefabFaceAlphaClipUnlit>().Value,
                TextureType.Transparent when lit => GetSingleton<PrefabFaceTransparentLit>().Value,
                TextureType.Transparent when !lit => GetSingleton<PrefabFaceTransparentUnlit>().Value,
                _ => Entity.Null
            };
            return prefab;

            // var face = ecb.Instantiate(prefab);
            // ecb.AddComponent<FaceTag>(face); // Collider requires this
            // ecb.AddComponent(face, new Parent {Value = element});
            // ecb.AddComponent(face,
            //     new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
            // ecb.AddComponent(face, new ShaderArrayIndex {Value = texture.index});
            // ecb.AddComponent(face, new ShaderIsModel() {Value = 1f});
            // return face;
        }

        // TODO Optimize trip through UnityEngine.Transform#RotateAround
        static void RotateAround(ref float3 translation, ref quaternion rotation, float3 pivot, Vector3 axis, float angle)
        {
            var gameObject = new GameObject(); // TODO optimize
            Transform transform = gameObject.transform;
            transform.position = translation;
            transform.rotation = rotation;
            transform.RotateAround(pivot, axis, angle);
            translation = transform.position;
            rotation = transform.rotation;
            GameObject.Destroy(gameObject);
        }
    }
}