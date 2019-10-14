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
