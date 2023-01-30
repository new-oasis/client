using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Oasis.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(TexturesGroup))]
    public partial class TextureSystem : SystemBase
    {
        private EntityManager _em;

        public Dictionary<TextureType, Texture2DArray> _arrays;
        private Dictionary<TextureType, Entity[]> _elements;
        public Dictionary<Grpc.DomainName, Entity> _entities;

        private NativeParallelHashMap<FixedString32Bytes, int> _indexes; // texture to array index
        private NativeParallelHashMap<FixedString32Bytes, TextureType> _types; // texture to TextureType

        readonly int arraySize = 512;
        readonly int textureSize = 16;

        private Material _opaqueLit;
        private Material _opaqueUnlit;

        private Material _alphaClipLit;
        private Material _alphaClipUnlit;

        private Material _transparentLit;
        private Material _transparentUnlit;

        private Material _liquidLit;
        private Material _liquidUnlit;

        private const TextureFormat OpaqueTextureFormat = TextureFormat.ARGB32;
        private const TextureFormat AlphaClipTextureFormat = TextureFormat.ARGB32;
        private const TextureFormat TransTextureFormat = TextureFormat.ARGB32;
        private const TextureFormat LiquidTextureFormat = TextureFormat.ARGB32;

        protected override void OnCreate()
        {
            _indexes = new NativeParallelHashMap<FixedString32Bytes, int>(512, Allocator.Persistent);
            _types = new NativeParallelHashMap<FixedString32Bytes, TextureType>(512, Allocator.Persistent);
            _arrays = new Dictionary<TextureType, Texture2DArray>();
            _elements = new Dictionary<TextureType, Entity[]>();
            _entities = new Dictionary<Grpc.DomainName, Entity>();

            // Opaque
            var opaqueArray = new Texture2DArray(textureSize, textureSize, arraySize, OpaqueTextureFormat, true);
            opaqueArray.filterMode = FilterMode.Point;
            opaqueArray.anisoLevel = 9;
            _arrays[TextureType.Opaque] = opaqueArray;
            _elements[TextureType.Opaque] = new Entity[arraySize];
            _opaqueLit = Resources.Load<Material>("Faces/Materials/lit_opaque_t2a");
            _opaqueLit.SetTexture("_array", opaqueArray);
            _opaqueUnlit = Resources.Load<Material>("Faces/Materials/unlit_opaque_t2a");
            _opaqueUnlit.SetTexture("_array", opaqueArray);

            // Alpha Clip
            var alphaclipArray = new Texture2DArray(textureSize, textureSize, arraySize, AlphaClipTextureFormat, true);
            alphaclipArray.filterMode = FilterMode.Point;
            alphaclipArray.anisoLevel = 9;
            _arrays[TextureType.AlphaClip] = alphaclipArray;
            _elements[TextureType.AlphaClip] = new Entity[arraySize];
            _alphaClipLit = Resources.Load<Material>("Faces/Materials/lit_alphaclip_t2a");
            _alphaClipLit.SetTexture("_array", alphaclipArray);
            _alphaClipUnlit = Resources.Load<Material>("Faces/Materials/unlit_alphaclip_t2a");
            _alphaClipUnlit.SetTexture("_array", alphaclipArray);

            // Transparent
            var transparentArray = new Texture2DArray(textureSize, textureSize, arraySize, TransTextureFormat, true);
            transparentArray.filterMode = FilterMode.Point;
            transparentArray.anisoLevel = 9;
            _arrays[TextureType.Transparent] = transparentArray;
            _elements[TextureType.Transparent] = new Entity[arraySize];
            _transparentLit = Resources.Load<Material>("Faces/Materials/lit_transparent_t2a");
            _transparentLit.SetTexture("_array", transparentArray);
            _transparentUnlit = Resources.Load<Material>("Faces/Materials/unlit_transparent_t2a");
            _transparentUnlit.SetTexture("_array", transparentArray);

            // Liquid
            var liquidArray = new Texture2DArray(textureSize, textureSize, arraySize, LiquidTextureFormat, true);
            liquidArray.filterMode = FilterMode.Point;
            liquidArray.anisoLevel = 9;
            _arrays[TextureType.Transparent] = transparentArray;
            _elements[TextureType.Transparent] = new Entity[arraySize];
            _liquidLit = Resources.Load<Material>("Faces/Materials/lit_liquid_t2a");
            _liquidLit.SetTexture("_array", transparentArray);
            _liquidUnlit = Resources.Load<Material>("Faces/Materials/unlit_liquid_t2a");
            _liquidUnlit.SetTexture("_array", transparentArray);

            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            if (_indexes.IsCreated)
                _types.Dispose();

            if (_indexes.IsCreated)
                _indexes.Dispose();
            base.OnDestroy();
        }

        public Entity Load(Grpc.DomainName domainName)
        {
            if (_entities.ContainsKey(domainName))
                return _entities[domainName];

            Entity e = _em.CreateEntity(typeof(LoadingTag));
            _em.SetName(e, "Texture " + domainName.Domain + "/" + domainName.Name);
            _entities[domainName] = e;
            if (GetSingleton<Settings>().online)
                LoadAsync(e, domainName);
            else
                LoadLocal(e, domainName);
            return e;
        }

        public async void LoadLocal(Entity e, Grpc.DomainName gDomainName)
        {
            var settings = GetSingleton<Settings>();
            var path = $"content/1.17.1/minecraft/textures/{gDomainName.Name}.png";
            Texture2D texture2d = Resources.Load(path) as Texture2D; // TODO settings should have version
            if (texture2d == null)
                throw new Exception($"TextureSystem#LoadLocal failed to find file {gDomainName.Name}");
            LoadTextureToArray(e, gDomainName, texture2d);
        }

        public async void LoadAsync(Entity e, Grpc.DomainName gDomainName)
        {
            try
            {
                Grpc.Texture record = await Client.Instance.client.GetTextureAsync(gDomainName, Client.Instance.Metadata);
                Texture2D texture2d = new Texture2D(2, 2);
                texture2d.LoadImage(Convert.FromBase64String(record.Base64));
                LoadTextureToArray(e, gDomainName, texture2d);
            }
            catch (RpcException exception)
            {
                Debug.LogWarning($"TextureSystem#LoadAsync {gDomainName} \t {exception.Message}");
            }
        }

        private void LoadTextureToArray(Entity e, Grpc.DomainName gDomainName, Texture2D texture2d)
        {
            // Find empty array element
            // TextureType textureType = ComputeTextureType(record.Type);
            TextureType textureType = TextureType.Opaque; // TODO move this logic from server
            int index = Array.IndexOf(_elements[textureType], Entity.Null);
            if (index == -1)
                throw new Exception("#GetFreeIndex No free textureArray slots.");

            // Convert textureFormat
            TextureFormat textureFormat = TextureTypeToFormat(textureType);
            Texture2D dstTexture = new Texture2D(16, 16, textureFormat, true);
            if (texture2d.format != textureFormat)
                Debug.LogWarning($"===== Converting from {texture2d.format} to {textureFormat}");

            dstTexture.SetPixels(texture2d.GetPixels());
            dstTexture.Apply();
            Graphics.CopyTexture(dstTexture, 0, _arrays[textureType], index);

            _em.AddComponentData(e, new Texture()
            {
                domainName = new DomainName(gDomainName),
                type = textureType,
                index = index,
            });

            // Store array type and index
            _elements[textureType][index] = e;
            _em.RemoveComponent<LoadingTag>(e);
            _em.AddComponent<LoadedTag>(e);
        }

        TextureType ComputeTextureType(Oasis.Grpc.TextureType textureType)
        {
            if (textureType == Grpc.TextureType.Opaque)
                return TextureType.Opaque;
            if (textureType == Grpc.TextureType.Transparent)
                return TextureType.Transparent;
            if (textureType == Grpc.TextureType.Alphaclip)
                return TextureType.AlphaClip;

            Debug.LogError("Failed to parse texture type: " + textureType);
            return TextureType.Opaque;
        }

        public TextureFormat TextureTypeToFormat(TextureType textureType)
        {
            if (textureType == TextureType.Opaque)
                return OpaqueTextureFormat;
            else if (textureType == TextureType.AlphaClip)
                return AlphaClipTextureFormat;
            else if (textureType == TextureType.Transparent)
                return TransTextureFormat;
            else
            {
                Debug.LogError("Should never get here");
                return TextureFormat.Alpha8;
            }
        }

        public Texture ComputeTexture(Grpc.Model record, string tex)
        {
            var textureSystem = World.GetOrCreateSystem<TextureSystem>();
            string actual;
            if (tex.StartsWith("#"))
            {
                string stripped = tex.Substring(1);
                actual = record.Textures[stripped].Replace("minecraft:", "").Replace("block/", "");
            }
            else
                actual = tex.Replace("minecraft:", "").Replace("block/", "");


            var gDomainName = new Grpc.DomainName {Version = record.DomainName.Version, Domain = record.DomainName.Domain, Name = actual};
            var textureEntity = _entities[gDomainName];


            var texture = EntityManager.GetComponentData<Texture>(textureEntity);
            return texture;
        }
    }
}