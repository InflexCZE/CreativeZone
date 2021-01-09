using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS;
using FlatBuffers;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using Service.Building;
using Service.Radiation;
using Service.UI;
using UnityEngine;

namespace CreativeZone.Services
{
    public class CreativeBuildingService : ProxyService<CreativeBuildingService, IBuildingService>
    {
        [LogicProxy]
        public UID? CreateBuilding(BuildingType buildingType, GridPosition position, ObjectRotation rotation, bool _, int visualID = -1, Action<UID> additionalAction = null, List<InventoryComponent2.ResourceAmount> initialInventoryData = null, bool putInitialInventoryInPreProduction = false)
        {
            try
            {
                CreativeEntityManager.Instance.SuppressDestroyEntity++;
                return this.Vanilla.CreateBuilding(buildingType, position, rotation, true, visualID, additionalAction, initialInventoryData, putInitialInventoryInPreProduction);
            }
            finally
            {
                CreativeEntityManager.Instance.SuppressDestroyEntity--;
            }
        }

        [LogicProxy]
        public void RequestBuildingDestruction(UID buildingEntity)
        {
            ServiceMapper.uIService.CloseWindow(WindowTypeEnum.InspectorWindow);
            ServiceMapper.sessionService.GetSessionData().Destroy(buildingEntity);
        }

        [LogicProxy]
        public void RequestBuildingUpgrade(UID buildingEntity, BuildingType upgradeToBuildingType)
        {
            var grid = ServiceMapper.gridService;
            var entity = ServiceMapper.entityManager;

            var transform = entity.GetComponent<TransformComponent>(buildingEntity);
            var building = entity.GetComponent<BuildingDataComponent>(buildingEntity);

            var rotation = building.rotation;
            var position = grid.GetGridPositionForWorldPos(transform.position);

            //Disable disappearing effect
            entity.RemoveComponent<RenderComponent>(buildingEntity); 
            RequestBuildingDestruction(buildingEntity);

            CreativeEntityManager.Instance.Invoke(() =>
            {
                CreateBuilding(upgradeToBuildingType, position, rotation, default);
            });
        }
    }
}
