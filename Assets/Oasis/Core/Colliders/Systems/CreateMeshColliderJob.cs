using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile]
public struct CreateMeshColliderJob : IJob
{
    [ReadOnly] public NativeArray<float3> MeshVerts;
    [ReadOnly] public NativeArray<int> MeshTris;
    public NativeArray<BlobAssetReference<Collider>> BlobCollider;

    public void Execute()
    {
        var cVerts = new NativeArray<float3>(MeshVerts.Length, Allocator.Temp);
        var cTris = new NativeArray<int3>(MeshTris.Length / 3, Allocator.Temp);

        for (var i = 0; i < MeshVerts.Length; i++)
            cVerts[i] = MeshVerts[i];

        var ii = 0;
        for (var j = 0; j < MeshTris.Length; j += 3)
            cTris[ii++] = new int3(MeshTris[j], MeshTris[j + 1], MeshTris[j + 2]);

        
        var filter = new CollisionFilter()
        {
            BelongsTo = 1u << 4,
            CollidesWith = ~0u, // all 1s, so all layers, collide with everything
            GroupIndex = 0
        };
        
        
        
        // Debug.Log(string.Join(", ", CVerts));
        BlobCollider[0] = MeshCollider.Create(cVerts, cTris, filter);

        // TODO Optimize with Quad/Plane collider
        // BlobCollider[0] = PolygonCollider.CreateQuad(v.c0, v.c1, v.c2, v.c3, filter);
        
        cVerts.Dispose();
        cTris.Dispose();
    }
}
