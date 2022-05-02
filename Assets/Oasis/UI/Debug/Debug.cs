using Oasis.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Oasis.UI
{
    
    public class Debug : MonoBehaviour
    {
        private static Debug _instance;
        public static Debug Instance => _instance;
    
        private VisualElement _root;
        private float _nextActionTime;
        public float period = 0.1f;

        void Awake()
        {
            _instance = this;
            _root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            _nextActionTime = Time.time;
        }
    
 
        void Update () {
            if (Time.time > _nextActionTime)
                _nextActionTime += period;
            else
                return;
        
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var chunks = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ChunkSystem>();
            // var chunkCreateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ChunkCreate>();
            // _root.Q<TextField>("player-chunk").value = chunkCreateSystem.ChunkCurrent.ToStr();
        
            var highlightQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Highlight>());
            if (highlightQuery.CalculateEntityCount() < 1) return;
            var highlight = highlightQuery.GetSingleton<Highlight>();
        
            var facingQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerFacing>());
            if (facingQuery.CalculateEntityCount() < 1) return;
            var facing = facingQuery.GetSingleton<PlayerFacing>().Value;
            _root.Q<TextField>("player-facing").value = facing.ToString();
        
        
            // var highlight = em.GetComponentData<Highlight>();
        
        
        
            if (highlight.HitEntity != Entity.Null && chunks._entities.ContainsKey(highlight.Voxel.Chunk()))
            {
                _root.Q<TextField>("highlight-chunk").value = highlight.Voxel.Chunk().ToStr();
                _root.Q<TextField>("highlight-voxel").value = highlight.Voxel.ChunkVoxel().ToStr();
                _root.Q<TextField>("highlight-side").value = highlight.Side.ToString();
                _root.Q<TextField>("highlight-entity").value = highlight.HitEntity.ToString();
        
                var chunk = chunks._entities[highlight.Voxel.Chunk()];
                if (!em.HasComponent<VoxelElement>(chunk))
                    return;
            
                var voxels = em.GetBuffer<VoxelElement>(chunk);
                var voxel = voxels[highlight.Voxel.ChunkVoxel().ToIndex()];
                var blockStates = em.GetBuffer<BlockStateElement>(chunk);
                var blockStateEntity = blockStates[voxel.Value].Value;
        
                var blockState = em.GetComponentData<BlockState>(blockStateEntity);
                _root.Q<TextField>("highlight-block").value = blockState.domainName.name.ToString();
                var state = "";
                // foreach (Property p in blockState.states)
                //     state = $"{p.key}={p.value};{state}";
                _root.Q<TextField>("highlight-state").value = state;
            
            
                // Adjacent
                var adjacentChunk = chunks._entities[highlight.VoxelAdjacent.Chunk()];
                var adjacentVoxels = em.GetBuffer<VoxelElement>(adjacentChunk);
                var adjacentVoxel = adjacentVoxels[highlight.VoxelAdjacent.ChunkVoxel().ToIndex()];
                var adjacentBlockStates = em.GetBuffer<BlockStateElement>(adjacentChunk);
                Entity adjacentBlockStateEntity = adjacentBlockStates[adjacentVoxel.Value].Value;
                var adjacentBlockState = em.GetComponentData<BlockState>(adjacentBlockStateEntity);
                _root.Q<TextField>("adjacent-block").value = adjacentBlockState.domainName.name.ToString();
                const string adjacentState = "";
                // foreach (var p in blockState.states)
                //     state = $"{p.key}={p.value};{adjacentState}";
                _root.Q<TextField>("adjacent-state").value = adjacentState;
            }
            else
            {
                _root.Q<TextField>("highlight-chunk").value = "";
                _root.Q<TextField>("highlight-voxel").value = "";
                _root.Q<TextField>("highlight-side").value = "";
                _root.Q<TextField>("highlight-entity").value = "";
                _root.Q<TextField>("highlight-block").value = "";
                _root.Q<TextField>("highlight-state").value = "";
            
                _root.Q<TextField>("adjacent-block").value = "";
                _root.Q<TextField>("adjacent-state").value = "";
            }
        }

    }
}


// public int3 voxelAdjacent;
// public Quadrant quadrant;
// public Facing facing;
// public bool up;