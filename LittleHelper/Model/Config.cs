using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LittleHelper.Model
{
    class Config
    {
        public enum VoicePersonType
        {

        }

        public string BaiduAPIKey { get; set; } // API Key

        public string BaiduSecretKey { get; set; } // Secret Key

        public int VoiceSpeed { get; set; } // 语音语速
        public int VoiceVolume { get; set; } // 语音音量
        public VoicePersonType VoicePerson { get; set; } // 语音发音人

        public int RepeatInterval { get; set; } // 句子重复间隔
    }
}
