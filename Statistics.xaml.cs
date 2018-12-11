using SUNC_Main_DoctorProcess.BLL;
using SUNC_Main_DoctorProcess.DAL;
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

namespace SUNC_Main_DoctorProcess
{
    /// <summary>
    /// Window2.xaml 的交互逻辑
    /// </summary>
    public partial class Statistics : Window
    {

        Bll_BurnRecord BRD = new Bll_BurnRecord();
        Dal_BurnRecord DBR = new Dal_BurnRecord();
        public Statistics()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //System.DateTime currentTime = new System.DateTime();
            //currentTime = System.DateTime.Now;
            //string time = currentTime.ToString("yyyy-MM-dd");
            string time = MainWindow.Gettime;
            string autodate = BRD.Selectdate_1(time);
            string nursedate = BRD.Selectdate_2(time);
            int sum =int.Parse(autodate) +int.Parse(nursedate);
            textBox_sum.Text = sum.ToString();
            textBox_zizhu.Text = autodate;
            textBox_hushi.Text = nursedate;
            textBox_time.Text = time;
            textBox_month.Text = DBR.Getselectdate_month(time);
            textBox_year.Text = DBR.Getselectdate_year(time);

        }
    }
}
