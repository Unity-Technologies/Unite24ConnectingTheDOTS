using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    
    public partial struct DeadSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Lanes>();
            state.RequireForUpdate<DeadEntities>();
            
            var deadEntities = new NativeQueue<DeadEntity>(Allocator.Persistent);
            state.EntityManager.AddComponentData(state.SystemHandle, new DeadEntities(){Value = deadEntities});
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var lanes = SystemAPI.GetSingletonRW<Lanes>().ValueRW.Value;
            var deadEntities = SystemAPI.GetSingletonRW<DeadEntities>().ValueRW.Value;
            
            while (deadEntities.TryDequeue(out var deadEntity))
            {
                state.EntityManager.DestroyEntity(deadEntity.Entity);
                var lane = lanes[deadEntity.LaneIndex];
                
                if(deadEntity.EntityType == EntityType.Bug)
                    lane.Bugs.Remove(deadEntity.Entity);
                else if (deadEntity.EntityType == EntityType.Coder)
                    lane.Coders.Remove(deadEntity.Entity);
                else if (deadEntity.EntityType == EntityType.Projectile)
                    lane.Projectiles.Remove(deadEntity.Entity);

                lanes[deadEntity.LaneIndex] = lane;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            var deadEntities = SystemAPI.GetSingletonRW<DeadEntities>();
            deadEntities.ValueRW.Value.Dispose();
        }
    }
}