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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LittleHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
         * TODO:
         * 快捷键的设置以及使用
         * 新的复读(?)线程的处理方法
         * 对设置参数的检查
         * 下载失败/损坏(我觉得可能是多线程的问题)的重试
         * 句子当前个数/总数的提示
         */


        public MainWindow()
        {
            InitializeComponent();

            // 加载配置
            ConfigManager.ReadConfig();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow();
            settingWindow.ShowDialog();
            ConfigManager.SaveConfig();
        }
        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = e.Source as TextBox;
            tb.PreviewMouseDown += new MouseButtonEventHandler(OnPreviewMouseDown);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = e.Source as TextBox;
            tb.Focus();
            e.Handled = true;
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = e.Source as TextBox;
            tb.SelectAll();
            tb.PreviewMouseDown -= new MouseButtonEventHandler(OnPreviewMouseDown);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PlayerWindow playerWindow = new PlayerWindow();
            //playerWindow.Owner = this;
            playerWindow.ShowWithText(this.TextBoxText.Text);
        }
    }
}
