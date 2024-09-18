using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(DeadSystem))]
    [UpdateAfter(typeof(UpdateConflictStateSystem))]
    public partial struct NormalBugDamageSystem : ISystem
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

            foreach (var (damage, conflictState, cooldown, entity) in
                     SystemAPI.Query<RefRO<Damage>, RefRW<ConflictState>, RefRW<Cooldown>>().WithEntityAccess())
            {
                if (conflictState.ValueRW.ConflictType != ConflictType.Normal)
                    continue;

                cooldown.ValueRW.Value -= SystemAPI.Time.DeltaTime;
                if (cooldown.ValueRO.Value > 0)
                    continue;

                var target = conflictState.ValueRO.Target;

                // if the target is still alive, apply damage
                if (!healthLookup.HasComponent(target))
                {
                    Debug.Log($"Should have been deleted, {entity}, {target}");
                }
                var targetHealth = healthLookup[target];
                if (targetHealth.Value <= 0)
                {
                    return;
                }

                targetHealth.Value -= damage.ValueRO.Value;
                healthLookup[target] = targetHealth;
                cooldown.ValueRW.Value = cooldown.ValueRO.StartValue;

                if (targetHealth.Value <= 0)
                {
                    var laneId = Lanes.FromTransform(transformLookup[target]);

                    // Mark Entity for deletion
                    parallelWriter.Enqueue(new DeadEntity() {Entity = target, EntityType = EntityType.Coder, LaneIndex = laneId});
                    conflictState.ValueRW.Target = Entity.Null;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}