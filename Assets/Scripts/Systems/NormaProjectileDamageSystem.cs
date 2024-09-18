using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(DeadSystem))]
    [UpdateAfter(typeof(UpdateConflictStateSystem))]
    public partial struct NormalProjectileDamageSystem : ISystem
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
            
            var healthLookup = SystemAPI.GetComponentLookup<Health>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            var conflictStateLookup = SystemAPI.GetComponentLookup<ConflictState>();

            foreach (var (damage, conflictState, entity) in
                     SystemAPI.Query<RefRO<Damage>, RefRW<ConflictState>>().WithNone<Cooldown>().WithEntityAccess())
            {
                if (conflictState.ValueRW.ConflictType != ConflictType.Normal)
                    continue;

                var target = conflictState.ValueRO.Target;

                // if the target is already dead, early out
                if (!healthLookup.TryGetComponent(target, out var targetHealth))
                    return;
                if (targetHealth.Value <= 0)
                    return;

                targetHealth.Value -= damage.ValueRO.Value;
                healthLookup[target] = targetHealth;

                var laneId = Lanes.FromTransform(transformLookup[target]);
                parallelWriter.Enqueue(new DeadEntity() {Entity = entity, EntityType = EntityType.Projectile, LaneIndex = laneId});

                if (targetHealth.Value <= 0)
                {
                    parallelWriter.Enqueue(new DeadEntity() {Entity = target, EntityType = EntityType.Bug, LaneIndex = laneId});
                }

            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}