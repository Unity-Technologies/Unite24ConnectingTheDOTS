using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class ExplosionAuthoring : MonoBehaviour
    {
        public int blastRadius;
        
        class Baker : Baker<ExplosionAuthoring>
        {
            public override void Bake(ExplosionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BlastRadius(){Value = authoring.blastRadius});
            }
        }
    }
}