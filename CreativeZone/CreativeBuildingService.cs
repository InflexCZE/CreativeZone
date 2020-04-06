using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS;
using FlatBuffers;
using Service.Building;
using Service.Radiation;
using UnityEngine;

namespace CreativeZone
{
    public class CreativeBuildingService : CreativeService<CreativeBuildingService, IBuildingService>
    {
        [HarmonyReplace]
        public UID? CreateBuilding(BuildingType buildingType, GridPosition position, ObjectRotation rotation, bool _, Action<UID> additionalAction, List<ResourceAmount> initialInventoryData, bool putInitialInventoryInPreProduction)
        {
            try
            {
                CreativeEntityManager.Instance.SuppressDestroyEntity++;
                return this.Vanilla.CreateBuilding(buildingType, position, rotation, true, additionalAction, initialInventoryData, putInitialInventoryInPreProduction);
            }
            finally
            {
                CreativeEntityManager.Instance.SuppressDestroyEntity--;
            }
        }

        [HarmonyReplace]
        public void RequestBuildingDestruction(UID buildingEntity)
        {
            ServiceMapper.uIService.CloseAllInspectors();
            ServiceMapper.sessionService.GetSessionData().Destroy(buildingEntity);
        }

        [HarmonyReplace]
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
                CreateBuilding(upgradeToBuildingType, position, rotation, default, default, default, default);
            });
        }
    }
}
