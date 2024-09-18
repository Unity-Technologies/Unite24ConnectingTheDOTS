using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class ThrowAuthoring : MonoBehaviour
    {
        public int cooldown;
        public int projectileIndex;

        public class Baker : Baker<ThrowAuthoring>
        {
            public override void Bake(ThrowAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Cooldown {Value = authoring.cooldown, StartValue = authoring.cooldown});
                AddComponent(entity, new ProjectileIndex {Value = authoring.projectileIndex});
            }
        }
    }
}