using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LittleHelper.Model;
using System.IO;
using Newtonsoft.Json;

namespace LittleHelper
{
    class ConfigManager
    {
        public static Config Config = new Config();

        private const string FileName = "config.json";

        public static void ReadConfig()
        {
            if(!File.Exists(FileName)) // 如果配置文件不存在，就创建一个默认配置文件
            {
                Config.BaiduAPIKey = "wjZQdBsgnqjaRisL7Ufo01bO";
                Config.BaiduSecretKey = "kYdmEvCDkgKG0ngPzO74WuxNGbm4Eyy6";
                Config.VoiceSpeed = 5;          //默认语速：5
                Config.VoiceVolume = 5;         //默认音量：5
                Config.VoicePersonInt = 3;      //默认发音人：度逍遥
                Config.RepeatInterval = 2;      //默认间隔：2s
                SaveConfig();
            }

            using (var sr = new StreamReader(FileName))
            {
                string jstr = sr.ReadToEnd();
                Config = JsonConvert.DeserializeObject<Config>(jstr); // 反序列化到配置对象
            }
        }

        public static void SaveConfig()
        {
            using (var sw = new StreamWriter(FileName))
            {
                var jstr = JsonConvert.SerializeObject(Config); // 序列化并写到文件
                sw.Write(jstr);
            }
        }
    }
}
