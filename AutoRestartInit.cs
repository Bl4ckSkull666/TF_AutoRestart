using AutoRestart.Classes;
using Bolt;
using ModAPI.Attributes;
using System;
using System.Threading;
using TheForest.TaskSystem;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutoRestart
{
    public class AutoRestartInit
    {
        private AutoRestartConfig _conf;
        private Timer _timer = null;
        private int _nextStop = 0;

        private static AutoRestartInit _instance = null;

        [ExecuteOnGameStart]
        private static void Load()
        {
            if(_instance == null)
                Reload();
        }

        public static void Reload()
        {
            if(_instance != null)
            {
                _instance._timer.Dispose();
                _instance = null;
            }
            _instance = new AutoRestartInit();
        }

        public AutoRestartInit()
        {
            _conf = new AutoRestartConfig();

            if(!_conf.getString("action").Contains("restart") && !_conf.getString("action").Contains("shutdown"))
            {
                ModAPI.Log.Write("Missing action, must restart or shutdown");
                return;
            }

            DateTime next = getDefaultDateTime();
            foreach (string strTime in _conf.getTimes())
            {
                int h, m;
                string[] str = strTime.Split(':');
                if (str.Length != 2 || !int.TryParse(str[0], out h) || !int.TryParse(str[1], out m))
                    continue;
                
                DateTime isTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, h, m, 0);
                if (DateTime.Now > isTime)
                    isTime = isTime.AddDays(1);
                
                if (next == getDefaultDateTime() || next > isTime)
                {
                    next = isTime;
                }
            }

            if (next == getDefaultDateTime())
            {
                ModAPI.Log.Write("Can't find times.");
                return;
            }

            int timeTillCountDown = (int)(next - DateTime.Now).TotalSeconds;
            _nextStop = timeTillCountDown;
            TimerCallback callBack = new TimerCallback((obj) =>
            {
                CountDownTask();
            });
            _timer = new Timer(callBack, null, 1000, 1000);
        }

        private DateTime getDefaultDateTime()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0);
        }

        public void CountDownTask()
        {
            _nextStop--;

            ModAPI.Log.Write($"nextStop: {_nextStop}");
            
            if(_conf.getCountdownMessage(_nextStop) != String.Empty)
            {
                string msg = _conf.getCountdownMessage(_nextStop);
                if (msg != String.Empty)
                {
                    msg = msg.Replace("%typ", _conf.getString(_conf.getString("action")));
                    ModAPI.Log.Write(msg);
                    SendChatMessage(msg);
                }
            }
            else if(_nextStop == 0)
            {
                BoltLauncher.Shutdown();
                System.Threading.Thread.Sleep(5000);
                CoopSteamServer.Shutdown();
                CoopSteamClient.Shutdown();
                CoopTreeGrid.Clear();
                if (_conf.getString("action").Contains("restart"))
                {
                    System.Threading.Thread.Sleep(5000);
                    SceneManager.LoadScene("SteamDedicatedBootstrapScene", LoadSceneMode.Single);
                }
                else
                {
                    Application.Quit();
                }
            }
        }

        private void SendChatMessage(string message)
        {
            ChatEvent c = ChatEvent.Create(GlobalTargets.AllClients);
            c.Message = message;
            c.Send();
        }
    }
}
