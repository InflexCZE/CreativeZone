using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS;
using FlatBuffers;
using Service.Building;
using Service.UserWorldTasks;

namespace CreativeZone
{
    public class CreativeUserWorldTaskService : CreativeService<CreativeUserWorldTaskService, IUserWorldTasksService>
    {
        [HarmonyReplace]
        public void _CreateRemoveStreetTasks(int startCellX, int startCellY, int endCellX, int endCellY)
        {
            for (int x = startCellX; x < endCellX; x++)
            for (int y = startCellY; y < endCellY; y++)
            {
                ServiceMapper.streetService.RemoveStreet(x, y);
            }
        }

        [HarmonyReplace]
        public void _CreateRemoveRadiationTasks(int startCellX, int startCellY, int endCellX, int endCellY)
        {
            for (int x = startCellX; x < endCellX; x++)
            for (int y = startCellY; y < endCellY; y++)
            {
                ServiceMapper.radiationService.RemoveRadiation(x, y);
            }
        }

        [HarmonyReplace] //Aka add radiation
        public void _KillAllRemoveRadiationWorldTasks(int startCellX, int startCellY, int endCellX, int endCellY)
        {
            for (int x = startCellX; x < endCellX; x++)
            for (int y = startCellY; y < endCellY; y++)
            {
                ServiceMapper.radiationService.ChangeRadiation(x, y, 0.4f);
            }
        }

        [HarmonyProperty]
        public bool IsDragging { get; set; }

        /*
        [HarmonyExpose]
        private void CalculateSelectionBoundingBox(int dragStartX, int dragStartY, int dragEndX, int dragEndY, ref int startX, ref int startY, ref int endX, ref int endY)
        {

        }
        */

        [HarmonyReplace]
        private void CreateGatherResourcesTasks(List<UID> _, ResourceComponent.ResourceType? __)
        {
            //TODO: Don't stop mode on click
            this.IsDragging = true;

            /*
            this.cellDrop = this.gridService.GetGridPositionForScreenPoint(this.userInputService.GetPointerPosition());
            int num7 = 0;
            int num8 = 0;
            int num9 = 0;
            int num10 = 0;
            this.CalculateSelectionBoundingBox(this.cellDrag.x, this.cellDrag.y, this.cellDrop.x, this.cellDrop.y, ref num7, ref num8, ref num9, ref num10);
            int sizeX = num9 - num7;
            int sizeY = num10 - num8;
            if (this.currentMode == UserWorldTaskMode.SetBuilding && this.fieldBuildingTypes.Contains(this.currentBuildingComponents.buildingData.buildingType) && this.CanPlaceField(num7, num8, sizeX, sizeY))
            {
                this.buildingService.CreateField(this.currentBuildingComponents.buildingData.buildingType, num7, num8, num9 - num7, num10 - num8);
            }
            if (this.currentMode != UserWorldTaskMode.CreateRoad && this.currentMode != UserWorldTaskMode.None && this.currentMode != UserWorldTaskMode.CancelRemoveRadiation && this.currentMode != UserWorldTaskMode.SetBuilding && this.currentMode != UserWorldTaskMode.RemoveRoad)
            {
                this.entityCache = this.gridService.GetEntitiesForCellArea(num7, num8, num9, num10);
            }
            */

            switch (this.Vanilla.GetCurrentMode())
            {
                case UserWorldTaskMode.GatherWood:
                {
                    break;
                }
                case UserWorldTaskMode.GatherAllResources:
                case UserWorldTaskMode.GatherScrap:
                    break;
            }
        }

        private void PlaceTrees()
        {
            /*
            x = Data.Tree.distanceBetweenTrees;
            while ((float)x < result.terrainResult.terrainData.data.xSize - (float)Data.Tree.distanceBetweenTrees)
            {
                int y = Data.Tree.distanceBetweenTrees;
                while ((float)y < result.terrainResult.terrainData.data.ySize - (float)Data.Tree.distanceBetweenTrees)
                {
                    _towncenterCheckPos.x = (float)x / result.terrainResult.terrainData.data.xSize * (float)result.sizeX;
                    _towncenterCheckPos.z = (float)y / result.terrainResult.terrainData.data.ySize * (float)result.sizeY;
                    if (!this.IsTowncenterArea(result, normalizedTowncenterPosition, _towncenterCheckPos))
                    {
                        if (result.terrainResult.terrainData.GetGrassForWorldPosition((float)x, (float)y) > forestCheck && UnityEngine.Random.Range(0f, 0.57f) > 0.35f)
                        {
                            float bilinearHeightForWorldPosition = result.terrainResult.terrainData.GetBilinearHeightForWorldPosition((float)x, (float)y);
                            Vector3 vector = new Vector3((float)x, bilinearHeightForWorldPosition, (float)y);
                            if (this.gridService.GetCellFlagsByWorldPos(vector).HasAnyFlagNonAlloc(CellFlag.BlockPathfinding | CellFlag.HasBuilding | CellFlag.Footprint) || !ServiceMapper.pathfindingService.IsReachableWorld(vector))
                            {
                                goto IL_1226;
                            }
                            float num8 = treeBiomeNoiseGenerator.GetSimplex((float)x, (float)y) * 0.5f + 0.5f;
                            if (num8 > this.treeBiomeLeaves && num8 < this.treeBiomeConifer)
                            {
                                float t = Mathf.Abs(num8.Map(this.treeBiomeLeaves, this.treeBiomeConifer, -1f, 1f));
                                num8 = Mathf.Lerp(((float)x * 3.1415f + (float)y) * 11.09f % 1f, num8, t);
                            }
                            UID? uid;
                            if ((int)(num8 * 100f) >= 50)
                            {
                                uid = this.worldResourceService.CreateTree(TreeComponent.TreeType.leaf, vector, -1f);
                            }
                            else
                            {
                                uid = this.worldResourceService.CreateTree(TreeComponent.TreeType.conifer, vector, -1f);
                            }
                            if (uid != null)
                            {
                                TransformComponent component = this.entityManager.GetComponent<TransformComponent>(uid.Value);
                                vector.x += UnityEngine.Random.Range(-maxTreePositionOffset, maxTreePositionOffset);
                                vector.z += UnityEngine.Random.Range(-maxTreePositionOffset, maxTreePositionOffset);
                                component.position = vector;
                                component.isDirty = true;
                                result.trees.Add(uid.Value);
                            }
                        }
                        if (this.CheckClock())
                        {
                            yield return null;
                            this.generateWatch.Restart();
                        }
                    }
                IL_1226:
                    y += Data.Tree.distanceBetweenTrees;
                }
                x += Data.Tree.distanceBetweenTrees;
            }
            this.terrainService.ApplyTextureChanges(true);
            */
        }
    }
}
