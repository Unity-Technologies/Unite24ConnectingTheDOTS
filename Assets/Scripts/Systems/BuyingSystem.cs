using System;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[UpdateAfter(typeof(UISystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct BuyingSystem : ISystem, ISystemStartStop
{
    class Singleton : IComponentData
    {
        public VisualElement BuyingPane;
        public Label CurrencyLabel;
        public Camera Camera;
        int m_TotalCurrency;
        public int TotalCurrency => m_TotalCurrency;
        public void AddCurrency(int amount)
        {
            m_TotalCurrency += amount;
            CurrencyLabel.text = $"{TotalCurrency}c";   
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CoderInfo>();
        state.RequireForUpdate<ModelHandleElement>();
        state.RequireForUpdate<UISystem.Singleton>();
        state.RequireForUpdate<BuilderGhost>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        // Get Buying Pane
        var uiSingleton = SystemAPI.ManagedAPI.GetSingleton<UISystem.Singleton>();
        var buyingPane = uiSingleton.Root.Q("buying-pane");

        // Get ghost query
        var builderGhost = SystemAPI.GetSingletonEntity<BuilderGhost>();
        
        // Add buttons
        var coderInfos = SystemAPI.GetSingletonBuffer<CoderInfo>();
        var modelHandles = SystemAPI.GetSingletonBuffer<ModelHandleElement>();
        for (var codeStoreIndex = 0; codeStoreIndex < coderInfos.Length; codeStoreIndex++)
        {
            var coderInfo = coderInfos[codeStoreIndex];
            if (!coderInfo.loadMe)
                continue;

            // to be displayed on load
            coderInfo.Icon.LoadAsync();

            // add button
            var uiTemplates = SystemAPI.ManagedAPI.GetSingleton<UITemplates>();
            var coderSlot = uiTemplates.BuySlot.Instantiate();
            var button = coderSlot.Q<Button>();
            coderSlot.Q<Label>().text = $"{coderInfo.CoffeeCost}c";
            button.userData = codeStoreIndex;
            buyingPane.Add(coderSlot);

            // set model drawn by ghost
            button.RegisterCallback<ClickEvent, (WorldUnmanaged w, Entity target,  MaterialMeshInfo m, int codeStoreIndex)>(
                static (_, data) =>
                {
                    data.w.EntityManager.SetComponentData(data.target, data.m);
                    data.w.EntityManager.SetComponentData(data.target, new GhostLastStoreIndex{Value = data.codeStoreIndex});
                },
                (state.WorldUnmanaged, builderGhost, modelHandles[codeStoreIndex].Value, codeStoreIndex)
            );
        }
        SystemAPI.SetComponent(builderGhost, default(MaterialMeshInfo));

        var entity = state.EntityManager.CreateEntity(typeof(Singleton));
        var singleton = new Singleton
        {
            BuyingPane = buyingPane,
            CurrencyLabel = uiSingleton.Root.Q<Label>("currency-show"),
            Camera = Camera.main
        };
        singleton.AddCurrency(100);
        state.EntityManager.SetComponentData(entity, singleton);
#if UNITY_EDITOR
        state.EntityManager.SetName(entity, "S:BuyingSystem");
#endif
    }

    public void OnUpdate(ref SystemState state)
    {
        var buyingSingleton = SystemAPI.ManagedAPI.GetSingleton<Singleton>();
        foreach (var slot in buyingSingleton.BuyingPane.Children())
        {
            var button = slot.Q<Button>();
            var coderInfo = SystemAPI.GetSingletonBuffer<CoderInfo>()[(int)button.userData];
            button.SetEnabled(coderInfo.CoffeeCost <= buyingSingleton.TotalCurrency);
            var isBackgroundMissing = button.style.backgroundImage.keyword == StyleKeyword.Null;
            if (isBackgroundMissing && coderInfo.Icon.LoadingStatus == ObjectLoadingStatus.Completed)
                button.style.backgroundImage = coderInfo.Icon.Result;
        }

        var builderGhostQuery = SystemAPI.QueryBuilder().WithAll<BuilderGhost>().Build();
        var mouseScreenPosition = Mouse.current.position.ReadValue();
        var ray = buyingSingleton.Camera.ScreenPointToRay(mouseScreenPosition);
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // buy coder
            var coderInfos = SystemAPI.GetSingletonBuffer<CoderInfo>();
            var builderGhost = builderGhostQuery.GetSingletonEntity();
            var codeStoreIndex = SystemAPI.GetComponent<GhostLastStoreIndex>(builderGhost).Value;
            if (codeStoreIndex < 0 || codeStoreIndex >= coderInfos.Length)
                return;
            var coderInfo = coderInfos[codeStoreIndex];
            if (coderInfo.CoffeeCost <= buyingSingleton.TotalCurrency)
            {
                // check if no coder is already there
                var localTransform = SystemAPI.GetComponent<LocalTransform>(builderGhost);
                var lanes = SystemAPI.GetSingletonRW<Lanes>().ValueRW;
                var laneId = Lanes.FromTransform(localTransform);
                if (laneId < 0 || laneId >= lanes.Value.Length)
                    return;
                
                var lane = lanes.Value[laneId];
                var hasCollision = false;
                foreach (var coderEntity in lane.Coders)
                {
                    var coderLocalTransform = SystemAPI.GetComponent<LocalTransform>(coderEntity);
                    if (math.distance(localTransform.Position, coderLocalTransform.Position) < 0.1f)
                    {
                        hasCollision = true;
                        break;
                    }
                }
                if (hasCollision)
                    return;

                // buy coder
                buyingSingleton.AddCurrency(-coderInfo.CoffeeCost);
                
                // instantiate coder prefab
                var coder = state.EntityManager.Instantiate(coderInfo.EntityPrefab);
                SystemAPI.SetComponent(coder, localTransform);
                
                // add to lane
                lane.Coders.Add(coder);
                lanes.Value[laneId] = lane;

                // reset ghost
                SystemAPI.SetComponent(builderGhost, default(MaterialMeshInfo));
                SystemAPI.SetComponent(builderGhost, new GhostLastStoreIndex{Value = -1});
            }
                
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            var builderGhost = builderGhostQuery.GetSingletonEntity();
            SystemAPI.SetComponent(builderGhost, new GhostLastStoreIndex{Value = -1});
            SystemAPI.SetComponent(builderGhost, default(MaterialMeshInfo));
        }

        // get mouse position on plane
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance))
        {
            var point = ray.GetPoint(distance);
            var gridPoint = new float3(Mathf.RoundToInt(point.x+0.5f), 0, Mathf.RoundToInt(point.z+0.5f));
            var e = SystemAPI.GetSingletonEntity<BuilderGhost>();
            var b = SystemAPI.GetComponent<BuilderGhost>(e);
            SystemAPI.SetComponent(e, LocalTransform.FromMatrix(b.offset).Translate(gridPoint));
        }
    }

    public void OnStopRunning(ref SystemState state) {}
}

// UISystem
public partial struct UISystem : ISystem, ISystemStartStop
{
    public class Singleton : IComponentData
    {
        public VisualElement Root;
    }
    
    public void OnStartRunning(ref SystemState state)
    {
        // Don't run UI when document is not in root scene
        var uiDocument = Object.FindObjectOfType<UIDocument>();
        if (uiDocument == null)
            return;
        
        // Create entity with Singleton
        var entity = state.EntityManager.CreateEntity(typeof(Singleton));
        state.EntityManager.SetComponentData(entity, new Singleton
        {
            Root = uiDocument.rootVisualElement
        });
#if UNITY_EDITOR
        state.EntityManager.SetName(entity, "S:UISystem");
#endif
    }

    public void OnStopRunning(ref SystemState state) {}
}