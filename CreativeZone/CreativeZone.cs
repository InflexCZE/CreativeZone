using System;
using PatchZone.Hatch;
using PatchZone.Hatch.Utils;
using CreativeZone.Services;

using ECS;
using Service.Achievement;
using Service.Building;
using Service.Localization;
using Service.Street;
using Service.UserWorldTasks;

namespace CreativeZone
{
    public class CreativeZone : Singleton<CreativeZone>, IPatchZoneMod
    {
        public IPatchZoneContext Context { get; private set; }

        public void Init(IPatchZoneContext context)
        {
            this.Context = context;
        }

        public void OnBeforeGameStart()
        {
            this.Context.RegisterProxyService<IEntityManager,         CreativeEntityManager>();
            this.Context.RegisterProxyService<IStreetService,         CreativeStreetService>();
            this.Context.RegisterProxyService<IBuildingService,       CreativeBuildingService>();
            this.Context.RegisterProxyService<IAchievementService,    CreativeAchievementService>();
            this.Context.RegisterProxyService<ILocalizationService,   CreativeLocalizationService>();
            this.Context.RegisterProxyService<IUserWorldTasksService, CreativeUserWorldTaskService>();
        }
    }
}
