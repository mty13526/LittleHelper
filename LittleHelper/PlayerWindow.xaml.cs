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

        private CancellationTokenSource tokenSource;
        private ManualResetEvent resetEvent;

        private Dictionary<int, byte[]> preloadVoice = new Dictionary<int, byte[]>();
        private Dictionary<string, object> baiduOption = new Dictionary<string, object>()
        {
            { "spd", ConfigManager.Config.VoiceSpeed },
            { "vol", ConfigManager.Config.VoiceVolume },
            { "per", (int)ConfigManager.Config.VoicePerson },
            { "aue", 6 } // 使用wav格式
        };

        private Task repeatTimerTask;
        private static async void repeatTimer(object state)
        {
            var s = (object[])state;
            var window = (PlayerWindow)s[0];
            if (window.patterns.Length == 0) return;

            var token = (CancellationToken)s[1];
            var resetEvent = (ManualResetEvent)s[2];

            if (window.SleepInterval == Timeout.Infinite)
            {
                window.PlayCurrentSound();
                return;
            }

            while (true)
            {
                if (token.IsCancellationRequested) return;

                window.PlayCurrentSound();

                resetEvent.WaitOne();

                await Task.Delay(window.SleepInterval);

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
                patterns = LittleHelper.Text.Split(value).Select(i => i.Trim()).Where(i => i != string.Empty).ToArray();

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
            this.baiduTts.Timeout = 5000;

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
                Task.Run(() =>
                {
                    this.PlayCurrentSound();
                    PreloadVoice(this.Pos + 1); // 预加载下一句，确保流畅
                });
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
            var preloaded = this.preloadVoice.TryGetValue(n, out value);
            if(!preloaded)
            {
                var errmsg = GetVoice(this.patterns[n], out value);
                if (value == null) throw new Exception("无法加载声音: " + errmsg);
            }

            soundPlayer.Stream = new MemoryStream(value);
            soundPlayer.PlaySync();
        }

        private void PreloadVoice(int n)
        {
            if (this.preloadVoice.ContainsKey(n)) return;

            if (n > this.MaxPos || n < 0) return;
            var text = patterns[n];

            byte[] data;
            var errmsg = GetVoice(this.patterns[n], out data);
            if (data == null) throw new Exception(errmsg);

            this.preloadVoice.Add(n, data);
        }

        private void UnLoadVoice(int n)
        {
            this.preloadVoice.Remove(n);
        }

        private void ResetRepeatTimer()
        {
            if(this.tokenSource != null) this.tokenSource.Cancel(); // 取消掉上个播放线程

            this.tokenSource = new CancellationTokenSource();
            this.resetEvent = new ManualResetEvent(true);

            if (this.repeatTimerTask != null)
            {
                this.repeatTimerTask.ContinueWith((_) =>
                {
                    this.repeatTimerTask = new Task(repeatTimer, new object[] { this, tokenSource.Token, resetEvent });
                    repeatTimerTask.Start();
                });

            } else
            {
                this.repeatTimerTask = new Task(repeatTimer, new object[] { this, tokenSource.Token, resetEvent });
                this.repeatTimerTask.Start();
            }


        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.tokenSource.Cancel();
        }
    }
}
