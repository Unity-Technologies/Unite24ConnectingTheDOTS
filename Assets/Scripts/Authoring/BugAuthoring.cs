using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class BugAuthoring : MonoBehaviour
{
    public int health;
    public int damage;
    public int speed;
    public float eatCooldown;
    public ConflictType conflictType;

    class Baker : Baker<BugAuthoring>
    {
        public override void Bake(BugAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace);
            AddComponent(entity, new Health(){Value = authoring.health});
            AddComponent(entity, new Damage(){Value = authoring.damage});
            AddComponent(entity, new Speed(){Value = -authoring.speed});
            AddComponent(entity, new Cooldown(){Value = 0, StartValue = authoring.eatCooldown});
            AddComponent(entity, new ConflictState(){ConflictType = authoring.conflictType});
        
            SetComponentEnabled<ConflictState>(entity, false);
            
        }
    }
}
