using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[InternalBufferCapacity(4)]
public struct ModelHandleElement : IBufferElementData
{
    public MaterialMeshInfo Value;
    public static implicit operator ModelHandleElement(MaterialMeshInfo value) => new(){Value = value};

    public static void AddBufferNoInit(IBaker baker, Entity entity, int length)
    {
        baker.AddBuffer<ModelHandleElement>(entity).Resize(length, NativeArrayOptions.UninitializedMemory);
    }
    
    public static void AddPrefabModelToBuffer(IBaker baker, Entity entityWithModelHandles, int indexInModelHandleBuffer, GameObject prefabModel)
    {
        // Add to RenderMeshArray storage.
        // Baking of Entities.Graphics will:
        // 1. Add it to the common RenderMeshArray.
        // 2. Assign MaterialMeshInfo with the corresponding model and material index.
        var tempMesh = baker.CreateAdditionalEntity(TransformUsageFlags.None, true);
        baker.AddComponent(tempMesh, new RenderMeshUnmanaged
        {
            mesh = prefabModel.GetComponent<MeshFilter>().sharedMesh,
            materialForSubMesh = prefabModel.GetComponent<MeshRenderer>().sharedMaterial
        });
        baker.AddComponent<MaterialMeshInfo>(tempMesh);
        
        // Add MaterialMeshInfo to ModelHandle buffer
        baker.AddComponent(tempMesh, new WriteMeshInfoToModelHandle
        {
            entityWithModelHandles = entityWithModelHandles,
            indexInBuffer = indexInModelHandleBuffer
        });
    }
}

[TemporaryBakingType]
struct WriteMeshInfoToModelHandle : IComponentData
{
    public Entity entityWithModelHandles;
    public int indexInBuffer;
}

[UpdateInGroup(typeof(PostBakingSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial struct BakingModelHandlePostFixUp : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // copy individual MaterialMeshInfo to the ModelHandleElement buffer
        foreach (var (tempRenderer, materialMeshInfo) in SystemAPI.Query<RefRO<WriteMeshInfoToModelHandle>, RefRO<MaterialMeshInfo>>())
        {
            var models = SystemAPI.GetBuffer<ModelHandleElement>(tempRenderer.ValueRO.entityWithModelHandles);
            models[tempRenderer.ValueRO.indexInBuffer] = materialMeshInfo.ValueRO;
        }
    }
}
