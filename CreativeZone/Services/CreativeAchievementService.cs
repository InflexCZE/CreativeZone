using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using Service.Achievement;

namespace CreativeZone.Services
{
    /// <summary>
    /// Disable achievement progress so ppl are not tempted to cheat with creative enabled
    /// </summary>
    public class CreativeAchievementService : ProxyService<CreativeAchievementService, IAchievementService>
    {
        [LogicProxy]
        public void Unlock(Service.Achievement.Achievement achievement)
        { }

        [LogicProxy]
        public void UnlockByIdentifier(string identifier)
        { }

        [LogicProxy]
        public void Progress(Service.Achievement.Achievement achievement, int progress = 1)
        { }

        [LogicProxy]
        public void ProgressByIdentifier(string identifier, int progress = 1)
        { }

        [LogicProxy]
        public void Tick(float deltaTime)
        { }

        [LogicProxy]
        public void ActiveSessionTick(float deltaTime)
        { }
    }
}
