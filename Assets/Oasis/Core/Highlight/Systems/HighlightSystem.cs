using Oasis.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Math = System.Math;

public partial class HighlightSystem : SystemBase
{
    private BuildPhysicsWorld _buildPhysicsWorld;
    private CollisionWorld _collisionWorld;
    private CollisionFilter _collisionFilter;
    private float rayDistance = 100f;
    private EntityQuery _query;
    private ChunkSystem _chunkSystem;

    private bool _up;
    private bool _hit;
    public uint actualId;

    private Entity _highlightEntity;
    
    protected override void OnCreate()
    {
        _chunkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ChunkSystem>();
        _buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        _collisionFilter = new CollisionFilter
        {
            BelongsTo = 1u << 3,
            CollidesWith = 1u << 4,
            GroupIndex = 0,
        };

        RequireSingletonForUpdate<FirstPersonCharacterComponent>();
        RequireSingletonForUpdate<PrefabHighlight>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        // Voxel = int3.zero;
        // VoxelAdjacent = int3.zero;
        
        if (_highlightEntity == Entity.Null)
            _highlightEntity = EntityManager.Instantiate(GetSingleton<PrefabHighlight>().Value);

        var highlight = GetComponent<Highlight>(_highlightEntity);
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        var raycastInput = new RaycastInput()
        {
            Start = ray.origin,
            End = ray.origin + (ray.direction * rayDistance),
            Filter = _collisionFilter
        };
        var collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;
        collisionWorld.CastRay(raycastInput, out var rch);
        highlight.HitEntity = _buildPhysicsWorld.PhysicsWorld.Bodies[rch.RigidBodyIndex].Entity;
        
        // If hit pending chunk
        if (HasComponent<Chunk>(highlight.HitEntity))
        {
            highlight.Chunk = GetComponent<Chunk>(highlight.HitEntity).id;
            EntityManager.SetComponentData(_highlightEntity, new Translation { Value = new float3(0f)});
            return;
        }
        
        highlight.Voxel = (rch.Position - (rch.SurfaceNormal * 0.05f)).ToInt3();
        highlight.VoxelAdjacent = (rch.Position + (rch.SurfaceNormal * 0.05f)).ToInt3();
        highlight.Chunk = highlight.Voxel.Chunk();
        EntityManager.SetComponentData(_highlightEntity, new Translation { Value = highlight.Voxel + new float3(0.5f)});

        if (highlight.VoxelAdjacent.xyz.y > highlight.Voxel.xyz.y)
             highlight.Side = Side.Up;
        else if (highlight.VoxelAdjacent.xyz.y < highlight.Voxel.xyz.y)
            highlight.Side = Side.Down;
        else if (highlight.VoxelAdjacent.xyz.x > highlight.Voxel.xyz.x)
            highlight.Side = Side.East;
        else if (highlight.VoxelAdjacent.xyz.x < highlight.Voxel.xyz.x)
            highlight.Side = Side.West;
        else if (highlight.VoxelAdjacent.xyz.z > highlight.Voxel.xyz.z)
            highlight.Side = Side.North;
        else if (highlight.VoxelAdjacent.xyz.z < highlight.Voxel.xyz.z)
            highlight.Side = Side.South;

        // Pointing
        // int y = (int)Math.Round(Global.ySource.localEulerAngles.y);
        // string pointing;
        // if (y > 315 & y < 45) { pointing = "north"; } else if (y > 45 && y < 135) { pointing = "east"; } else if (y > 135 && y < 225) { pointing = "south"; } else { pointing = "west"; }

        // Quadrant
        highlight.Quadrant = Quadrant.None;
        if (highlight.Side == Side.Up)
        {
            if (((rch.Position.x - Math.Truncate(rch.Position.x)) >= 0.5) &&
                ((rch.Position.z - Math.Truncate(rch.Position.z)) >= 0.5))
                highlight.Quadrant = Quadrant.NorthEast;

            if (((rch.Position.x - Math.Truncate(rch.Position.x)) >= 0.5) &&
                ((rch.Position.z - Math.Truncate(rch.Position.z)) < 0.5))
                highlight.Quadrant = Quadrant.SouthEast;

            if (((rch.Position.x - Math.Truncate(rch.Position.x)) < 0.5) &&
                ((rch.Position.z - Math.Truncate(rch.Position.z)) < 0.5))
                highlight.Quadrant = Quadrant.SouthWest;

            if (((rch.Position.x - Math.Truncate(rch.Position.x)) < 0.5) &&
                ((rch.Position.z - Math.Truncate(rch.Position.z)) >= 0.5))
                highlight.Quadrant = Quadrant.NorthWest;
        }
        // TODO Quadrant for other sides

        
        EntityManager.SetComponentData(_highlightEntity, highlight);
    }
    
    // public Oasis.Proto.Pointer ToPb()
    // {
    //     return new Oasis.Proto.Pointer { Side = side.ToString().ToLower(), Quadrant = quadrant.ToString().ToLower(), Facing = facing.ToString().ToLower(), Up = up };
    // }

}
    
