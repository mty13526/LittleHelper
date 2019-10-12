using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Media;
using Baidu.Aip.Speech;
using System.IO;
using System.Threading;

namespace LittleHelper
{
    /// <summary>
    /// PlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PlayerWindow : Window
    {
        private SoundPlayer soundPlayer = new SoundPlayer();
        private Tts baiduTts;
        private int npos = 0;
        public int SleepInterval;
        public bool ResetVoice { get; set; }

        private object lk = new object();

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);

        private Dictionary<int, byte[]> preloadVoice = new Dictionary<int, byte[]>();
        private Dictionary<string, object> baiduOption = new Dictionary<string, object>()
        {
            { "spd", ConfigManager.Config.VoiceSpeed },
            { "vol", ConfigManager.Config.VoiceVolume },
            { "per", ConfigManager.Config.VoicePersonInt },
            { "aue", 6 } // 使用wav格式
        };

        private Task repeatTimerTask;
        private static async void repeatTimer(object state)
        {
            var s = (object[])state;
            var window = (PlayerWindow)s[0];
            if (window.patterns == null || window.patterns.Length == 0) return;

            var interval = window.SleepInterval;

            var token = (CancellationToken)s[1];
            var pauseEvent = (ManualResetEvent)s[2];

            if (window.SleepInterval == Timeout.Infinite)
            {
                return;
            }

            while (true)
            {

                pauseEvent.WaitOne();
                window.PlayCurrentSound();

                if (!window.ResetVoice)
                {
                    Thread.Sleep(interval);
                }


                window.ResetVoice = false;

                if (window?.Visibility != Visibility.Visible) return;
                if (token.IsCancellationRequested) return;
            }

        }

        public int Pos
        {
            get
            {
                return npos;
            }

            set
            {
                if (value > this.MaxPos || value < 0) return;
                npos = value;
                Update();
            }
        }

        public int MaxPos
        {
            get => this.patterns.Length - 1;
        }

        private string[] patterns;

        private string text;
        public string Text
        {
            get => this.text;
            set
            {
                this.text = value;
                patterns = LittleHelper.Text.Split(value);

                if (patterns.Length != 0)
                    this.Pos = 0;
                else
                    this.Pos = -1;
            }
        }

        private bool paused = false;
        public bool Paused
        {
            get
            {
                return this.paused;
            }
            set
            {
                this.paused = value;
                Update();
            }
        }

        public void Update()
        {
            this.Dispatcher?.Invoke(() =>
            {
                if(this.patterns.Length != 0) this.TextCurrent.Text = patterns[this.Pos];
                this.ButtonPause.Content = this.Paused ? "继续" : "暂停";
                this.ButtonNext.IsEnabled = this.ButtonPrevious.IsEnabled = !this.Paused;
            });
        }

        public PlayerWindow()
        {
            InitializeComponent();

            this.baiduTts = new Tts(ConfigManager.Config.BaiduAPIKey, ConfigManager.Config.BaiduSecretKey);
            this.baiduTts.Timeout = 10000;

            this.SleepInterval = ConfigManager.Config.RepeatInterval * 1000;
            if (this.SleepInterval <= 0) this.SleepInterval = Timeout.Infinite;
            this.ResetRepeatTimer();

        }

        public void ShowWithText(string text)
        {
            this.Text = text;
            this.ResetRepeatTimer();
            base.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var cpos = this.Pos;
            UnLoadVoice(cpos);
            ++this.Pos;
            if (this.Pos != cpos)
            {
                this.ResetRepeatTimer();
                //Task.Run(() =>
                //{
                //    PreloadVoice(this.Pos + 1); // 预加载下一句，确保流畅
                //});
            }   
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var cpos = this.Pos;
            --this.Pos;
            if (this.Pos != cpos)
            {
                this.ResetRepeatTimer();
                Task.Run(() => this.PlayCurrentSound());
            }
                
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            this.Paused = !this.Paused;
            if (this.paused) pauseEvent.Reset();
            else pauseEvent.Set();
        }

        /// <summary>
        /// 从百度API获得合成后的语音
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="data">合成后的语音</param>
        /// <returns>错误消息，如果没有为空</returns>
        private string GetVoice(string text, out byte[] data)
        {
            try
            {
                var result = baiduTts.Synthesis(text, baiduOption);
                if (result.ErrorCode == 0)
                {
                    data = result.Data;
                    return string.Empty;
                }

                data = null;
                return result.ErrorMsg;
            } catch(Exception e)
            {
                data = null;
                return e.Message;
            }

        }

        private void PlayCurrentSound()
        {
            PlaySound(this.Pos);
        }

        private void PlaySound(int n)
        {
            if (n > this.MaxPos || n < 0) return;

            byte[] value = null;
            bool preloaded;

            try
            {
                Monitor.Enter(this.lk);
                preloaded = this.preloadVoice.TryGetValue(n, out value);
            } finally
            {
                Monitor.Exit(this.lk);
            }


            if(!preloaded)
            {
                var errmsg = GetVoice(this.patterns[n], out value);
                if (value == null) throw new Exception("无法加载声音: " + errmsg);
                
                if (this.preloadVoice.ContainsKey(n))
                    this.preloadVoice.Add(n, value); // 省得加载第二遍
            }

            soundPlayer.Stream = new MemoryStream(value);
            soundPlayer.PlaySync();
        }

        private void PreloadVoice(int n)
        {
            if (this.preloadVoice.ContainsKey(n)) return;

            try
            {
                Monitor.Enter(this.lk);
                if (n > this.MaxPos || n < 0) return;
                var text = patterns[n];

                byte[] data;
                var errmsg = GetVoice(this.patterns[n], out data);
                if (data == null) throw new Exception(errmsg);

                this.preloadVoice.Add(n, data);
            }
            finally
            {
                Monitor.Exit(this.lk);
            }

        }

        private void UnLoadVoice(int n)
        {
            this.preloadVoice.Remove(n);
        }

        private void ResetRepeatTimer()
        {
            if (this.repeatTimerTask == null || this.repeatTimerTask.IsCompleted)
            {
                this.repeatTimerTask = new Task(repeatTimer, new object[] { this, tokenSource.Token, pauseEvent });
                this.repeatTimerTask.Start();
            }
            else
            {
                this.ResetVoice = true;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.tokenSource.Cancel();
        }
    }
}
