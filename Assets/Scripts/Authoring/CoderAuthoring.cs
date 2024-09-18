using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class CoderAuthoring : MonoBehaviour
{
    public int health;
    
    class Baker : Baker<CoderAuthoring>
    {
        public override void Bake(CoderAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Health(){Value = authoring.health});
        }
    }
}
