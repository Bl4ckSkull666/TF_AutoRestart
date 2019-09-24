using ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AutoRestart.Classes
{
    public class AutoRestartConfig
    {
        private Dictionary<string, string> _config = new Dictionary<string, string>();
        private Dictionary<int, string> _countdown = new Dictionary<int, string>();
        private List<string> _times = new List<string>();

        private string _configPath;
        private string _configFile;

        private DateTime LastUpdate;

        public AutoRestartConfig()
        {
            _configPath = $"Mods{Path.DirectorySeparatorChar}AutoRestart";
            _configFile = $"{Path.DirectorySeparatorChar}Config.xml";
            Load();
        }

        private void Load()
        { //Load Config from file
            _config.Clear();
            if (!File.Exists(_configPath + _configFile))
            {
                try
                {
                    if (!Directory.Exists(_configPath))
                        Directory.CreateDirectory(_configPath);

                    File.WriteAllText(_configPath + _configFile, AutoRestart.Properties.Resources.AutoRestartDefaultConfig);
                    ModAPI.Log.Write("Saved default configuration for AutoRestart Mod");
                }
                catch (IOException ex)
                {
                    ModAPI.Log.Write("Failed to save default configuration for Discord Webhook Info Mod");
                    ModAPI.Log.Write($"Error: {ex.Message}");
                }
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(_configPath + _configFile);
                XmlElement root = doc.DocumentElement;
                foreach (XmlNode node in root.SelectNodes("/Configuration/*"))
                {
                    if (node.Attributes["Name"] != null && node.Attributes["Value"] != null)
                    {
                        string name = node.Attributes["Name"].Value;
                        string val = node.Attributes["Value"].Value;
                        if (name.Contains("countdown"))
                        {
                            string[] str = val.Split(':');
                            int t;
                            if (str.Length != 2 || !int.TryParse(str[0], out t))
                                continue;

                            _countdown.Add(t, str[1]);
                        }
                        else if(name.Contains("times"))
                        {
                            string[] str = val.Split(',');
                            foreach (string str1 in str)
                            {
                                string[] ts = str1.Split(':');
                                int h, m;
                                if (ts.Length != 2 || !int.TryParse(ts[0], out h) || !int.TryParse(ts[1], out m))
                                    continue;

                                if (_times.Contains(str1))
                                    continue;

                                _times.Add(str1);
                            }
                        }
                        else
                        {
                            _config.Add(name.ToLower(), val);
                        }
                    }
                }
                LastUpdate = File.GetLastWriteTime(_configPath + _configFile);
            }
            catch (Exception ex)
            {
                ModAPI.Log.Write("Error on load Config file.");
                ModAPI.Log.Write("Error: " + ex.Message);
            }
        }

        public string getString(string key)
        {
            if (LastUpdate < File.GetLastWriteTime(_configPath + _configFile))
            {
                AutoRestartInit.Reload();
                return "null";
            }

            if (!_config.ContainsKey(key))
                return "null";
            return _config[key];
        }

        public int getInt(string key)
        {
            if (LastUpdate < File.GetLastWriteTime(_configPath + _configFile))
            {
                AutoRestartInit.Reload();
                return -1;
            }

            if (!_config.ContainsKey(key))
                return -1;

            int back = -1;
            if (!int.TryParse(_config[key], out back))
                return -1;

            if (back < 0)
                return -1;
            return back;
        }

        public bool getBool(string key)
        {
            if (LastUpdate < File.GetLastWriteTime(_configPath + _configFile))
            {
                AutoRestartInit.Reload();
                return false;
            }

            if (!_config.ContainsKey(key))
                return false;

            bool back;
            if (!bool.TryParse(_config[key], out back))
                return false;

            return back;
        }

        public string getCountdownMessage(int num)
        {
            if (_countdown.ContainsKey(num))
                return _countdown[num];
            return String.Empty;
        }

        public int getHighestCountdown()
        {
            int t = 0;
            foreach(int i in _countdown.Keys.ToList())
            {
                if (t == 0 || i > t)
                    t = i;
            }
            return t;
        }

        public int getNextCountdown(int num)
        {
            List<int> tmp = _countdown.Keys.ToList();
            tmp.Sort();
            tmp.Reverse();
            foreach(int i in tmp)
            {
                if (i < num)
                    return i;
            }
            return 0;
        }

        public List<string> getTimes()
        {
            return _times;
        }
    }
}