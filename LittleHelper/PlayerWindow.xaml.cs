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
using NAudio.Wave;

namespace LittleHelper
{
    /// <summary>
    /// PlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PlayerWindow : Window
    {
        private WaveOutEvent waveOutEvent = new WaveOutEvent();

        private int npos = 0;
        public int Pos
        {
            get
            {
                return npos;
            }

            set
            {
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

                this.Pos = 0;
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
                this.TextCurrent.Text = patterns[npos];
                this.ButtonPause.Content = this.Paused ? "继续" : "暂停";
                this.ButtonNext.IsEnabled = this.ButtonPrevious.IsEnabled = !this.Paused;
            });
        }
        
        public PlayerWindow()
        {
            InitializeComponent();
        }

        public void ShowWithText(string text)
        {
            base.Show();
            this.Text = text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Pos >= this.MaxPos) return;
            ++this.Pos;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (this.Pos <= 0) return;
            --this.Pos;
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            this.Paused = !this.Paused;
        }
    }
}
