using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SUNC_Main_DoctorProcess
{
    /// <summary>
    /// Londing.xaml 的交互逻辑
    /// </summary>
    public partial class Londing : Window
    {
        private delegate void UpdateDelegate();
        
        public Londing()
        {
            InitializeComponent();
           
        }

        public static void show(string od)
        {
           
            if (od == "star")
            {
                Londing childwin = new Londing();
                childwin.Star();
                childwin.WindowStartupLocation = WindowStartupLocation.Manual;
                childwin.Left = 500;
                childwin.Top = 150;
                childwin.ShowDialog();
            }
            else 
            {
                Londing childwin = new Londing();
                childwin.End();
                childwin.WindowStartupLocation = WindowStartupLocation.Manual;
                childwin.Left = 500;
                childwin.Top = 150;
                childwin.ShowDialog();
            }
           
        }
          
        public void Star()
        {
            tishi.Content = "请稍后。。。";
            cols.Visibility = Visibility.Hidden;
        }

        public void End()
        {
           
            //ThreadPool.QueueUserWorkItem(new WaitCallback(PrintTest));  
            new Thread(() =>
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    tishi.Content = "数据提取完成！";
                    cols.Visibility = Visibility.Visible;
                }));
            }).Start();
        }

        private void cols_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PrintTest(object state)
        {
            Thread.Sleep(2000);
            // UI thread dispatch the event into the event queue Async  
            this.Dispatcher.BeginInvoke(new UpdateDelegate(UIUpdate));

        }

        private void UIUpdate()
        {
            
        }  




      
    }
}
