using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LeagueSharp.Loader.Class
{
    public enum BeatState
    {
        Ping1,
        Ping2,
        Dead
    }

    public class BotAccountManager : ObservableCollection<BotAccount>
    {
        private string[] _StringSeparators = new string[] { "|" };
        private string _LauncherPath = @"C:\Riot Games\League of Legends\";
        private Dictionary<string, DateTime?> _HeartbeatMonitor = new Dictionary<string, DateTime?>();
        private Timer _MyTimer;
        private Thread _DeadBotThread;

        public BotAccountManager() :
            base()
        {
            _DeadBotThread = new Thread(
                () =>
                {
                    while (true)
                    {
                        foreach (BotAccount bot in this)
                        {
                            if(bot.IsRunning && !bot.HasUpdatedStatusRecently())
                            {
                                bot.IsRunning = false;

                                Thread.Sleep(5* 1000);

                                bot.IsRunning = true;
                            }
                        }

                        Thread.Sleep(60*1000);
                    }
                });



            LoadBots();

            //_DeadBotThread.Start();
            //_MyTimer = new Timer(HeartbeatCallback, null, 5 * 1000, 5 * 1000);
        }
       

        private void HeartbeatCallback(Object state)
        {
            foreach(BotAccount acc in this)
            {
                if (!acc.Bot.IsGameLaunchedHandled())
                {
                    acc.Bot.GameLaunched += Bot_GameLaunched;
                }

                if(!_HeartbeatMonitor.Keys.Contains(acc.UserName))
                {
                    _HeartbeatMonitor.Add(acc.UserName, DateTime.Now);
                }
            }

            foreach(string userName in _HeartbeatMonitor.Keys)
            {
                if(this.Where(acc => acc.UserName == userName).FirstOrDefault() == null)
                {
                    _HeartbeatMonitor.Remove(userName);
                }
            }

            foreach (string userName in this.Select(bot => bot.UserName))
            {
                BotAccount acc = this.Where(bot => bot.UserName == userName).FirstOrDefault();
                if (acc != null &&
                    acc.CurrentState == BotState.InGame &&
                    DateTime.Now.Subtract(_HeartbeatMonitor[userName].Value).TotalSeconds > 15)
                {
                    acc.RebootGame();
                    _HeartbeatMonitor[userName] = DateTime.Now;
                }
            }
        }

        void Bot_GameLaunched(RitoBot.GameLaunchedEventArgs e)
        {
            _HeartbeatMonitor[e.UserName] = DateTime.Now;
        }

        public void Add(string userName, string password)
        {
            BotAccount acc = new BotAccount(userName, password, _LauncherPath);
            acc.Bot.GameLaunched += Bot_GameLaunched;
            this.Add(acc);
            _HeartbeatMonitor.Add(userName, DateTime.Now);
        }

        public void LoadBots()
        {
            this.Clear();

            TextReader tr = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "accounts.txt");
            string line;
            while ((line = tr.ReadLine()) != null)
            {
                string[] results = line.Split(_StringSeparators, StringSplitOptions.None);
                this.Add(new BotAccount(results[0], results[1], _LauncherPath));
            }
            tr.Close();
        }

        public void SaveBots()
        {
            using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "accounts.txt", false))
            {
                foreach (BotAccount botAccount in this)
                {
                    writer.WriteLine(botAccount.UserName + _StringSeparators[0] + botAccount.Password);
                }
            }
        }

        public void Heartbeat(string userName, DateTime beatTime)
        {
            BotAccount checkBot = this.Where(bot => bot.UserName == userName).FirstOrDefault();

            if (checkBot != null)
            {
                _HeartbeatMonitor[userName] = beatTime;
            }
        }
    }
}
