using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct SpawnProjectileSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectilePrefab>();
            state.RequireForUpdate<Lanes>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<ProjectilePrefab>();
            var lanes = SystemAPI.GetSingletonRW<Lanes>().ValueRW;

            foreach (var (cooldown, projectileIndex, localTransform) in SystemAPI.Query<RefRW<Cooldown>, RefRO<ProjectileIndex>, RefRO<LocalTransform>>())
            {
                var laneId = Lanes.FromTransform(localTransform.ValueRO);
                var lane = lanes.Value[laneId];
                if(lane.Bugs.Length == 0)
                    continue;
                
                cooldown.ValueRW.Value -= SystemAPI.Time.DeltaTime;
                if (cooldown.ValueRO.Value > 0)
                    continue;

                var projectile = state.EntityManager.Instantiate(buffer[projectileIndex.ValueRO.Value].EntityPrefab);
                SystemAPI.SetComponent(projectile, localTransform.ValueRO);
                
                // add to lane
                lane.Projectiles.Add(projectile);
                lanes.Value[laneId] = lane;
                
                // Reset Cooldown
                cooldown.ValueRW.Value = cooldown.ValueRO.StartValue;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}