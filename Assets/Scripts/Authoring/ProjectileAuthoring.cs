using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        public int speed;
        public int damage;
        public ConflictType conflictType;
        
        class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace);
                AddComponent(entity, new Speed(){Value = authoring.speed});
                AddComponent(entity, new Damage(){Value = authoring.damage});
                AddComponent(entity, new ConflictState(){ConflictType = authoring.conflictType});
        
                SetComponentEnabled<ConflictState>(entity, false);
            }
        }
    }
}