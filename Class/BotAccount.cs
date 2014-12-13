using LoLLauncher;
using RitoBot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LeagueSharp.Loader.Class
{
    public enum BotState
    {
        Queue,
        ChampSelect,
        PostGame,
        InGame,
        Unknown
    }

    // Testing 123456
    public class BotAccount : INotifyPropertyChanged
    {
        private bool _IsRunning = false;
        private string _UserName = string.Empty;
        private string _Password = string.Empty;
        private string _Level = "?";
        private BotState _CurrentState = BotState.Unknown;
        private string _Status = string.Empty;
        private VoliBot _Bot = null;
        private static Dictionary<string, BotState> _StatusConverter;
        private string _StartingExperience = "?";
        private string _CurrentExperience = "?";
        private DateTime? _StartDate;
        private DateTime _LastUpdate;


        public event PropertyChangedEventHandler PropertyChanged;

        public static Dictionary<string, BotState> StatusConverter
        {
            get
            {
                if (_StatusConverter == null)
                {
                    _StatusConverter = new Dictionary<string, BotState>();
                    _StatusConverter.Add("playing", BotState.InGame);
                    _StatusConverter.Add("queue", BotState.Queue);
                    _StatusConverter.Add("game", BotState.InGame);
                }

                return _StatusConverter;
            }
        }

        public VoliBot Bot
        {
            get
            {
                return _Bot;
            }
        }

        public string Status
        {
            get
            {
                return _Status;
            }
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    CurrentState = ConvertStatusToState();
                    OnPropertyChanged("Status");
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                return _IsRunning;
            }
            set
            {
                if (_IsRunning != value)
                {
                    _IsRunning = value;

                    if (_IsRunning)
                    {
                        _StartDate = DateTime.Now;
                        _LastUpdate = DateTime.Now;
                        OnPropertyChanged("StartDate");
                        _Bot.Start();
                    }
                    else
                    {
                        RebootGame();
                    }

                    OnPropertyChanged("IsRunning");
                }
            }
        }

        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                if (_UserName != value)
                {
                    _UserName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                if (_Password != value)
                {
                    _Password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        public string Level
        {
            get
            {
                return _Level;
            }
            set
            {
                if (_Level != value)
                {
                    _Level = value;
                    OnPropertyChanged("Level");
                }
            }
        }

        public string StartingExperience
        {
            get
            {
                return _StartingExperience;
            }
            set
            {
                if(_StartingExperience != value)
                {
                    _StartingExperience = value;
                    OnPropertyChanged("StartingExperience");
                }
            }
        }

        public string CurrentExperience
        {
            get
            {
                return _CurrentExperience;
            }
            set
            {
                if(_CurrentExperience != value)
                {
                    _CurrentExperience = value;
                    OnPropertyChanged("CurrentExperience");
                    OnPropertyChanged("ExpPerHour");
                }
            }
        }

        public string ExpPerHour
        {
            get
            {
                if(_StartDate == null)
                {
                    return "NA";
                }
                else if(_StartDate.Value.Subtract(DateTime.Now).TotalHours == 0)
                {
                    return "NA";
                }
                else
                {
                    double val1 = double.Parse(_CurrentExperience);
                    double val2 = double.Parse(_StartingExperience);
                    double val3 = (double)_StartDate.Value.Subtract(DateTime.Now).TotalHours;
                    return ((val1 - val2) / val3).ToString();
                }
            }
        }

        public BotState CurrentState
        {
            get
            {
                return _CurrentState;
            }
            set
            {
                if (_CurrentState != value)
                {
                    _CurrentState = value;
                    OnPropertyChanged("CurrentState");
                    OnPropertyChanged("ExpPerHour");
                }
            }

        }

        public string StartDate
        {
            get
            {
                return (_StartDate == null) ? string.Empty : _StartDate.ToString();
            }
        }

        public void RebootGame()
        {
            _Bot.Stop();
            _Bot = new VoliBot(UserName, Password, "NA", @"C:\Riot Games\League of Legends\");
            _Bot.UpdateChanged += _Bot_UpdateChanged;
            _Bot.LevelChanged += _Bot_LevelChanged;
            _Bot.ExperienceLaunched += _Bot_ExperienceLaunched;
        }

        void _Bot_ExperienceLaunched(ExperienceLaunchedEventArgs e)
        {
            if(StartingExperience == "?")
            {
                StartingExperience = e.Experience.ToString();
            }

            CurrentExperience = e.Experience.ToString();
        }

        public BotAccount(string userName, string password, string launcherPath)
        {
            UserName = userName;
            Password = password;
            _Bot = new VoliBot(UserName, Password, "NA", launcherPath);
            _Bot.UpdateChanged += _Bot_UpdateChanged;
            _Bot.LevelChanged += _Bot_LevelChanged;
            _Bot.ExperienceLaunched += _Bot_ExperienceLaunched;

        }

        void _Bot_LevelChanged(LevelChangedEventArgs e)
        {
            Level = e.Level.ToString();
        }

        void _Bot_UpdateChanged(UpdateChangedEventArgs e)
        {
            Status = e.Update;
            _LastUpdate = DateTime.Now;
        }

        public bool HasUpdatedStatusRecently()
        {
            if(Status.Contains("error"))
            {
                return true;
            }

            return DateTime.Now.Subtract(_LastUpdate).TotalMinutes >= 25;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private BotState ConvertStatusToState()
        {
            string stateKey = StatusConverter.Keys.Where(key => Status.ToLower().Contains(key)).FirstOrDefault();
            return (stateKey != null) ?
                StatusConverter[stateKey] : BotState.Unknown;
        }
    }
}
