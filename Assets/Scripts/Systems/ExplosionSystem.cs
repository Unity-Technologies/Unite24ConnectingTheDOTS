using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(DeadSystem))]
    [UpdateAfter(typeof(UpdateConflictStateSystem))]
    public partial struct ExplosionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Lanes>();
            state.RequireForUpdate<DeadEntities>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var lanes = SystemAPI.GetSingletonRW<Lanes>();
            var deadEntities = SystemAPI.GetSingletonRW<DeadEntities>();
            var parallelWriter = deadEntities.ValueRW.Value.AsParallelWriter();
            
            var healthLookup = SystemAPI.GetComponentLookup<Health>();
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();


            foreach (var (damage, conflictState, blastRadius, entity) in
                     SystemAPI.Query<RefRO<Damage>, RefRO<ConflictState>, RefRO<BlastRadius>>().WithEntityAccess())
            {
                var inflictedDamage = damage.ValueRO.Value;
                var position = transformLookup[entity].Position;
                var blastRadiusSq = blastRadius.ValueRO.Value * blastRadius.ValueRO.Value;

                for (int i = 0; i < lanes.ValueRW.Value.Length; i++)
                {
                    var lane = lanes.ValueRW.Value[i];

                    foreach (var projectile in lane.Projectiles)
                    {
                        var distancesq = math.distancesq(transformLookup[projectile].Position, position);
                        if (distancesq <= blastRadiusSq)
                        {
                            parallelWriter.Enqueue(new DeadEntity() {Entity = projectile, EntityType = EntityType.Projectile, LaneIndex = i});
                        }
                    }

                    foreach (var coder in lane.Coders)
                    {
                        var distancesq = math.distancesq(transformLookup[coder].Position, position);
                        if (distancesq <= blastRadiusSq)
                        {
                            var coderHealth = healthLookup[coder];
                            coderHealth.Value -= inflictedDamage;
                            healthLookup[coder] = coderHealth;

                            // Mark Entity for deletion
                            if (coderHealth.Value <= 0)
                                parallelWriter.Enqueue(new DeadEntity() {Entity = coder, EntityType = EntityType.Coder, LaneIndex = i});
                        }
                    }

                    foreach (var bug in lane.Bugs)
                    {
                        var distancesq = math.distancesq(transformLookup[bug].Position, position);
                        if (distancesq <= blastRadiusSq)
                        {
                            var bugHealth = healthLookup[bug];
                            bugHealth.Value -= inflictedDamage;
                            healthLookup[bug] = bugHealth;

                            // Mark Entity for deletion
                            if (bugHealth.Value <= 0)
                                parallelWriter.Enqueue(new DeadEntity() {Entity = bug, EntityType = EntityType.Bug, LaneIndex = i});
                        }
                    }
                }

                // Destroy the current exploding bug
                var laneId = Lanes.FromTransform(transformLookup[entity]);
                parallelWriter.Enqueue(new DeadEntity() {Entity = entity, EntityType = EntityType.Bug, LaneIndex = laneId});
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}