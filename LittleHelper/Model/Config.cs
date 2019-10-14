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
        /// <summary>
        /// API Key
        /// </summary>
        public string BaiduAPIKey { get; set; }

        /// <summary>
        /// Secret Key
        /// </summary>
        public string BaiduSecretKey { get; set; }

        /// <summary>
        /// 语音语速
        /// </summary>
        public int VoiceSpeed { get; set; }
        /// <summary>
        /// 语音音量
        /// </summary>
        public int VoiceVolume { get; set; }
        /// <summary>
        /// 语音人
        /// </summary>
        public int VoicePersonInt { get; set; }
        /// <summary>
        /// 句子重复间隔
        /// </summary>
        public int RepeatInterval { get; set; }
    }
}
