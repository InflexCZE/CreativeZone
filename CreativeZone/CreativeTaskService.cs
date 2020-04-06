using System;
using System.Collections.Generic;
using System.IO;
using Systems;
using ECS;
using FlatBuffers;
using Service.TaskService2;
using UnityEngine;
using Zenject;

namespace CreativeZone
{
    public class CreativeTaskService : CreativeService<CreativeTaskService, ITaskService>
    {
        public int Serialize(FlatBufferBuilder builder)
        {
            return this.Vanilla.Serialize(builder);
        }

        public void Deserialize(object incoming)
        {
            this.Vanilla.Deserialize(incoming);
        }

        public void Deserialize(ByteBuffer buf)
        {
            this.Vanilla.Deserialize(buf);
        }

        public void Initialize()
        {
            this.Vanilla.Initialize();
        }

        public T CreateGlobalTask<T>(Vector3 position, UID target, SettlerDataComponent.Profession profession = SettlerDataComponent.Profession.Worker, TaskPriority priority = TaskPriority.Default) where T : ITask
        {
            return this.Vanilla.CreateGlobalTask<T>(position, target, profession, priority);
        }

        public T CreateBuildingTask<T>(UID owningBuilding, Vector3 position, UID target, SettlerDataComponent.Profession profession, TaskPriority priority = TaskPriority.Default) where T : ITask
        {
            return this.Vanilla.CreateBuildingTask<T>(owningBuilding, position, target, profession, priority);
        }

        public void BuildingDestroyed(UID building)
        {
            this.Vanilla.BuildingDestroyed(building);
        }

        public T CreateAndSetFollowupTask<T>(ITask task, Vector3 position, UID target) where T : ITask
        {
            return this.Vanilla.CreateAndSetFollowupTask<T>(task, position, target);
        }

        public void AssignTasksToIdleSettlers(Dictionary<int, List<SettlerTaskAssignmentSystemComponents>> idleSettlers)
        {
            this.Vanilla.AssignTasksToIdleSettlers(idleSettlers);
        }

        public ITask _KillTask(ITask task, bool killFollowupTasks)
        {
            return this.Vanilla._KillTask(task, killFollowupTasks);
        }

        public bool _TakeTask(ITask task, UID settler)
        {
            return this.Vanilla._TakeTask(task, settler);
        }

        public List<ITask> GetAllTasks(Type taskType)
        {
            return this.Vanilla.GetAllTasks(taskType);
        }

        public void Clear()
        {
            this.Vanilla.Clear();
        }

        public void ChangeBuildingTaskPriority(UID buildingUID, TaskPriority newPriority)
        {
            this.Vanilla.ChangeBuildingTaskPriority(buildingUID, newPriority);
        }

        public void User_ChangeBuildingTaskPriority(UID buildingUID, TaskPriority newPriority)
        {
            this.Vanilla.User_ChangeBuildingTaskPriority(buildingUID, newPriority);
        }

        public UID? GetLastPrioritizedBuilding()
        {
            return this.Vanilla.GetLastPrioritizedBuilding();
        }

        public bool IsLastPrioritizedBuilding(UID uid)
        {
            return this.Vanilla.IsLastPrioritizedBuilding(uid);
        }

        public void ClearLastPrioritizedBuilding()
        {
            this.Vanilla.ClearLastPrioritizedBuilding();
        }

        public int GetTotalAmountBuildingTaskForBuilding(UID buildingUID)
        {
            return this.Vanilla.GetTotalAmountBuildingTaskForBuilding(buildingUID);
        }

        public void KillAllTasksForBuilding(UID buildingUID)
        {
            this.Vanilla.KillAllTasksForBuilding(buildingUID);
        }
    }
}
