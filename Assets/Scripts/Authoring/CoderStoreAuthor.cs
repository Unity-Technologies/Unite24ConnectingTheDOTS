using System;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

// Runtime
[InternalBufferCapacity(4)]
struct CoderInfo : IBufferElementData
{
    public Entity EntityPrefab; // temporially not a weak reference
    public WeakObjectReference<Texture2D> Icon;
    public int CoffeeCost;
    public bool loadMe;
}

class UITemplates : IComponentData
{
    public VisualTreeAsset BuySlot;
}

// Authoring
public class CoderStoreAuthor : MonoBehaviour
{
    [Serializable]
    public struct CoderAuthorInfo
    {
        public CoderAuthoring coderPrefab;
        public Texture2D icon;
        public int cost;
    }
    public CoderAuthorInfo[] coders;
    
    [Header("Setup Assets")]
    public VisualTreeAsset UITemplateBuySlot;
    
#if UNITY_EDITOR
    class Baker : Baker<CoderStoreAuthor>
    {
        public override void Bake(CoderStoreAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var coderInfos = AddBuffer<CoderInfo>(entity);
            coderInfos.Capacity = authoring.coders.Length;
            for (var i = 0; i < authoring.coders.Length; i++)
            {
                var coderAuthorInfo = authoring.coders[i];
                coderInfos.Add(new CoderInfo
                {
                    EntityPrefab = GetEntity(coderAuthorInfo.coderPrefab.gameObject, TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace),
                    Icon = new WeakObjectReference<Texture2D>(coderAuthorInfo.icon),
                    CoffeeCost = coderAuthorInfo.cost,
                    loadMe = true // most games would set this at runtime
                });
                ModelHandleElement.AddPrefabModelToBuffer(this, entity, i, coderAuthorInfo.coderPrefab.gameObject);
            }
            ModelHandleElement.AddBufferNoInit(this, entity, authoring.coders.Length);

            // Add UI templates
            AddComponentObject(entity,  new UITemplates
            {
                BuySlot = authoring.UITemplateBuySlot
            });
        }
    }
#endif
}