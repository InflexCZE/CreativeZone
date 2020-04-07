using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Achievement;

namespace CreativeZone
{
    /// <summary>
    /// Disable achievement progress so ppl are not tempted to cheat with creative enabled
    /// </summary>
    public class CreativeAchievementService : CreativeService<CreativeAchievementService, IAchievementService>
    {
        [HarmonyReplace]
        public void Unlock(Service.Achievement.Achievement achievement)
        { }

        [HarmonyReplace]
        public void UnlockByIdentifier(string identifier)
        { }

        [HarmonyReplace]
        public void Progress(Service.Achievement.Achievement achievement, int progress = 1)
        { }

        [HarmonyReplace]
        public void ProgressByIdentifier(string identifier, int progress = 1)
        { }

        [HarmonyReplace]
        public void Tick(float deltaTime)
        { }

        [HarmonyReplace]
        public void ActiveSessionTick(float deltaTime)
        { }
    }
}
