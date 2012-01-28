using System;
using System.IO;
using Newtonsoft.Json;

namespace HousingDistricts
{
    public class HConfigFile
    {
        public bool NotifyOnEntry = true;
        public string NotifyOnEntry_description = "Global setting: Notifies the owner of the house and the user who entered the house when the user enters.";
        public bool NotifyOnExit = true;
        public string NotifyOnExit_description = "Global setting: Notifies the owner of the house and the user who exited the house when the user exits.";
        public bool HouseChatEnabled = true;
        public string HouseChatEnabled_description = "Global setting: False completely disables house chat.";
        public int MaxHouseSize = 5000;
        public string MaxHouseSize_description = "Not yet implemented";
        public int MaxHousesByUsername = 3;
        public string MaxHousesByUsername_description = "Not yet implemented";
        public int MaxHousesByIP = 3;
        public string MaxHousesByIP_description = "Not yet implemented";

        public static HConfigFile Read(string path)
        {
            if (!File.Exists(path))
                return new HConfigFile();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static HConfigFile Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<HConfigFile>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<HConfigFile> ConfigRead;
    }
}