using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct SpawnBugSystem : ISystem
    {
        int m_SpawnCount;
        Random m_Random;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SpawnCount = 0;
            m_Random = new Random(1234);
            
            state.RequireForUpdate<BugPrefab>();
            state.RequireForUpdate<Lanes>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<BugPrefab>();
            var lanes = SystemAPI.GetSingletonRW<Lanes>().ValueRW;

            var startPos = new float3(10, 0, -1);

            if (m_SpawnCount >= buffer.Length)
                return;
            
            while (buffer[m_SpawnCount].startTime < SystemAPI.Time.ElapsedTime)
            {
                var bugPrefab = buffer[m_SpawnCount];
                
                var laneId = m_Random.NextInt(0, lanes.Value.Length);
                startPos.z = laneId;

                var bug = state.EntityManager.Instantiate(bugPrefab.EntityPrefab);
                var currentTransform = SystemAPI.GetComponent<LocalTransform>(bug);
                currentTransform.Position.xz += startPos.xz;
                SystemAPI.SetComponent(bug, currentTransform);

                // add to lane
                var lane = lanes.Value[laneId];
                lane.Bugs.Add(bug);
                lanes.Value[laneId] = lane;

                m_SpawnCount++;
                if (m_SpawnCount >= buffer.Length)
                    break;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}