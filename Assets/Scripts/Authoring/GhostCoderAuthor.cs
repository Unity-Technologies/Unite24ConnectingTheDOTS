using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class GhostCoderAuthor : MonoBehaviour
{
    public class GhostCoderAuthorBaker : Baker<GhostCoderAuthor>
    {
        public override void Bake(GhostCoderAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace);
            AddComponent(entity, new BuilderGhost
            {
                offset = authoring.transform.localToWorldMatrix,
            });
            AddComponent(entity, new GhostLastStoreIndex {Value = -1});
        }
    }
}

public struct BuilderGhost : IComponentData
{
    public float4x4 offset;
}

public struct GhostLastStoreIndex : IComponentData
{
    public int Value;
}
