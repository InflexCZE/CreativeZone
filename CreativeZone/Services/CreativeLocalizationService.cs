using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreativeZone.Utils;
using FlatBuffers;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using Service.Localization;
using TMPro;

namespace CreativeZone.Services
{
    public class CreativeLocalizationService : ProxyService<CreativeLocalizationService, ILocalizationService>
    {
        [LogicProxy]
        public void Localize(Keys locaKey, TextMeshProUGUI textOutput, Dictionary<string, string> replacements, ReplacementStyle replacementStyle)
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

        [LogicProxy]
        public string GetLocalization(Keys locaKey, Dictionary<string, string> replacements, ReplacementStyle replacementStyle)
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
