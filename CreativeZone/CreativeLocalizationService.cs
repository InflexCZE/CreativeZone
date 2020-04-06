using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreativeZone.Utils;
using FlatBuffers;
using Service.Localization;
using TMPro;

namespace CreativeZone
{
    public class CreativeLocalizationService : CreativeService<CreativeLocalizationService, ILocalizationService>
    {
        [HarmonyReplace]
        public void Localize(Keys locaKey, TextMeshProUGUI textOutput, Dictionary<string, string> replacements = null, ReplacementStyle replacementStyle = null)
        {
            this.Vanilla.Localize(locaKey, textOutput, replacements, replacementStyle);

            if
            (
                locaKey == Keys.Common_ClosedBetaDisclaimer || 
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_Watermarks_EarlyAccess
            )
            {
                var text = textOutput.text;
                text += "[Creative Zone]";
                textOutput.text = text;
            }
        }

        [HarmonyReplace]
        public string GetLocalization(Keys locaKey, Dictionary<string, string> replacements = null)
        {
            var text = this.Vanilla.GetLocalization(locaKey, replacements);
            
            if
            (
                locaKey == Keys.Common_ClosedBetaDisclaimer || 
                locaKey == Keys.Common_Watermarks_ClosedBeta ||
                locaKey == Keys.Common_EarlyAccessDisclaimer
            )
            {
                text += "[Creative Zone]";
            }

            return text;
        }
    }
}
