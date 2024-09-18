using DefaultNamespace;
using Unity.Entities;
using UnityEngine;

// Runtime
[InternalBufferCapacity(4)]
struct ProjectilePrefab : IBufferElementData
{
    public Entity EntityPrefab;
}

// Authoring
public class ProjectileStoreAuthor : MonoBehaviour
{
    public ProjectileAuthoring[] projectiles;

    class Baker : Baker<ProjectileStoreAuthor>
    {
        public override void Bake(ProjectileStoreAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var projectilePrefabs = AddBuffer<ProjectilePrefab>(entity);
            projectilePrefabs.Capacity = authoring.projectiles.Length;

            for (var i = 0; i < authoring.projectiles.Length; i++)
            {
                projectilePrefabs.Add(new ProjectilePrefab
                {
                    EntityPrefab = GetEntity(authoring.projectiles[i], TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace),
                });
            }
        }
    }
}