using BovineLabs.Event.Systems;
using Unity.Entities;
using Unity.Transforms;

namespace Oasis.Core
{
    public partial class PlayerChunkChangeSystem : SystemBase
    {
        public bool triggerOnDefaultInt3;
        protected override void OnUpdate()
        {
            var eventSystem = this.World.GetOrCreateSystem<EventSystem>();
            var playerChunkChangeEventWriter = eventSystem.CreateEventWriter<PlayerChunkChangeEvent>();
            
            
            Entities
                .WithNone<LoadingTag>()
                .ForEach((Entity e, in PlayerChunk playerChunk, in Translation translation) =>
                {
                    var chunk = translation.Value.ToInt3().Chunk();
                    if (!playerChunk.Value.Equals(chunk) || triggerOnDefaultInt3)
                    {
                        triggerOnDefaultInt3 = false;
                        EntityManager.SetComponentData(e, new PlayerChunk() {Value = chunk});
                        playerChunkChangeEventWriter.Write(new PlayerChunkChangeEvent{Value = chunk});
                    }
                }).WithoutBurst().Run();
            
            eventSystem.AddJobHandleForProducer<PlayerChunkChangeEvent>(this.Dependency);
        }

    }
}