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
            if(!File.Exists(FileName))
            {
                SaveConfig();
            }

            using (var sr = new StreamReader(FileName))
            {
                string jstr = sr.ReadToEnd();
                Config = JsonConvert.DeserializeObject<Config>(jstr);
            }
        }

        public static void SaveConfig()
        {
            using (var sw = new StreamWriter(FileName))
            {
                var jstr = JsonConvert.SerializeObject(Config);
                sw.Write(jstr);
            }
        }
    }
}
