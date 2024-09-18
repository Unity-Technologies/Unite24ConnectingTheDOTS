using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct PositionUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DeadEntities>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deadEntities = SystemAPI.GetSingletonRW<DeadEntities>();
            var parallelWriter = deadEntities.ValueRW.Value.AsParallelWriter();
            
            foreach (var (speed, transform, entity) in 
                     SystemAPI.Query<RefRO<Speed>, RefRW<LocalTransform>>().WithNone<ConflictState>().WithEntityAccess())
            {
                transform.ValueRW.Position.x += speed.ValueRO.Value * SystemAPI.Time.DeltaTime;
                
                // check if entity is out of bounds
                if (transform.ValueRW.Position.x > 10)
                {
                    // Mark Entity for deletion
                    parallelWriter.Enqueue(new DeadEntity() {Entity = entity, EntityType = EntityType.Projectile, LaneIndex = Lanes.FromTransform(transform.ValueRO)});
                }
                else if (transform.ValueRW.Position.x < -10)
                {
                    // Mark Entity for deletion
                    parallelWriter.Enqueue(new DeadEntity() {Entity = entity, EntityType = EntityType.Bug, LaneIndex = Lanes.FromTransform(transform.ValueRO)});
                    // TODO: We lost
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}