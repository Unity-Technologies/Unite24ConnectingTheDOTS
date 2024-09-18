using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class CurrencyAuthoring : MonoBehaviour
    {
        public int coinValue;
        public int cooldown;
        
        class Baker : Baker<CurrencyAuthoring>
        {
            public override void Bake(CurrencyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CoinValue(){Value = authoring.coinValue});
                AddComponent(entity, new Cooldown(){Value = authoring.cooldown, StartValue = authoring.cooldown});
            }
        }
    }
}