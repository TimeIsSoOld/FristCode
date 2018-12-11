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
using System.Configuration;
using SUNC_Main_DoctorProcess.TheThirdPart;

namespace SUNC_Main_DoctorProcess
{
    /// <summary>
    /// Options.xaml 的交互逻辑
    /// </summary>
    public partial class Options : Window
    {
        
        INIClass iniClass = null;
        string ps;
        public Options()
        {
            string FileFath = System.IO.Directory.GetCurrentDirectory() + "\\INIConfig\\OptionsSave.ini";
            iniClass = new INIClass(FileFath);
            InitializeComponent();
        }
        

        private void save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ps = this.textbox.Text;
                if (tzcheckBox.IsChecked == true)
                {
                    iniClass.IniWriteValue("Connect1", "IsChecked", true.ToString());
                }
                else
                {
                    iniClass.IniWriteValue("Connect1", "tzIsChecked", false.ToString());

                }
                if (blcheckBox.IsChecked == true)
                {
                    iniClass.IniWriteValue("Connect1", "IsChecked", true.ToString());
                }
                else
                {

                    iniClass.IniWriteValue("Connect1", "blIsChecked", false.ToString());
                }
                iniClass.IniWriteValue("Connect1", "ps", ps);
                MessageBox.Show("保存成功");
            }
            catch (Exception)
            {

                MessageBox.Show("保存失败，请重试！");
            }
           
            //this.Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textbox.Text = iniClass.IniReadValue("Connect1", "ps").ToString();
            if (iniClass.IniReadValue("Connect1", "tzIsChecked")==true.ToString())
            {
                tzcheckBox.IsChecked = true;
            }
            else
            {
                tzcheckBox.IsChecked = false;
            }
            if (iniClass.IniReadValue("Connect1", "tzIsChecked") == true.ToString())
            {
                blcheckBox.IsChecked = true;
            }
            else
            {
                blcheckBox.IsChecked = false;
            }
        }

        private void Dclose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
