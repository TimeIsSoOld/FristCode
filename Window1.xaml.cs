using SUNC_Main_DoctorProcess.TheThirdPart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }
        private void OpenWindow(object sender, RoutedEventArgs e)
        {
           
        } 

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            Ttp_PSetPrintingValue psptv = new Ttp_PSetPrintingValue();
            List<Ttp_TPrinting> tptList = new List<Ttp_TPrinting>();
            Ttp_TzPrinting pmpt = new Ttp_TzPrinting();
            pmpt.mPrint();
        }

        static void AddSecurityControll2Folder(string dirPath)
        {
            //获取文件夹信息
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            //获得该文件夹的所有访问权限
            System.Security.AccessControl.DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
            //设定文件ACL继承
            InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            //添加ereryone用户组的访问权限规则 完全控制权限
            FileSystemAccessRule everyoneFileSystemAccessRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            //添加Users用户组的访问权限规则 完全控制权限
            FileSystemAccessRule usersFileSystemAccessRule = new FileSystemAccessRule("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            bool isModified = false;
            dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule, out isModified); dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out isModified);
            //设置访问权限
            dir.SetAccessControl(dirSecurity);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Ttp_PSetPrintingValue psptv = new Ttp_PSetPrintingValue();
            List<Ttp_TPrinting> tptList = new List<Ttp_TPrinting>();
            Ttp_TPrinting tpt = new Ttp_TPrinting();
            Ttp_PMPrinting pmpt = new Ttp_PMPrinting();
            int sun = 0;
           // tpt.killWinWordProcess();
            for (int i= 0; i < 1; i++)
            {

                sun++;
                pmpt.Print.SerialNumber = sun.ToString();
                pmpt.Print.StudyMethod_ID = 2;
                pmpt.Print.ImagingFindings = "度发票都烦到死萨迪欧派扫地欧派dasoiopasfsdfjdskf 分鸡婆算了发圣诞节快乐福建省打开了房间莱克斯顿发圣诞节快乐简历撒电话即可了解活动的境况 大家快来空间的撒娇卡了登记卡洛斯，大家快来大健康大经理大叔控就开了daksl.dasjkjd多久啊萨科技的来看看即打开即可，大家撒空间看了大家快来打开打开即可卡四大皆空\n萨达金坷垃打开了多久开机快拉倒大生科技、第三方会计师大立科技";
                pmpt.Print.ImagingConclusion = "回家睡大觉伺服电机开发商放大和放假回家开发商打飞机";
                pmpt.mPrint();
            }
           

            // this._loading.Visibility = Visibility.Collapsed;
        }
    }

}
//System.Threading.Thread.Sleep(3000);
//OpenLonding("End");


//Ttp_PSetPrintingValue psptv = new Ttp_PSetPrintingValue();
//List<Ttp_TPrinting> tptList = new List<Ttp_TPrinting>();
//Ttp_PMPrinting pmpt = new Ttp_PMPrinting();
//pmpt.mPrint();