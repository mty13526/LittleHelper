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
        /// <summary>
        /// 声音播放器
        /// </summary>
        private SoundPlayer soundPlayer = new SoundPlayer();

        /// <summary>
        /// 百度接口实例
        /// </summary>
        private Tts baiduTts;
        /// <summary>
        /// 当前句子位置
        /// </summary>
        private int npos = 0;
        /// <summary>
        /// 句子重复间隔
        /// </summary>
        public int SleepInterval;
        /// <summary>
        /// 复读线程重置
        /// </summary>
        public bool ResetVoice { get; set; }
        /// <summary>
        /// 锁
        /// </summary>
        private object lk = new object();

        /// <summary>
        /// 用于控制复读线程的暂停和取消
        /// </summary>
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);

        /// <summary>
        /// 预载的声音数据
        /// </summary>
        private Dictionary<int, byte[]> preloadVoice = new Dictionary<int, byte[]>();
        /// <summary>
        /// tts的选项
        /// </summary>
        private Dictionary<string, object> baiduOption = new Dictionary<string, object>()
        {
            { "spd", ConfigManager.Config.VoiceSpeed }, // 语速
            { "vol", ConfigManager.Config.VoiceVolume }, // 音量
            { "per", ConfigManager.Config.VoicePersonInt }, // 发音人
            { "aue", 6 } // 使用wav格式
        };

        private Task repeatTimerTask;
        
        /// <summary>
        /// 复读线程
        /// </summary>
        /// <param name="state"></param>
        private static void repeatTimer(object state)
        {
            var s = (object[])state;
            var window = (PlayerWindow)s[0];
            if (window.patterns == null || window.patterns.Length == 0) return;

            var interval = window.SleepInterval;

            var token = (CancellationToken)s[1];
            var pauseEvent = (ManualResetEvent)s[2];

            if (window.SleepInterval == Timeout.Infinite) // 如果不重复，就直接退出线程
            {
                return;
            }

            while (true)
            {
                
                pauseEvent.WaitOne(); // 等待暂停
                window.PlayCurrentSound();

                if (!window.ResetVoice) // 未切换到下一句
                {
                    Thread.Sleep(interval);
                }


                window.ResetVoice = false;

                // 当窗口退出时或线程被取消时退出 
                if (window?.Visibility != Visibility.Visible) return;
                if (token.IsCancellationRequested) return;
            }

        }

        /// <summary>
        /// 当前句子位置，当设置时会自动更新界面
        /// </summary>
        public int Pos
        {
            get
            {
                return npos;
            }

            set
            {
                if (value > this.MaxPos || value < 0) return; // 确保不会超出范围
                npos = value;
                Update();
            }
        }

        /// <summary>
        /// 最大句子位置
        /// </summary>
        public int MaxPos
        {
            get => this.patterns.Length - 1;
        }

        
        private string[] patterns; // 分割好的句子

        private string text;
        /// <summary>
        /// 用于显示的全文
        /// </summary>
        public string Text
        {
            get => this.text;
            set
            {
                this.text = value;
                patterns = LittleHelper.Text.Split(value); // 按照规则切分文本

                if (patterns.Length != 0)
                    this.Pos = 0;
                else
                    this.Pos = -1;
            }
        }

        private bool paused = false;
        /// <summary>
        /// 暂停
        /// </summary>
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

        /// <summary>
        /// 更新界面
        /// </summary>
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
            this.baiduTts.Timeout = 10000; // 设置超时时间为10s

            this.SleepInterval = ConfigManager.Config.RepeatInterval * 1000;
            if (this.SleepInterval <= 0) this.SleepInterval = Timeout.Infinite;
            this.ResetRepeatTimer(); // 启动复读线程

        }

        /// <summary>
        /// 设置文本，并且显示窗口
        /// </summary>
        /// <param name="text"></param>
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

        /// <summary>
        /// 播放当前声音
        /// </summary>
        private void PlayCurrentSound()
        {
            PlaySound(this.Pos);
        }

        /// <summary>
        /// 播放声音
        /// </summary>
        /// <param name="n"></param>
        private void PlaySound(int n)
        {
            if (n > this.MaxPos || n < 0) return;

            byte[] value = null;
            bool preloaded;

            // 预载资源，需要加锁
            try
            {
                Monitor.Enter(this.lk);
                preloaded = this.preloadVoice.TryGetValue(n, out value);
            } finally
            {
                Monitor.Exit(this.lk);
            }


            if(!preloaded) // 没有预载，就当场加载
            {
                var errmsg = GetVoice(this.patterns[n], out value);
                if (value == null) throw new Exception("无法加载声音: " + errmsg);
                
                if (this.preloadVoice.ContainsKey(n))
                    this.preloadVoice.Add(n, value); // 省得加载第二遍
            }

            soundPlayer.Stream = new MemoryStream(value); // 播放
            soundPlayer.PlaySync();
        }

        /// <summary>
        /// 预载声音
        /// </summary>
        /// <param name="n">句子的位置</param>
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

        /// <summary>
        /// 卸载已加载的语音
        /// </summary>
        /// <param name="n">句子的位置</param>
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
            this.tokenSource.Cancel(); // 取消复读线程
        }
        
    }
}
