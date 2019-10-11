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

        public PlayerWindow()
        {
            InitializeComponent();
            
        }
    }
}
