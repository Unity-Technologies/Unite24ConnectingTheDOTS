using System;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// Runtime
[InternalBufferCapacity(64)]
struct BugPrefab : IBufferElementData
{
    public Entity EntityPrefab;
    public float startTime;
}

// Authoring
public class BugStoreAuthor : MonoBehaviour
{
    [Serializable]
    public struct BugSetupInfo
    {
        public BugAuthoring bugPrefab;
        public AnimationCurve spawnCurve;
    }

    [Serializable]
    public struct WaveSetupInfo
    {
        public BugSetupInfo[] bugs;
        public float waveDuration;
        public int subWaveCount;
        public AnimationCurve subWaveEnemyCountCurve;
    }
    
    public WaveSetupInfo[] Waves;

    class Baker : Baker<BugStoreAuthor>
    {
        public override void Bake(BugStoreAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var bugPrefabs = AddBuffer<BugPrefab>(entity);

            var random = new Random(124);
            float startTime = 0;
            foreach (var wave in authoring.Waves)
            {
                var subWaveDuration = wave.waveDuration / wave.subWaveCount;
                for (var subwaveIndex = 0; subwaveIndex < wave.subWaveCount; subwaveIndex++)
                {
                    startTime += subWaveDuration;
                    var highestCount = (int)wave.subWaveEnemyCountCurve.keys[^1].time;
                    var sum = 0f;
                    for (var i = 1; i < highestCount; i++)
                        sum += wave.subWaveEnemyCountCurve.Evaluate(i);
                    var t = random.NextFloat(sum);
                    var enemyCount = 0;
                    for (var i = 1; i < highestCount; i++)
                    {
                        t -= wave.subWaveEnemyCountCurve.Evaluate(i);
                        if (t <= 0)
                        {
                            enemyCount = i;
                            break;
                        }
                    }
                    
                    for (var i = 0; i < enemyCount; i++)
                    {
                        var bugIndex = random.NextInt(wave.bugs.Length);
                        var bug = wave.bugs[bugIndex];
                        bugPrefabs.Add(new BugPrefab
                        {
                            EntityPrefab = GetEntity(bug.bugPrefab.gameObject, TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace),
                            startTime = startTime
                        });
                    }
                }
                startTime += wave.waveDuration + 5f;
            }
        }
    }
}