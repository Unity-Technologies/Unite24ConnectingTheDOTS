using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct UpdateConflictStateSystem : ISystem
    {
        private const float DefaultCollisionDistance = 0.5f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Lanes>();
            state.EntityManager.AddComponentData(state.SystemHandle, new Lanes(9));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var lanes = SystemAPI.GetSingletonRW<Lanes>();

            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            var conflictStateLookup = SystemAPI.GetComponentLookup<ConflictState>();
            var entityStorageLookup = SystemAPI.GetEntityStorageInfoLookup();

            state.Dependency = new DetectCollisionJob()
            {
                Lanes = lanes.ValueRW.Value,
                LocalTransformLookup = localTransformLookup,
                EntityStorageInfo = entityStorageLookup,
                ConflictStateLookup = conflictStateLookup
            }.ScheduleParallel(lanes.ValueRW.Value.Length, 1, state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        struct DetectCollisionJob : IJobFor
        {
            [ReadOnly] public NativeArray<Lane> Lanes;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfo;

            [NativeDisableParallelForRestriction] public ComponentLookup<ConflictState> ConflictStateLookup;

            public void Execute(int index)
            {
                var lane = Lanes[index];

                for (int j = 0; j < lane.Bugs.Length; j++)
                {
                    var bug = lane.Bugs[j];
                    var bugPos = LocalTransformLookup[bug].Position.x;
                    var updateCoderCollision = true;

                    // if bug is already in conflict, skip
                    if (ConflictStateLookup.IsComponentEnabled(bug))
                    {
                        var bugConflictState = ConflictStateLookup[bug];
                        if (bugConflictState.Target == Entity.Null || !EntityStorageInfo.Exists(bugConflictState.Target))
                        {
                            // if the target is invalid, reset
                            ConflictStateLookup.SetComponentEnabled(bug, false);
                        }
                        else
                        {
                            updateCoderCollision = false;
                        }
                    }

                    if (updateCoderCollision)
                    {
                        // Bugs check for Coders
                        for (int k = 0; k < lane.Coders.Length; k++)
                        {
                            var coder = lane.Coders[k];
                            var coderPos = LocalTransformLookup[coder].Position.x;

                            // I am in within eating distance
                            if (math.abs(coderPos - bugPos) < DefaultCollisionDistance)
                            {
                                // Set collision target
                                ConflictStateLookup.SetComponentEnabled(bug, true);
                                var bugConflictState = ConflictStateLookup[bug];
                                bugConflictState.Target = coder;
                                ConflictStateLookup[bug] = bugConflictState;

                                // Early out
                                break;
                            }
                        }
                    }

                    // Bugs check for Projectiles
                    for (int k = 0; k < lane.Projectiles.Length; k++)
                    {
                        var projectile = lane.Projectiles[k];
                        var projectilePos = LocalTransformLookup[projectile].Position.x;

                        // Projectile is in range of the Bug
                        if (math.abs(projectilePos - bugPos) < DefaultCollisionDistance)
                        {
                            // Set collision target
                            ConflictStateLookup.SetComponentEnabled(projectile, true);
                            var projectileConflictState = ConflictStateLookup[projectile];
                            projectileConflictState.Target = bug;
                            ConflictStateLookup[projectile] = projectileConflictState;
                        }
                    }
                }
            }
        }
    }
}