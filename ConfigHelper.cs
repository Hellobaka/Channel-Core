using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channel_Core
{
    public static class ConfigHelper
    {
        public const string ConfigPath = "Config\\Config.json";
        public static object ReadConfig(string section)
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    JObject config = JObject.Parse(File.ReadAllText(ConfigPath));
                    if (config.ContainsKey(section))
                    {
                        return config[section].ToObject<object>();
                    }
                    else
                    {
                        config.Add(section, "");
                        return "";
                    }
                }
                catch(Exception ex)
                {
                    Helper.OutError(ex.Message);
                    throw;
                }
            }
            else
            {
                Directory.CreateDirectory(new FileInfo(ConfigPath).DirectoryName);
                File.WriteAllText(ConfigPath, "{\"AppID\": \"\",\"AppKey\": \"\",\"Token\": \"\"}");
                Helper.OutError("配置文件为空");
                return "";
            }
        }
        public static void WriteConfig(string section, object content)
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    JObject config = JObject.Parse(File.ReadAllText(ConfigPath));
                    if (config.ContainsKey(section))
                    {
                        config[section] = new JObject(content);
                    }
                    else
                    {
                        config.Add(section, JsonConvert.SerializeObject(content));
                    }
                    File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config));
                }
                catch (Exception ex)
                {
                    Helper.OutError(ex.Message);
                    throw;
                }
            }
            else
            {
                Directory.CreateDirectory(ConfigPath);
                throw new FileNotFoundException(ConfigPath);
            }
        }
    }
}
