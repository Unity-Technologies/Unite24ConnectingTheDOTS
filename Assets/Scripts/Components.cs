using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public struct Health : IComponentData
{
    public int Value;
}

public struct Damage : IComponentData
{
    public int Value;
}

public struct Speed : IComponentData
{
    public int Value;
}

public struct BlastRadius : IComponentData
{
    public int Value;
}

public struct Cooldown : IComponentData
{
    public float Value;
    public float StartValue;
}

public struct CoinValue : IComponentData
{
    public int Value;
}

public struct ConflictState : IComponentData, IEnableableComponent
{
    public ConflictType ConflictType;
    public Entity Target;
}

public struct ProjectileIndex : IComponentData
{
    public int Value;
}

public enum ConflictType
{
    None,
    Normal,
    Explosion
}

public struct Lane
{
    public FixedList512Bytes<Entity> Coders;
    public FixedList512Bytes<Entity> Bugs;
    public FixedList512Bytes<Entity> Projectiles;
}

public struct Lanes : IComponentData
{
    public NativeArray<Lane> Value;

    public static int FromTransform(LocalTransform localTransform)
    {
        return (int)(localTransform.Position.z + 0.5f);
    }

    public Lanes(int laneCount)
    {
        Value = new NativeArray<Lane>(laneCount, Allocator.Persistent);

        for (int i = 0; i < laneCount; i++)
        {
            Value[i] = new Lane()
            {
                Coders = new FixedList512Bytes<Entity>(),
                Bugs = new FixedList512Bytes<Entity>(),
                Projectiles = new FixedList512Bytes<Entity>()
            };
        }
    }
}

public struct DeadEntities : IComponentData
{
    public NativeQueue<DeadEntity> Value;
}

public struct DeadEntity
{
    public int LaneIndex;
    public EntityType EntityType;
    public Entity Entity;
}


public enum EntityType
{
    Bug,
    Projectile,
    Coder
}
