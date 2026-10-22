using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace BreatheZ.Ui.Util
{
    public static class Talker
    {
        public static SpeechSynthesizer GetSpeechSynthesizer()
        {
            var reader = new SpeechSynthesizer()
            {
                Volume = 100,
                Rate = -2,
            };

            var voice = reader
                .GetInstalledVoices()
                .FirstOrDefault(v => v.VoiceInfo.Name == "Microsoft Zira Desktop");

            if (voice != null)
            {
                reader.SelectVoice(voice.VoiceInfo.Name);
            }

            return reader;
        }

    }
}
