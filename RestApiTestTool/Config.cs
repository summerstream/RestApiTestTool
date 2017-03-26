using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;

namespace RestApiTestTool
{
    class ConfigManager
    {
        private static string _filename = "config.json";
        private static string GetSettingsFromFile(string filename="config.json")
        { 
            string result = string.Empty;
            string path = AppDomain.CurrentDomain.BaseDirectory + filename;
            using(StreamReader sr=new StreamReader(File.OpenRead(path))){
                result=sr.ReadToEnd();
                sr.Close();
            }
            return result;
        }
        private static void SaveSettingsToFile(string str)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + _filename;
            //using (StreamWriter sw = new StreamWriter(File.OpenWrite(path)))
            //{
            //    sw.Write(str);
            //    sw.write
            //    sw.Close();
            //}
            File.WriteAllText(path,str);
        }
        public static void SaveCollection(Collection c)
        {
            Config config = GetConfig();
            if(config.collections.Contains(c)){
                config.collections.Remove(c);
            }
            config.collections.Add(c);
            SaveConfig(config);
        }
        public static void DeleteCollection(Collection c)
        {
            Config config = GetConfig();
            config.collections.Remove(c);
            SaveConfig(config);
        }
        public static Config GetConfig()
        {
            string json = GetSettingsFromFile();
            JavaScriptSerializer serializer=new JavaScriptSerializer();
            return serializer.Deserialize<Config>(json);
        }
        public static void SaveConfig(Config config)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            SaveSettingsToFile(serializer.Serialize(config));
        }
    }
    class Collection:System.Object
    {
        public string Url { get; set; }
        public string Data { get; set; }
        public string Type { get; set; }
        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Collection p = obj as Collection;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            if(this.Url==p.Url){
                return true;
            }
            return false;
        }
    }
    class Config
    {
        public List<Collection> collections { get; set; }
    }
}
