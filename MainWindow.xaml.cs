using SUNC_Main_DoctorProcess.BLL;
using SUNC_Main_DoctorProcess.DAl;
using SUNC_Main_DoctorProcess.MDL;
using SUNC_Main_DoctorProcess.TheThirdPart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;
using SUNC_Main_DeviceProcess.TheThirdPart;
using System.Threading;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using Newtonsoft.Json;

namespace SUNC_Main_DoctorProcess
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {


        #region 定义的变量对象

        public string BarCodeResult = string.Empty;//= "001063864"//读取到的就诊卡号
        public string ProcessNumber = string.Empty;//流水号
        public string SerialNumber = string.Empty;//注册号
        public int ps;             //储存设置界面传递来的刷卡器，条码枪扫的卡号位数
        public bool tzisChecked;    //接收设置界面默认发卡时打印报告与贴纸的状态
        public bool blisChecked;
        int sert = 10;              //页面显示数据记录条数
       
        INIClass iniClass = null;
        Ttp_PHospital PH = new Ttp_PHospital();
        SerialCommunication SC = new SerialCommunication();
        Bll_TBL_B_VisitList BPG = new Bll_TBL_B_VisitList();
        USBBardCodeHooK BarCodeHooK = new USBBardCodeHooK();
        cCommunicationMiddleware CM = new cCommunicationMiddleware();
        BindingList<Mdl_MPatientInfo> BPGM = new BindingList<Mdl_MPatientInfo>();
        Bll_BurnRecord BRD = new Bll_BurnRecord();
        BindingList<Mdl_OFFLINE_PATIENT_INFOR> BPGMTwomonth = new BindingList<Mdl_OFFLINE_PATIENT_INFOR>();
        BLL.BLL_BIsEnough bEnough = new BLL.BLL_BIsEnough();
        #endregion
        #region 查询表示----------------------------------------------------
        public static  int IQuery = 0; //记录查询类别，0：查询，1：提取两个月前
        #endregion
        Thread[] _Thread = new Thread[2];
        public static bool IsTwoINFO=false;
        Bll_BIsCardLegal iscardlegal = new Bll_BIsCardLegal();
        public MainWindow()
        {        
            InitializeComponent();
            this.Left = 0;
            this.Top = 0;
            this.Height = SystemParameters.WorkArea.Height;//获取屏幕的宽高  使之不遮挡任务栏  
            this.Width = SystemParameters.WorkArea.Width;
            stater.IsEnabled = false;
            rest.IsEnabled = false;
            States.IsEnabled = false;
            statetwoMomethData.IsEnabled = false;
            tishi.Visibility = Visibility.Hidden;
            tishi1.Visibility = Visibility.Hidden;
            tishi2.Visibility = Visibility.Hidden;
            //BarCodeHooK.BarCodeEvent += new USBBardCodeHooK.BardCodeDeletegate(BarCode_BarCodeEvent);
            CM.onChangeEd += new cCommunicationMiddleware.UserDelegate(Monitocode);         //建立事件函数
            SC.DataReceivedHappen += new SerialCommunication.UserDelegateCom(Monitocode1);  //建立事件函数 
            caldate.SelectedDatesChanged += Caldate_SelectedDatesChanged;

            //string UCardName = bEnough.f_listFiles();
            this.Uname.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// 日历选择事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Caldate_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var dd = (Calendar)sender;
            DateTime? calendar = dd.SelectedDate;
            texttime.Text = calendar.Value.ToString("yyyy-MM-dd");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string FileFath = Directory.GetCurrentDirectory() + "\\INIConfig\\OptionsSave.ini";
            iniClass = new INIClass(FileFath);
            ps = int.Parse(iniClass.IniReadValue("Connect1", "ps"));
            tzisChecked = bool.Parse(iniClass.IniReadValue("Connect1", "tzIsChecked"));
            blisChecked = bool.Parse(iniClass.IniReadValue("Connect1", "blIsChecked"));
            //BarCodeHooK.Start();//安装钩子
            //f_CardIegal(BarCodeResult,  SerialNumber,  ProcessNumber);
            ChangeControl();//检查页面状态；
            InitialTimer();//加载并开始检查自助发卡机错误的定时器
            LondingTimer();
            //GetGridData();
            Make_TimerUCheck();
        }


        #region 定时器
        private DispatcherTimer _TimerUCheck;
        private int _TimerUCheckDelay = 600;
        private void Make_TimerUCheck()
        {
            _TimerUCheck = new DispatcherTimer();
            _TimerUCheck.Interval = TimeSpan.FromMilliseconds(_TimerUCheckDelay);
            _TimerUCheck.Tick += TimerUCheckEnvent;
            _TimerUCheck.Start();
        }

        private void TimerUCheckEnvent(object sender, object e)
        {
            if (Tool.UCardCheck.UCardMove)
            {
                this.Uname.Visibility = Visibility.Hidden;
            }
            else
            {
                this.Uname.Visibility = Visibility.Visible;
                if (Tool.UCardCheck.UCardIsReady)
                {
                    this.Uname.Content = "U卡可用：" + Tool.UCardCheck.UCardName;
                }
                else
                {
                    this.Uname.Content = "U卡不可用：" + Tool.UCardCheck.UCardName;
                }
            }
        }

        #region 页面自助终端报错定时器
        private DispatcherTimer _Timer;
        private int _TimeDelay = 9000;  //定时60秒   
        /// <summary>
        /// 页面自助终端报错定时器
        /// </summary>
        void InitialTimer()
        {
            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromMilliseconds(_TimeDelay);
            _Timer.Tick += _Timer_Tick;    //60秒计时完成触发该事件
            _Timer.Start();    //这个可以放在按钮事件里执行
        }
        /// <summary>
        /// 计时器事件，
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Timer_Tick(object sender, object e)
        {
            Dal_DVisitList Dv = new Dal_DVisitList();
            BindingList<MDL.Mdl_DeviceErrorLog> ErrorList = Dv.fB_BfontError();
            
            if (ErrorList.Count == 0)
            {
                tishi.Content = "设备端未发生错误！";
                tishi.Visibility = Visibility.Visible;
                //tishi1.Visibility = Visibility.Visible;
                //tishi2.Visibility = Visibility.Visible;
                _Timer.Stop();
            }
           else if (ErrorList.Count>0)
            {
                ErrorID = ErrorList[0].DEL_ID;
                tishi.Content = "时间："+ErrorList[0].DEL_D_ErrorTime+"    "+"错误信息："+ErrorList[0].DEL_V_ErrorInfo;
                tishi.Visibility = Visibility.Visible;
                tishi1.Visibility = Visibility.Visible;
                tishi2.Visibility = Visibility.Visible;
                _Timer.Stop();
            }
            else { return; }
        }
        int ErrorID = 0;
        private void tishi2_Click(object sender, RoutedEventArgs e)
        {
            _Timer.Start();
            tishi.Visibility = Visibility.Hidden;
            tishi1.Visibility = Visibility.Hidden;
            tishi2.Visibility = Visibility.Hidden;
        }
        private void tishi1_Click(object sender, RoutedEventArgs e)
        {
            Dal_DVisitList Dv = new Dal_DVisitList();
            if (Dv.fB_ModifyAnError(ErrorID))
            {
                tishi.Visibility = Visibility.Hidden;
                tishi1.Visibility = Visibility.Hidden;
                tishi2.Visibility = Visibility.Hidden;
                _Timer.Start();
            }
        }
        #endregion

        #region 更新卡片、等待窗体计时器
        private DispatcherTimer _UpDataTime;
        private int _UpDataTimeDelay = 1000;
        /// <summary>
        /// 更新卡片后台处理等待窗计时器
        /// </summary>
        void UpDataTimer()
        {
            _UpDataTime = new DispatcherTimer();
            _UpDataTime.Interval = TimeSpan.FromMilliseconds(_UpDataTimeDelay);//1秒计时完成触发该事件
            _UpDataTime.Tick += _UpData_Tick; //_UpData_Tick;    
            //_UpDataTime.Start();
        }
        /// <summary>
        /// 计时器触发的数据更新事件，并在更新完后关闭等待窗并显示最新数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _UpData_Tick(object sender, object e)
        {
            _UpDataTime.Stop();
            BLL_BIsEnough BLE = new BLL_BIsEnough();
            string chuanhao = BLE.f_listfile();//提取病人提供需要更新报告的的优卡内的串号
            Bll_BUpData UPD = new Bll_BUpData();
/*            bool Ok = UPD.AddUpData(chuanhao);*///传递提取出来的串号到数据中心，进行数据比对
            if (true)
            {
                //System.Windows.Forms.MessageBox.Show("更新报告申请已提交完成，请提醒病人十分钟后重新刷卡发放报告！");
                CloseLonding();

                #region 一直查询提交的更新报告的完成进度，但由于服务器终端相应较慢，不启用此代码改为提示病人十分钟后再来发卡
                //Dal_UpData DalUpdata = new Dal_UpData();//
                //do
                //{
                //    if (DalUpdata.fm_CheckUpDataConplet(chuanhao))
                //    {
                //        GetGridData(BarCodeResult);
                //        System.Windows.Forms.MessageBox.Show("数据更新提取完毕！请选择需要重发的数据。");
                //        CloseLonding();

                //        return;
                //    }

                //} while (true);
                #endregion
            }
            else
            {
                //System.Windows.Forms.MessageBox.Show("发送更新命令失败，请检查卡片和网络后重试！");
                CloseLonding();
            }
        }//
        /// <summary>
        /// 打开等待窗口
        /// </summary>
        public void OpenLonding()
        {
            this._loading.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 关闭等待窗
        /// </summary>
        public void CloseLonding()
        {
            this._loading.Visibility = Visibility.Collapsed;
        }
        #region 等待窗体计时器
        private DispatcherTimer _LondingTimer;
        private int WriteTime = 500;
        void LondingTimer()
        {
            _LondingTimer = new DispatcherTimer();
            _LondingTimer.Interval = TimeSpan.FromMilliseconds(WriteTime);
            _LondingTimer.Tick += _LondingTimer_Tick; //_UpData_Tick;    //20秒计时完成触发该事件
            //_LondingTimer.Start();
        }
        private void _LondingTimer_Tick(object sender, object e)
        {
            OpenLonding();
            //_LondingTimer.Stop();
        }
        #endregion
        #endregion


        #endregion

        #region 条码枪及扫码器
        /// <summary>
        /// 委托事件
        /// </summary>
        /// <param name="barCode"></param>
        private delegate void ShowInfoDelegate(USBBardCodeHooK.BarCodes barCode);
        private void ShowInfo(USBBardCodeHooK.BarCodes barCode)
        {
            //BarCodeResult中存放的为此卡刷卡器与扫码枪的输出结果
            BarCodeResult = barCode.IsValid ? barCode.BarCode : "";
            BarCodeResult = BarCodeResult.Substring(0, ps);
            //BarCodeResult = PH.f_CardCode();//测试时使用的随机就诊卡号

            f_CardIegal(BarCodeResult, SerialNumber, ProcessNumber);//判断就诊卡是否为合法的卡

            //f_ShowSendCard(BarCodeResult);//判断是否可以发卡
        }
        void BarCode_BarCodeEvent(USBBardCodeHooK.BarCodes barCode)
        {
            ShowInfo(barCode);
        }

        /// <summary>
        /// 接收条码枪返回值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        void Monitocode(object sender, cCommunication c)
        {
            //修改当前窗体控件值，
            BarCodeResult = c.ValueSrg;
            //BarCodeResult = PH.f_CardCode();//测试时使用的随机就诊卡号
            f_CardIegal(BarCodeResult, SerialNumber, ProcessNumber);
        }



        /// <summary>
        /// 监测条码枪
        /// </summary>
        /// <param name="indata"></param>
        void Monitocode1(string indata)
        {
            //修改当前窗体控件值，
            Dispatcher.BeginInvoke((Action)(() =>
            {
                CM.fv_SetMiddle("T11", indata);
            }));
        }
        #endregion

        protected override void OnSourceInitialized(EventArgs e)//狗子
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            Tool.UCardCheck _UCardCheck = new Tool.UCardCheck();
            source.AddHook(_UCardCheck.WndProc);
        }

        #region 页面数据处理
        /// <summary>
        /// 给页面的GridControl绑定数据并处理显示样式
        /// </summary>
        public bool GetGridData(string BarCodeResult, string SerialNumber, string ProcessNumber)
        {
            BPGM = BPG.fM_Select(BarCodeResult, SerialNumber, ProcessNumber);//查询barCodeResult, SerialNumber, ProcessNumber就诊卡下的报告信息
            BarCodeResult = BPGM[0].PI_V_CardCode;
            if (BPGM == null)
            {
                MessageBox.Show("未查询到数据，请确认是否是两个月之前的报告记录！");
                return false;
            }
            if (BPGM.Count() == 0)
            {
                MessageBox.Show("未查询到数据，请确认是否是两个月之前的报告记录！");
                return false;
            }
            else
            {
               
                gridControl1.ItemsSource = BPGM;
                DataContext = BPGM;
                gridpage.ShowPages(gridControl1, Mdl.PageSqlHelp.ToDataTable(BPGM), sert);
                name.Content = gridControl1.GetCellValue(0, "PI_V_Name");
                cardcode.Content = gridControl1.GetCellValue(0, "VL_PI_V_CardCode");
                StudyMethod.Text = gridControl1.GetCellValue(0, "VL_V_StudyBodyPart").ToString();
            }
            return true;
        }
        /// <summary>
        /// 给页面的GridControl绑定筛选过后的数据
        /// </summary>
        /// <param name="barCodeResult">就诊卡号</param>
        /// <param name="state">指定发卡的状态值</param>
        public void Screen(string barCodeResult, string state)
        {
            BPGM = BPG.fM_SelectBurnRecord(barCodeResult, state);//查询指定就诊卡下指定发卡状态的数据
            gridControl1.ItemsSource = BPGM;
            gridpage.ShowPages(gridControl1, Mdl.PageSqlHelp.ToDataTable(BPGM), sert);
            if (BPGM.Count == 0)
            {
                MessageBox.Show("未查询到数据！");

                return;
            }
            DataContext = BPGM;
            name.Content = gridControl1.GetCellValue(0, "PI_V_Name");
            cardcode.Content = gridControl1.GetCellValue(0, "VL_PI_V_CardCode");
            StudyMethod.Text = gridControl1.GetCellValue(0, "VL_V_StudyBodyPart").ToString();
        }

        /// <summary>
        /// 更改页面数据
        /// </summary>
        /// <param name="value">改变的值</param>
        /// <param name="columnName">改变值的列名</param>
        public void ChangeScoue(int value, string columnName)
        {
            for (int i = 0; i < sert; i++)
            {
                int rowHandle = gridControl1.GetRowHandleByListIndex(i);

                object wholeRowObject = gridControl1.GetRow(rowHandle);
                if (wholeRowObject == null)
                { break; }
                else
                {
                    object rowCheck = gridControl1.GetCellValue(rowHandle, "IsValid");
                    bool ifCheck = (bool)rowCheck;
                    if (ifCheck)
                    {
                        gridControl1.SetCellValue(rowHandle, columnName, value);
                    }
                }
            }
        }
        #endregion

        #region 复选框相关事件=================================
        /// <summary>
        /// 根据勾选的checkBox改变对应按钮的状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_Click(object sender, RoutedEventArgs e)
        {
            ChangeControl();//改变控件状态。未满足状态的按钮为不可用状态
            //checkControlState();
        }
        #region 验证状态
        /// <summary>
        /// 得到列的选中状态和索引值并提取注册号
        /// </summary>0
        public void checkControlState()
        {
            //List<string> getSerialNumber = new List<string>();//储存注册号
            //string getStudyMethod = "";//储存检查部位
            List<int> ListrowHandle = new List<int>();
            for (int i = 0; i < sert; i++)
            {
                int rowHandle = gridControl1.GetRowHandleByListIndex(i);//获取行
                ListrowHandle.Add(rowHandle);
                object wholeRowObject = gridControl1.GetRow(ListrowHandle[i]);
                if (wholeRowObject == null)
                {
                    break;
                }
                else
                {
                    object rowCheck = gridControl1.GetCellValue(ListrowHandle[i], "IsValid");//提取指定行中指定列的值
                    bool ifCheck = (bool)rowCheck;
                    if (ifCheck)
                    {
                        string stateCard_1 = "";
                        object stateCard;
                        try
                        {
                            stateCard = gridControl1.GetCellValue(ListrowHandle[i], "VL_I_State");
                            if (stateCard.ToString() == null)
                            {

                            }
                        }
                        catch (Exception ex)
                        {
                            stateCard = gridControl1.GetCellValue(ListrowHandle[i], "OV_I_State");
                        }               
                      stateCard_1 = stateCard.ToString();
                        if (stateCard_1 =="0")
                        {
                            stateCard_1 = "未发卡";
                        }
                        switch (stateCard_1)
                        {
                            case "未发卡":
                                this.stater.IsEnabled = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

        }

        #endregion

        List<string> back = new List<string>();//储存提取到的注册号
        /// <summary>
        /// 得到列的选中状态和索引值并提取注册号
        /// </summary>0
        public List<string> GetCheckBoxId()
        {
            List<string> getSerialNumber = new List<string>();//储存注册号
            string getStudyMethod = "";//储存检查部位
            List<int> ListrowHandle = new List<int>();
            for (int i = 0; i < sert; i++)
            {
                int rowHandle = gridControl1.GetRowHandleByListIndex(i);//获取行
                ListrowHandle.Add(rowHandle);
                object wholeRowObject = gridControl1.GetRow(ListrowHandle[i]);
                if (wholeRowObject == null) 
                {
                    break;
                }
                else
                {
                    int L = ListrowHandle[i];
                    object rowCheck = gridControl1.GetCellValue(L, "IsValid");//提取指定行中指定列的值IsValid

                    bool ifCheck = (bool)rowCheck;
                    if (ifCheck)
                    {
                            object rowname = gridControl1.GetCellValue(ListrowHandle[i], "VL_V_SerialNumber");
                            getSerialNumber.Add(rowname.ToString());
                            object studmetd = gridControl1.GetCellValue(ListrowHandle[i], "VL_V_StudyBodyPart");
                            getStudyMethod += studmetd.ToString() + "\r\n";
                            back.Add(rowname.ToString());
                    }
                }
            }
            StudyMethod.Text = getStudyMethod;
            return getSerialNumber;
        }
        /// <summary>
        /// 根据勾选的CheckBox对应的流水号记录改变按钮状态
        /// </summary>
        public void ChangeControl()
        {
            List<string> sermnuber = GetCheckBoxId();
            if (sermnuber.Count == 0)
            {
                rest.IsEnabled = false;//重置按钮
                stater.IsEnabled = false;//发卡按钮
                sendcard.IsEnabled = false;//发卡按钮
                States.IsEnabled = false;//打印按钮
                FilmProcssing.IsEnabled = false;//胶片按钮
                reset.IsEnabled = false;//一键重置按钮  
                print.IsEnabled = false;//一键打印
                return;
            }
            else if (IQuery==1)
            {
                rest.IsEnabled = true;//重置按钮
                stater.IsEnabled = true;//发卡按钮
                sendcard.IsEnabled = true;//发卡按钮
                States.IsEnabled = true;//打印按钮
                FilmProcssing.IsEnabled = true;//胶片按钮
                reset.IsEnabled = true;//一键重置按钮  
                print.IsEnabled = true;//一键打印
                medical.IsEnabled = true;
                sick.IsEnabled = true;
            }
            else
            {
                States.IsEnabled = true;//打印按钮
                reset.IsEnabled = true;
                rest.IsEnabled = true;                
                print.IsEnabled = true;
                Bll_BCanSendCard canSendCard = new Bll_BCanSendCard();
                bool SendCard = canSendCard.fB_CanSendCard(sermnuber);
                if (SendCard) //SendCard
                {
                    stater.IsEnabled = true;
                    sendcard.IsEnabled = true ;
                }
                else
                {
                    sendcard.IsEnabled = false;
                    stater.IsEnabled = false;
                }
                bool MedicalState = canSendCard.fB_CanMedicalState(sermnuber);
                if (MedicalState)
                {
                    medical.IsEnabled = true;
                }
                else
                {
                    medical.IsEnabled = false;
                }
                bool SickState = canSendCard.fB_CanSickState(sermnuber);
                if (SickState)
                {
                    sick.IsEnabled = true;
                }
                else
                {
                    sick.IsEnabled = false;
                }
                bool FilmState = canSendCard.Fb_filmprocessing(sermnuber);
                if (FilmState)
                {
                    FilmProcssing.IsEnabled = true;
                    state.IsEnabled = true;
                }
                else
                {
                    FilmProcssing.IsEnabled = false;
                    state.IsEnabled = false;
                }

            }
        }
        #endregion

        #region 帮助 缩小 关闭==================================
        private void btnmin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnclose_Click(object sender, RoutedEventArgs e)
        {
            BarCodeHooK.Stop();//卸载钩子
            Killexe();
            //this.Close();
        }

        private void Dhelp_Click(object sender, RoutedEventArgs e)
        {
            string docPath = System.IO.Directory.GetCurrentDirectory() + "\\护士端程序帮助文档.docx";
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo(docPath);
            p.StartInfo = psi;
            try
            {
                p.Start();
            }
            catch (Exception ex)
            {

                System.Windows.Forms.MessageBox.Show("\r\n发生错误：\r\n" + ex.Message);
            }
        }
        #endregion

        #region 发卡 打印及搜索事件
        /// <summary>
        /// 发卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stater_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<Mdl_MPatientInfo> PatientInfoList = new List<Mdl_MPatientInfo>();
            if (BoolHaveUCard())
            {
                if (IQuery == 1)
                {
                    BLL_TwoMonth btwomonth = new BLL.BLL_TwoMonth();
                    //if (!btwomonth.fB_SetTwoMonth(keys.Text.Trim(), keys1.Text.Trim(), keys2.Text.Trim()))
                    //{
                    //    MessageBox.Show("数据处理错误，不能发卡！");
                       
                    //    return;
                    //}
                }
                List<string> serNumber = GetCheckBoxId();
                //提取需要发卡的信息
                f_ShowSendCard(serNumber);
                //根据设置的默认值，判断是否同时打印贴纸和报告单            
            }
        }

        /// <summary>
        /// 打印报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void medical_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            try
            {
                List<string> MedicalStateSerialNumber = GetCheckBoxId();//得到注册号
                List<Mdl_MPatientInfo> PatientInfoList = new List<Mdl_MPatientInfo>();
                BPGM = BPG.fM_Select(BarCodeResult, SerialNumber, ProcessNumber);//查询barCodeResult, SerialNumber, ProcessNumber就诊卡下的报告信息
                BarCodeResult = BPGM[0].PI_V_CardCode;
                for (int i = 0; i < MedicalStateSerialNumber.Count; i++)
                {
                    if (IQuery == 0)
                    {
                        PatientInfoList = BPG.fML_PatientInfo(BarCodeResult, MedicalStateSerialNumber[i]);                
                    }
                    else if (IQuery == 1)
                    {
                        #region
                        List<Mdl_OFFLINE_PATIENT_INFOR> PatientInfoList_ago = BPG.fML_PatientInfo_ago(BarCodeResult, MedicalStateSerialNumber[i]);
                        foreach (Mdl_OFFLINE_PATIENT_INFOR it in PatientInfoList_ago)
                        {
                            Mdl_MPatientInfo MDP = new Mdl_MPatientInfo();
                            MDP.PI_V_PatientNo = it.PIO_V_PatientNo;
                            MDP.PI_V_Name = it.PIO_V_Name;
                            MDP.PI_V_CardCode = it.PIO_V_CardCode;
                            MDP.PI_I_Age = it.PIO_I_Age;
                            MDP.PI_V_Sex = it.PIO_V_Sex;
                            MDP.PI_V_Address = it.PIO_V_Address;
                            MDP.PI_V_IDNumber = it.PIO_V_IDNumber;
                            MDP.PI_V_MedicareNumber = it.PIO_V_MedicareNumber;
                            MDP.PI_V_Phone = it.PIO_V_Phone;
                            MDP.PI_V_AgeUnit = it.PIO_V_AgeUnit;
                            MDP.PI_D_InsterTime = it.PIO_D_InsterTime;

                            MDP.VL_V_SerialNumber = it.OV_V_SerialNumber;
                            MDP.VL_PI_V_PatientNo = it.OV_PI_V_PatientNo;
                            MDP.VL_PI_V_CardCode = it.OV_PI_V_CardCode;
                            MDP.VL_V_DeptName = it.OV_V_DeptName;
                            MDP.VL_V_StudyBodyPart = it.OV_V_StudyBodyPart;
                            MDP.VL_V_StudyDeptName = it.VL_V_StudyDeptName;

                            try
                            {
                                MDP.VL_D_RegistrationDate = it.OV_D_RegistrationDate;
                            }
                            catch (Exception)
                            { }

                            MDP.VL_I_StudyMethod_ID = it.OV_I_StudyMethod_ID;
                            //MDP.VL_I_StudyMethod_Name = it.SM_V_VALUE + it.SM_V_Name;
                            MDP.VL_V_MODALITY = it.OV_V_MODALITY;

                            MDP.VL_I_DiseaseCategory_ID = it.OV_I_DiseaseCategory_ID.Value;
                            MDP.VL_I_RegistratorType = it.OV_I_RegistratorType;

                            MDP.VL_V_RoomNum = it.OV_V_RoomNum;
                            MDP.VL_V_BedNum = it.OV_V_BedNum;
                            MDP.VL_V_HospitalName = it.OV_V_HospitalName;

                            MDP.VL_I_SickPrintState = it.OV_I_SickPrintState;
                            MDP.VL_I_RePrintState = it.OV_I_RePrintState;
                            MDP.VL_I_State = it.OV_I_State;

                            MDP.VL_V_ProcessNumber = it.OV_V_ProcessNumber;
                            MDP.VL_I_FlagStudy = it.OV_I_FlagStudy;

                            MDP.VL_I_ImageCount = it.OV_I_ImageCount;
                            MDP.VL_V_PathNO = it.OV_V_PathNO;


                            MDP.DD_VL_V_SerialNumber = it.OD_VL_V_SerialNumber;
                            MDP.DD_PI_V_CardCode = it.OD_PI_V_CardCode;
                            MDP.DD_SUBMIT_DOC_ID = it.OD_SUBMIT_DOC_ID;
                            MDP.DD_SUBMIT_DOC_NAME = it.OD_SUBMIT_DOC_NAME;
                            MDP.DD_CHECK_DOC_ID = it.OD_CHECK_DOC_ID;

                            MDP.DD_CHECK_DOC_NAME = it.OD_CHECK_DOC_NAME;
                            MDP.DD_CHECK_DATETIME = it.OD_CHECK_DATETIME;
                            MDP.DD_FLAG_CHECK = it.OD_FLAG_CHECK;
                            MDP.DD_FLAG_INVALID = it.OD_FLAG_INVALID;
                            MDP.DD_T_ILLSUMMARY = it.OD_T_ILLSUMMARY;
                            MDP.DD_T_DIAGNOSIS = it.OD_T_DIAGNOSIS;
                            MDP.DD_T_DICOMFindings = it.OD_T_ImagingFindings;
                            MDP.DD_T_DICOMConclusion = it.OD_T_ImagingConclusion;
                            MDP.SM_V_Name = it.SM_V_Name;
                            MDP.SM_V_VALUE = it.SM_V_VALUE;
                            MDP.VL_D_StudyDate = it.OV_D_StudyDate;

                            MDP.DD_Submit_Doc_Image = it.OD_Submit_Doc_Image;
                            MDP.DD_Check_Doc_Image = it.OD_Check_Doc_Image;
                            PatientInfoList.Add(MDP);
                        }
                        #endregion
                    }
                    if (PatientInfoList.Count < 1) return;
                    if (!PatientInfoList[i].DD_FLAG_CHECK)
                    {
                        MessageBox.Show(string.Format("第{0}个报告未审核", i + 1));
                        continue;
                    }
                    //A4打印机打印
                    Ttp_PSetPrintingValue psptv = new Ttp_PSetPrintingValue();
                    List<Ttp_TPrinting> tptList = psptv.fL_LSetPrintingValue(PatientInfoList);

                    PrintPMAsync(tptList);
                    #region 设置报告打印状态为1
                    Dal_DVisitList dvlTemp = new Dal_DVisitList();
                    if (IQuery == 0)
                    {
                        if (dvlTemp.fB_BSetMedicalState(1, MedicalStateSerialNumber[i]))
                        {
                            //报告打印状态设置成功
                            //GetGridData(BarCodeResult,back);
                            ChangeScoue(1, "VL_I_RePrintState");
                        }
                        else
                        {
                            //报告打印状态设置为假
                            MessageBox.Show("报告打印状态更新失败，请重新打印！");
                        }
                    }
                    else if (IQuery == 1)
                    {
                        if (dvlTemp.fB_BSetMedicalState_ago(1, MedicalStateSerialNumber[i]))
                        {
                            //报告打印状态设置成功
                            //GetGridData(BarCodeResult,back);
                            ChangeScoue(1, "VL_I_RePrintState");
                        }
                        else
                        {
                            //报告打印状态设置为假
                            MessageBox.Show("报告打印状态更新失败，请重新打印！");
                        }
                    }
                    #endregion


                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("错误：" + ex);
            }
        }
        /// <summary>
        /// 打印报告报告（异步）
        /// </summary>
        /// <param name="tptList"></param>
        private async void PrintPMAsync(List<Ttp_TPrinting> tptList)
        {
            //var t3 = await Task.Run(() => GetGuid());
            Task<bool> task = new Task<bool>(() =>
            {
                foreach (Ttp_TPrinting tpt in tptList)
                {
                    Ttp_PMPrinting pmpt = new Ttp_PMPrinting();
                    pmpt.Print = tpt;
                    pmpt.mPrint();//发送打印命令
                    Thread.Sleep(8000);
                }
                return true;
            });
            task.Start();
            bool b = await task;

           
        }

        /// <summary>
        /// 打印贴纸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void sick_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<string> SickStateSerialNumber = GetCheckBoxId();
            List<Mdl_MPatientInfo> PatientInfoList = new List<Mdl_MPatientInfo>();
            BPGM = BPG.fM_Select(BarCodeResult, SerialNumber, ProcessNumber);//查询barCodeResult, SerialNumber, ProcessNumber就诊卡下的报告信息
            BarCodeResult = BPGM[0].PI_V_CardCode;
            for (int i = 0; i < SickStateSerialNumber.Count; i++)
            {
                if (IQuery == 0)
                {
                    PatientInfoList = BPG.fML_PatientInfo(BarCodeResult, SickStateSerialNumber[i]);
                }
                else if (IQuery == 1)
                {
                    List<Mdl_OFFLINE_PATIENT_INFOR> PatientInfoList_ago = BPG.fML_PatientInfo_ago(BarCodeResult, SickStateSerialNumber[i]);
                    foreach (Mdl_OFFLINE_PATIENT_INFOR it in PatientInfoList_ago)
                    {
                        Mdl_MPatientInfo MDP = new Mdl_MPatientInfo();
                        MDP.PI_V_PatientNo = it.PIO_V_PatientNo;
                        MDP.PI_V_Name = it.PIO_V_Name;
                        MDP.PI_V_CardCode = it.PIO_V_CardCode;
                        MDP.PI_I_Age = it.PIO_I_Age;
                        MDP.PI_V_Sex = it.PIO_V_Sex;
                        MDP.PI_V_Address = it.PIO_V_Address;
                        MDP.PI_V_IDNumber = it.PIO_V_IDNumber;
                        MDP.PI_V_MedicareNumber = it.PIO_V_MedicareNumber;
                        MDP.PI_V_Phone = it.PIO_V_Phone;
                        MDP.PI_V_AgeUnit = it.PIO_V_AgeUnit;
                        MDP.PI_D_InsterTime = it.PIO_D_InsterTime;

                        MDP.VL_V_SerialNumber = it.OV_V_SerialNumber;
                        MDP.VL_PI_V_PatientNo = it.OV_PI_V_PatientNo;
                        MDP.VL_PI_V_CardCode = it.OV_PI_V_CardCode;
                        MDP.VL_V_DeptName = it.OV_V_DeptName;
                        MDP.VL_V_StudyBodyPart = it.OV_V_StudyBodyPart;
                        MDP.VL_V_StudyDeptName = it.VL_V_StudyDeptName;

                        try
                        {
                            MDP.VL_D_RegistrationDate = it.OV_D_RegistrationDate;
                        }
                        catch (Exception)
                        { }

                        MDP.VL_I_StudyMethod_ID = it.OV_I_StudyMethod_ID;
                        //MDP.VL_I_StudyMethod_Name = it.SM_V_VALUE + it.SM_V_Name;
                        MDP.VL_V_MODALITY = it.OV_V_MODALITY;

                        MDP.VL_I_DiseaseCategory_ID = it.OV_I_DiseaseCategory_ID.Value;
                        MDP.VL_I_RegistratorType = it.OV_I_RegistratorType;

                        MDP.VL_V_RoomNum = it.OV_V_RoomNum;
                        MDP.VL_V_BedNum = it.OV_V_BedNum;
                        MDP.VL_V_HospitalName = it.OV_V_HospitalName;

                        MDP.VL_I_SickPrintState = it.OV_I_SickPrintState;
                        MDP.VL_I_RePrintState = it.OV_I_RePrintState;
                        MDP.VL_I_State = it.OV_I_State;

                        MDP.VL_V_ProcessNumber = it.OV_V_ProcessNumber;
                        MDP.VL_I_FlagStudy = it.OV_I_FlagStudy;

                        MDP.VL_I_ImageCount = it.OV_I_ImageCount;
                        MDP.VL_V_PathNO = it.OV_V_PathNO;


                        MDP.DD_VL_V_SerialNumber = it.OD_VL_V_SerialNumber;
                        MDP.DD_PI_V_CardCode = it.OD_PI_V_CardCode;
                        MDP.DD_SUBMIT_DOC_ID = it.OD_SUBMIT_DOC_ID;
                        MDP.DD_SUBMIT_DOC_NAME = it.OD_SUBMIT_DOC_NAME;
                        MDP.DD_CHECK_DOC_ID = it.OD_CHECK_DOC_ID;

                        MDP.DD_CHECK_DOC_NAME = it.OD_CHECK_DOC_NAME;
                        MDP.DD_CHECK_DATETIME = it.OD_CHECK_DATETIME;
                        MDP.DD_FLAG_CHECK = it.OD_FLAG_CHECK;
                        MDP.DD_FLAG_INVALID = it.OD_FLAG_INVALID;
                        MDP.DD_T_ILLSUMMARY = it.OD_T_ILLSUMMARY;
                        MDP.DD_T_DIAGNOSIS = it.OD_T_DIAGNOSIS;
                        MDP.DD_T_DICOMFindings = it.OD_T_ImagingFindings;
                        MDP.DD_T_DICOMConclusion = it.OD_T_ImagingConclusion;
                        MDP.SM_V_Name = it.SM_V_Name;
                        MDP.SM_V_VALUE = it.SM_V_VALUE;
                        MDP.VL_D_StudyDate = it.OV_D_StudyDate;
                        PatientInfoList.Add(MDP);
                    }
                }
                
                if (PatientInfoList.Count < 1) return;
                if (!PatientInfoList[i].DD_FLAG_CHECK)
                {
                    MessageBox.Show(string.Format("第{0}个报告未审核", i + 1));
                    continue;
                }
                //贴纸打印机
                //Ttp_PSetPrintingValue psptv = new Ttp_PSetPrintingValue();
                //List<Ttp_TPrinting> tptList = new List<Ttp_TPrinting>();
                //tptList = psptv.fL_LSetPrintingValue(PatientInfoList);

                bool printStateB = await PrintTZAsync(PatientInfoList);
                if (printStateB)
                {
                        ////修改贴纸打印状态
                        Dal_DVisitList dvlTemp = new Dal_DVisitList();
                    if (IQuery==0)
                    {
                        if (dvlTemp.fB_BSetSickState(1, SickStateSerialNumber[i]))
                        {
                            //GetGridData(BarCodeResult,back);
                            try
                            {
                                ChangeScoue(0, "VL_I_SickState");

                            }
                            catch (Exception ex)
                            {
                            }
                            // MessageBox.Show("贴纸打印成功");
                            //贴纸打印状态设置成功
                        }
                        else
                        {
                            //贴纸打印状态设置为假
                            MessageBox.Show("记录修改失败，请重试！");
                        }
                    }
                    else if (IQuery==1)
                    {
                        if (dvlTemp.fB_BSetSickState_ago(1, SickStateSerialNumber[i]))
                        {
                            //GetGridData(BarCodeResult,back);
                            try
                            {
                                ChangeScoue(0, "VL_I_SickState");

                            }
                            catch (Exception ex)
                            {
                            }
                            // MessageBox.Show("贴纸打印成功");
                            //贴纸打印状态设置成功
                        }
                        else
                        {
                            //贴纸打印状态设置为假
                            MessageBox.Show("记录修改失败，请重试！");
                        }
                    }                     
                }
            }
        }
        /// <summary>
        /// 打印贴纸（异步）
        /// </summary>
        /// <param name="MPIList"></param>
        private async Task<bool> PrintTZAsync(List<Mdl_MPatientInfo> MPIList)
        {
            bool PrintSate = await Task.Run(() =>
            {
                Ttp_TzPrinting pmpt = new Ttp_TzPrinting();
                Ttp_TPrinting ttp = new Ttp_TPrinting();
                //给贴纸上打印的内容赋值
                ttp.Name = MPIList[0].PI_V_Name;
                ttp.CardCode = MPIList[0].PI_V_CardCode;
                ttp.RegistrationDate = MPIList[0].VL_D_RegistrationDate.ToString();
                ttp.StudyBodyPart = MPIList[0].VL_V_StudyBodyPart;
                pmpt.Print = ttp;
                pmpt.mPrint();
                return true;
            });
            return PrintSate;
        }

        /// <summary>
        /// 打印胶片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void state_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            FilmProcssing_ItemClick(null, null);
            //List<string> StateSerialNumber = GetCheckBoxId();
            //Dal_DVisitList dvlTemp = new Dal_DVisitList();
            //if (IQuery==0)
            //{
            //    if (dvlTemp.fB_BSetState(3, StateSerialNumber))
            //    {
            //        ChangeScoue(3, "VL_I_State");
            //        //GetGridData(BarCodeResult,back);
            //        System.Windows.Forms.MessageBox.Show("请指导病人取胶片");
            //        //胶片打印状态设置成功
            //    }
            //    else
            //    {
            //        //胶片打印状态设置为假
            //        System.Windows.MessageBox.Show("记录修改失败，请重试！");
            //    }
            //}
            //else if (IQuery==1)
            //{

            //    if (dvlTemp.fB_BSetState_ago(3, StateSerialNumber))
            //    {
            //        ChangeScoue(3, "VL_I_State");
            //        //GetGridData(BarCodeResult,back);
            //        System.Windows.Forms.MessageBox.Show("请指导病人取胶片");
            //        //胶片打印状态设置成功
            //    }
            //    else
            //    {
            //        //胶片打印状态设置为假
            //        System.Windows.MessageBox.Show("记录修改失败，请重试！");
            //    }
            //}

        }
        /// <summary>
        /// 判断就诊卡是否合法,是：显示病人信息,否：提示医护人员确认
        /// </summary>
        private void f_CardIegal(string BarCodeResult, string SerialNumber, string ProcessNumber)
        {
            Bll_BIsCardLegal iscardlegal = new Bll_BIsCardLegal();
            if (BarCodeResult != null)
            {
                bool legal = iscardlegal.fB_IsCardLegal(BarCodeResult);
                if (legal)//就诊卡合法
                {
                    back.Clear();
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StudyMethod.Text = "";
                        GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                    }));
                }
                else//就诊卡非法
                {
                    MessageBox.Show("就诊卡无效，请确认卡片！");
                    return;
                }
            }
            else
            {
                BarCodeResult = iscardlegal.Fb_getBCR(SerialNumber, ProcessNumber).ToString();
                bool legal = iscardlegal.fB_IsCardLegal(BarCodeResult);
                if (legal)//就诊卡合法
                {
                    back.Clear();
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StudyMethod.Text = "";
                        GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                    }));
                }
                else//就诊卡非法
                {
                    MessageBox.Show("就诊卡无效，请确认卡片！");
                    return;
                }
            }
        }
        /// <summary>
        /// 判断就诊卡是否合法,是：显示病人信息,否：提示医护人员确认
        /// </summary>
        private void CardIegal(string BarCodeResult, string SerialNumber, string ProcessNumber)
        {
            Bll_BIsCardLegal iscardlegal = new Bll_BIsCardLegal();

            if (BarCodeResult == null)
            {
                BarCodeResult = iscardlegal.Fb_getBCR(SerialNumber, ProcessNumber);
                bool legal = iscardlegal.fB_IsCardLegal(BarCodeResult);
                if (legal)//就诊卡合法
                {
                    StudyMethod.Text = "";
                    back.Clear();
                    GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                }
                else//就诊卡非法
                {
                    MessageBox.Show("就诊卡无效，请确认卡片！");
                    return;
                }

            }

            else
            {
                bool legal = iscardlegal.fB_IsCardLegal(BarCodeResult);
                if (legal)//就诊卡合法
                {
                    StudyMethod.Text = "";
                    back.Clear();
                    GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                }
                else//就诊卡非法
                {
                    MessageBox.Show("就诊卡无效，请确认卡片！");
                    return;
                }

            }
        }

        
        /// <summary>
        /// 判断是否可以发卡
        /// </summary>
        /// <param name="barCode"></param>
        private async void f_ShowSendCard(List<string> SerialNumber)
        {
            Bll_BCanSendCard canSendCard = new Bll_BCanSendCard();
            bool SendCard = await Task.Run(() => canSendCard.fB_CanSendCard(SerialNumber));
                       
            if (SendCard)
            {
                if (IQuery == 0)
                {
                    WindowProcrss jdt = new WindowProcrss();
                    jdt.SerialNumber = SerialNumber;
                    if (BarCodeResult != null)
                    {
                        jdt.BarCodeResult = BarCodeResult;
                    }
                    else
                    {
                        BarCodeResult = iscardlegal.Fb_getBCR(SerialNumber, ProcessNumber);
                        BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                        jdt.BarCodeResult = BarCodeResult;
                    }
                    jdt.ShowDialog();
                    if (jdt.SendCardB)
                    {
                        //设置发卡状态

                        List<string> getSerialNumber = GetCheckBoxId();
                        Bll_TBL_B_VisitList bPatientInfo = new Bll_TBL_B_VisitList();
                        //获得查询结果
                        BindingList<Mdl_MPatientInfo> PatientInfoList = bPatientInfo.fM_SelectSerialNumber(BarCodeResult, getSerialNumber);
                        bool SetStateB = BSetState(PatientInfoList);
                        //将更新记录添加到TBL_B_BurnRecord（刻录记录表）中
                        // bool UpdateDate = BRD.GetUpdateDate(PatientInfoList);
                        //根据设置的默认值，判断是否同时打印贴纸和报告单
                        if (tzisChecked) sick_ItemClick(null, null);
                        if (blisChecked) medical_ItemClick(null, null);
                    }
                    else
                    {
                        MessageBox.Show("发卡失败！，请重试！");
                    }
                    return;
                }
                else if (IQuery == 1)
                {
                        WindowProcrss jdt = new WindowProcrss();
                        jdt.SerialNumber = SerialNumber;
                        if (BarCodeResult != null)
                        {
                            jdt.BarCodeResult = BarCodeResult;
                        }
                        else
                        {
                            BarCodeResult = iscardlegal.Fb_getBCR_agotwo(SerialNumber, ProcessNumber);
                            BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                            jdt.BarCodeResult = BarCodeResult;
                        }
                        jdt.ShowDialog();
                        if (jdt.SendCardB)
                        {
                            //设置发卡状态

                            List<string> getSerialNumber = GetCheckBoxId();
                            Bll_TBL_B_VisitList bPatientInfo = new Bll_TBL_B_VisitList();
                            //获得查询结果
                           BindingList<Mdl_OFFLINE_PATIENT_INFOR> PatientInfoList_ago = bPatientInfo.fM_SelectSerialNumber_ago(BarCodeResult, getSerialNumber);
                            bool SetStateB = BSetState_ago(PatientInfoList_ago);
                            //将更新记录添加到TBL_B_BurnRecord（刻录记录表）中
                            // bool UpdateDate = BRD.GetUpdateDate(PatientInfoList);
                            //根据设置的默认值，判断是否同时打印贴纸和报告单
                            if (tzisChecked) sick_ItemClick(null, null);
                            if (blisChecked) medical_ItemClick(null, null);
                        }
                        else
                        {
                            MessageBox.Show("发卡失败！，请重试！");
                        }
                        return;
                    }               
                MessageBox.Show("医生审核未完成或已发过卡，不能发卡。");           
            }
        }

        /// <summary>
        /// 返回所有报告信息并生成XML报告
        /// </summary>
        /// <returns></returns>
        private async Task<bool> f_ReturnsAllInformation(BindingList<Mdl_MPatientInfo> PatientInfoList)
        {
            Ttp_TCreateXmlFile CXF = new Ttp_TCreateXmlFile();
            //生成XML报告
            bool CopyXmlB = await CXF.XmlFileAsync(PatientInfoList);
            //复制DCOM图片
            bool CopyPictureXmlB = await CXF.CopyPicture(PatientInfoList);
            if (CopyXmlB && CopyPictureXmlB) return true;
            return false;
        }
        /// <summary>
        /// U卡串号
        /// </summary>
        private string Sc_i_serialnumber { get; set; }
        /// <summary>
        /// 判断U盘是否连接正常
        /// </summary>
        private bool BoolHaveUCard()
        {
            BLL_BIsEnough bi = new BLL_BIsEnough();
            //判断优卡是否连接，连接的优卡是否合法
            bi.Fn_B_IsEnough();
            //是否连接
            if (!bi.enoughb) { MessageBox.Show("未插入U卡！请插入U卡！"); return false; }
            //是否合法
            if (!bi.legal) { MessageBox.Show("U卡未识别，请检查！"); return false; }
            Sc_i_serialnumber = bi.shibie;
            return true;
        }
        #region          ---------------------------------------------
        //private async Task<bool> SendCardAndCopyDate(string BarCodeResult)
        //{
        //    List<string> getSerialNumber = GetCheckBoxId();
        //    Bll_TBL_B_VisitList bPatientInfo = new Bll_TBL_B_VisitList();
        //    //获得查询结果
        //    BindingList<Mdl_MPatientInfo> PatientInfoList = bPatientInfo.fM_SelectSerialNumber(BarCodeResult, getSerialNumber); //生成XML 拷贝DICOM   
        //    bool b = await f_ReturnsAllInformation(PatientInfoList);
        //    if (true)
        //    {
        //        MessageBox.Show("U卡数据拷贝出现问题！请更换U卡！");
        //        return false;
        //    }
        //    Mdl_UpData upd = new Mdl_UpData();
        //    upd.Sc_i_serialnumber = Sc_i_serialnumber;
        //    bool SetStateB = BSetState(PatientInfoList, upd);             //设置发卡状态
        //    return SetStateB;
        //}
        #endregion
        /// <summary>
        /// 设置发卡状态
        /// </summary>
        public bool BSetState(BindingList<Mdl_MPatientInfo> PatientInfoList)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            BarCodeResult = PatientInfoList[0].PI_V_CardCode;
            if (dvlTemp.fB_BSetState(2, StateSerialNumber))
            {
                //设置发卡状态并返回数据库修改结果
                dvlTemp.fB_BsetAdd(PatientInfoList);
                //发卡状态设置成功
                ChangeScoue(2, "VL_I_State");
                //BarCodeResult = iscardlegal.Fb_getBCR(SerialNumber, ProcessNumber);
                //BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                return true;
            }
            //发卡状态设置为假
            MessageBox.Show("发卡状态更新失败，请重试！");
            return false;
        }

        /// <summary>
        /// 设置发卡状态-------------ago
        /// </summary>
        public bool BSetState_ago(BindingList<Mdl_OFFLINE_PATIENT_INFOR> PatientInfoList)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            ProcessNumber = PatientInfoList[0].OV_V_ProcessNumber;
            if (dvlTemp.fB_BSetState_ago(2, StateSerialNumber))
            {
                //设置发卡状态并返回数据库修改结果
                dvlTemp.fB_BsetAdd_ago(PatientInfoList);
                //发卡状态设置成功
                ChangeScoue(2, "OV_I_State");              
                GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber);
                return true;
            }
            //发卡状态设置为假
            MessageBox.Show("发卡状态更新失败，请重试！");
            return false;
        }
        /// <summary>
        /// 根据就诊号/流水号/注册号点击查询按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ser_Click(object sender, RoutedEventArgs e)
        {
            IQuery = 0;

            try
            {
                if (keys.Text.Trim() == "" && keys1.Text.Trim() == "" && keys2.Text.Trim() == "")
                {
                    MessageBox.Show("必须输入一行数据！");
                    return;
                }

                if (keys.Text.Trim() != "")
                {
                    BarCodeResult = keys.Text.Trim().Substring(0, ps);
                    ProcessNumber = null;
                    SerialNumber = null;
                    if (GetGridData(BarCodeResult, SerialNumber, ProcessNumber))
                        return;
                }
                if (keys1.Text.Trim() != "")
                {
                    ProcessNumber = keys1.Text.Trim();
                    SerialNumber = null;
                    BarCodeResult = null;
                    if (GetGridData(BarCodeResult, SerialNumber, ProcessNumber))
                        return;
                }
                if (keys2.Text.Trim() != "")
                {
                    SerialNumber = keys2.Text.Trim();
                    ProcessNumber = null;
                    BarCodeResult = null;
                    GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("输入有误，检查号码是否正确！");

            }
            //判断就诊卡是否为合法的卡
        }
        #endregion

        #region 重置事件
        /// <summary>
        /// 重置发卡状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reststater_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            if (IQuery==0)
            {
                //MessageBox.Show("确认是否重置发卡状态，检查胶片状态！");
                int getJPstate = dvlTemp.fB_Bgetstate(StateSerialNumber);
                if (getJPstate == 3)
                {
                    MessageBox.Show("已经发过胶片，请勿重置！");
                    return;
                }

                if (dvlTemp.fB_BSetState(0, StateSerialNumber))
                {
                    //GetGridData(BarCodeResult,back);
                    ChangeScoue(0, "VL_I_State");
                    MessageBox.Show("重置发卡状态成功！");
                    ChangeControl();

                    //重置发卡状态成功
                }
                else
                {
                    //重置发卡状态失败
                    MessageBox.Show("重置发卡状态失败，请重试！");
                }
            }
            else if (IQuery==1)
            {
                //MessageBox.Show("确认是否重置发卡状态，检查胶片状态！");
                if (dvlTemp.fB_BSetState_ago(0, StateSerialNumber))
                {
                    //GetGridData(BarCodeResult,back);
                    ChangeScoue(0, "VL_I_State");
                    MessageBox.Show("重置发卡状态成功！");
                    ChangeControl();

                    //重置发卡状态成功
                }
                else
                {
                    //重置发卡状态失败
                    MessageBox.Show("重置发卡状态失败，请重试！");

                }
            }
           
        }
        /// <summary>
        /// 重置报告打印状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void restmedical_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<string> MedicalStateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            for (int i = 0; i < MedicalStateSerialNumber.Count; i++)
            {
                if (IQuery==0)
                {
                    if (dvlTemp.fB_BSetMedicalState(0, MedicalStateSerialNumber[i]))
                    {
                        ChangeScoue(0, "VL_I_MedicalState");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置报告打印状态成功！");
                        ChangeControl();
                        //报告打印状态重置成功
                    }
                    else
                    {
                        //报告打印状态重置失败
                        MessageBox.Show("重置报告打印状态失败，请重试！");
                    }
                }
                else if (IQuery ==1)
                {
                    if (dvlTemp.fB_BSetMedicalState_ago(0, MedicalStateSerialNumber[i]))
                    {
                        ChangeScoue(0, "VL_I_MedicalState");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置报告打印状态成功！");
                        ChangeControl();
                        //报告打印状态重置成功
                    }
                    else
                    {
                        //报告打印状态重置失败
                        MessageBox.Show("重置报告打印状态失败，请重试！");
                    }
                }
                
            }

        }
        /// <summary>
        /// 重置胶片打印状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void restsick_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            for (int i = 0; i < StateSerialNumber.Count; i++)
            {
                if (IQuery==0)
                {
                    if (dvlTemp.fB_BSetState(0, StateSerialNumber))
                    {
                        ChangeScoue(0, "VL_I_State");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置胶片打印状态成功！");
                        ChangeControl();
                        //胶片打印状态重置成功
                    }
                    else
                    {
                        //胶片打印状态重置为假
                        MessageBox.Show("重置胶片打印状态失败，请重试！");
                    }
                }
                else if (IQuery==1)
                {
                    if (dvlTemp.fB_BSetState_ago(0, StateSerialNumber))
                    {
                        ChangeScoue(0, "VL_I_State");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置胶片打印状态成功！");
                        ChangeControl();
                        //胶片打印状态重置成功
                    }
                    else
                    {
                        //胶片打印状态重置为假
                        MessageBox.Show("重置胶片打印状态失败，请重试！");
                    }
                }
                
            }

        }
        /// <summary>
        /// 重置贴纸打印状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reststate_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            List<string> SickStateSerialNumber = GetCheckBoxId();

            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            for (int i = 0; i < SickStateSerialNumber.Count; i++)
            {
                if (IQuery==0)
                {
                    if (dvlTemp.fB_BSetSickState(0, SickStateSerialNumber[i]))
                    {
                        ChangeScoue(0, "VL_I_SickState");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置贴纸打印状态成功！");
                        ChangeControl();
                        //贴纸打印状态重置成功
                    }
                    else
                    {
                        //贴纸打印状态重置为假
                        MessageBox.Show("重置贴纸打印状态失败，请重试！");
                    }
                }
                else if (IQuery==1)
                {
                    if (dvlTemp.fB_BSetSickState_ago(0, SickStateSerialNumber[i]))
                    {
                        ChangeScoue(0, "VL_I_SickState");
                        //GetGridData(BarCodeResult,back);
                        MessageBox.Show("重置贴纸打印状态成功！");
                        ChangeControl();
                        //贴纸打印状态重置成功
                    }
                    else
                    {
                        //贴纸打印状态重置为假
                        MessageBox.Show("重置贴纸打印状态失败，请重试！");
                    }
                }
               
            }

        }
        #endregion

        #region 设置及筛选
        /// <summary>
        /// 设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void options_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //打开设置窗体
            Options op = new Options();
            op.WindowStartupLocation = WindowStartupLocation.Manual;
            op.Left = 500;
            op.Top = 150;
            op.ShowDialog();
        }
        /// <summary>
        /// 未刻录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unrecord_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            StudyMethod.Text = "";
            all.IsChecked = false;
            recorded.IsChecked = false;
            Screen(BarCodeResult, "0,3");
        }
        /// <summary>
        /// 已刻录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recorded_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            StudyMethod.Text = "";
            all.IsChecked = false;
            unrecord.IsChecked = false;
            Screen(BarCodeResult, "1,2");
        }
        /// <summary>
        /// 全部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void all_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            StudyMethod.Text = "";
            recorded.IsChecked = false;
            unrecord.IsChecked = false;
            Screen(BarCodeResult, "0,1,2,3");
        }
        #endregion

        #region 更新、提取两个月前数据
        /// <summary>
        /// 更换卡片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updata_Click(object sender, RoutedEventArgs e)
        {
            //UpDataTimer();
            //OpenLonding();
            //_UpDataTime.Start();//加载定时器。调用更新数据时间。为了异步打开等待窗体，需要将事件写在计时器内
        }
        /// <summary>
        /// 向服务器发送提取两个月前指定就诊卡的部分报告信息指令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void twoMomthData_Click(object sender, RoutedEventArgs e)
        {
            IsTwoINFO = true;
            IQuery = 1;
            try
            {
                if (keys.Text.Trim() == "" && keys1.Text.Trim() == "" && keys2.Text.Trim() == "")
                {
                    MessageBox.Show("必须输入一行数据！");
                    return;
                }

                if (keys.Text.Trim() != "")
                {
                    BarCodeResult = keys.Text.Trim().Substring(0, ps);
                    ProcessNumber = null;
                    SerialNumber = null;
                    if (GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber))
                        return;
                }
                if (keys1.Text.Trim() != "")
                {
                    ProcessNumber = keys1.Text.Trim();
                    SerialNumber = null;
                    BarCodeResult = null;
                    if (GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber))
                        return;
                }
                if (keys2.Text.Trim() != "")
                {
                    SerialNumber = keys2.Text.Trim();
                    ProcessNumber = null;
                    BarCodeResult = null;
                    if (GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber))
                        return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("输入有误，请正确输入数字！");

            }
            //判断就诊卡是否为合法的卡






            ////BindingList<Mdl_MPatientInfo> BF = new BindingList<Mdl_MPatientInfo>();
            //Bll_Beforlnformation BBF = new Bll_Beforlnformation();
            //BarCodeResult = keys.Text;
            //BPGM = BBF.bf_Select(BarCodeResult);//向服务器发送提取两个月前指定就诊卡的报告信息指令

            //gridControl1.ItemsSource = BPGM;
            //DataContext = BPGM;
            //gridpage.ShowPages(gridControl1, Mdl.PageSqlHelp.ToDataTable(BPGM), sert);//给页面表单赋值新的数据
            //name.Content = gridControl1.GetCellValue(0, "PI_V_Name");
            //cardcode.Content = gridControl1.GetCellValue(0, "VL_PI_V_CardCode");
            //regdate.Content = gridControl1.GetCellValue(0, "VL_D_RegistrationDate");
            //statetwoMomethData.IsEnabled = true;
        }


        #region 提取两个月前数据-----------------------------------------------
        /// <summary>
        /// 给页面的GridControl绑定数据并处理显示样式
        /// </summary>
        public bool GetGridDataTwoMonth(string BarCodeResult, string SerialNumber, string ProcessNumber)
        {

            BPGMTwomonth = BPG.fM_SelectMonth(BarCodeResult, SerialNumber, ProcessNumber);//查询barCodeResult, SerialNumber, ProcessNumber就诊卡下的报告信息
            if (BPGMTwomonth == null)
            {
                MessageBox.Show("未查询到数据，数据库中没有两个月之前的报告记录！");
                return false;
            }
            if (BPGMTwomonth.Count() == 0)
            {
                MessageBox.Show("未查询到数据，数据库中没有两个月之前的报告记录！");
                return false;
            }
            else
            {
                gridControl1.ItemsSource = BPGMTwomonth;
                DataContext = BPGMTwomonth;
                gridpage.ShowPages(gridControl1, Mdl.PageSqlHelp.ToDataTable(BPGMTwomonth), sert);
                name.Content = gridControl1.GetCellValue(0, "PIO_V_Name");
                cardcode.Content = gridControl1.GetCellValue(0, "PIO_V_CardCode");
            }
            return true;
        }
        #endregion
        /// <summary>
        /// 发放指定就诊卡号的两月前的报告的完整信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void statetwoMomethData_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //List<string> SerileNumber = GetCheckBoxId();
            //Bll_Beforlnformation BBF = new Bll_Beforlnformation();
            ////Forms.MessageBox.Show("命令发送中，请稍后！");
            //if (BBF.AddTwoMomtherData(SerileNumber.ToString(), BarCodeResult))
            //{
            //    MessageBox.Show("提取指定的报告命令已发送至服务器，请提醒病人10分钟后去自助发卡终端或护士窗口重刷卡，发放优卡");
            //}
            //else
            //{
            //    MessageBox.Show("发送提取取报告命令失败，请检查网络状态后重试！");
            //}
        }
        #endregion


        /// <summary>
        ///为文件夹添加users，everyone用户组的完全控制权限
        /// </summary>
        /// <param name="dirPath"></param>
        static void AddSecurityControll2Folder(string dirPath)
        {
            //获取文件夹信息
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            //获得该文件夹的所有访问权限
            DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
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
        /// <summary>
        /// 关闭后台进程
        /// </summary>
        public void Killexe()
        {
            Process[] myprocess = Process.GetProcessesByName("SUNC_Main_DoctorProcess");
            if (myprocess.Length > 0)
            {
                myprocess[0].CloseMainWindow();
                myprocess[0].Kill();
            }
            Process[] myprocessVs = Process.GetProcessesByName("SUNC_Main_DoctorProcess.vshost");
            if (myprocessVs.Length > 0)
            {
                myprocessVs[0].CloseMainWindow();
                myprocessVs[0].Kill();
            }
        }


        public static string Gettime;

        /// <summary>
        /// 查询当日发卡数量按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {              
            if (caldate.SelectedDate==null)
            {
                MessageBox.Show("请选择日期");
            }
            else
            {
                DateTime? calendar = caldate.SelectedDate;
                string time = calendar.Value.ToString("yyyy-MM-dd");
                Gettime = time;
                Statistics stt = new Statistics();
                stt.ShowDialog();
            }
        }
        /// <summary>
        /// 一次性打印按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void print_Click(object sender, RoutedEventArgs e)
        {
            List<string> MedicalStateSerialNumber = GetCheckBoxId();
            if (MedicalStateSerialNumber.Count==0)
            {
                MessageBox.Show("请勾选一行！");
            }
            else
            {
                medical_ItemClick(null, null);
                sick_ItemClick(null, null);
            }          
        }
        /// <summary>
        /// 一次性全部重置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reset_Click(object sender, RoutedEventArgs e)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            if (IQuery ==0)
            {
                try
                {
                    #region ---------------重置发卡状态
                    List<string> CheckNumber = GetCheckBoxId();
                    Dal_DVisitList dvlTemp = new Dal_DVisitList();
                    int getJPstate = dvlTemp.fB_Bgetstate(CheckNumber);
                    if (CheckNumber.Count == 0)
                    {
                        reset.IsEnabled = false;
                    }
                    else
                    {
                        if (getJPstate == 3)
                        {
                            MessageBox.Show("已经发过胶片，请勿重置！");
                            return;
                        }

                        if (dvlTemp.fB_BSetState(0, CheckNumber))
                        {
                            ChangeScoue(0, "VL_I_State");
                           
                            //重置发卡状态成功
                        }
                        else
                        {
                            //重置发卡状态失败
                            MessageBox.Show("重置发卡状态失败，请重试！");
                        }
                        #endregion
                        #region ---------------重置报告状态
                        for (int i = 0; i < CheckNumber.Count; i++)
                        {
                            if (dvlTemp.fB_BSetMedicalState(0, CheckNumber[i]))
                            {
                                ChangeScoue(0, "VL_I_MedicalState");
                               
                                //报告打印状态重置成功
                            }
                            else
                            {
                                //报告打印状态重置失败
                                MessageBox.Show("重置报告打印状态失败，请重试！");
                            }
                        }
                        #endregion
                        #region ---------------重置胶片状态          
                        if (dvlTemp.fB_BSetState(0, CheckNumber))
                        {
                            ChangeScoue(0, "VL_I_State");
                           
                            //胶片打印状态重置成功
                        }
                        else
                        {
                            //胶片打印状态重置为假
                            MessageBox.Show("重置胶片打印状态失败，请重试！");
                        }
                        #endregion
                        #region ---------------重置贴纸状态
                        for (int i = 0; i < CheckNumber.Count; i++)
                        {
                            if (dvlTemp.fB_BSetSickState(0, CheckNumber[i]))
                            {
                                ChangeScoue(0, "VL_I_SickState");
                                
                                //贴纸打印状态重置成功
                            }
                            else
                            {
                                //贴纸打印状态重置为假
                                MessageBox.Show("重置贴纸打印状态失败，请重试！");
                            }
                        }
                        #endregion
                        MessageBox.Show("一键重置成功");
                        BarCodeResult = iscardlegal.Fb_getBCR(CheckNumber, ProcessNumber);
                        BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                        GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                        ChangeControl();
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("一键重置未成功");
                }
            }
            else if (IQuery==1)
            {
                restmedical_ItemClick(null, null);
                restmedical_ItemClick(null, null);
                restsick_ItemClick(null, null);
                reststate_ItemClick(null, null);
                BarCodeResult = iscardlegal.Fb_getBCR_agotwo(StateSerialNumber, ProcessNumber);
                BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber);
                ChangeControl();
            }

        }

        /// <summary>
        /// 今天发卡统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void todaysend_ItemClick(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// 胶片发放按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilmProcssing_ItemClick(object sender, RoutedEventArgs e)
        {
            List<string> StateSerialNumber = GetCheckBoxId();
            Dal_DVisitList dvlTemp = new Dal_DVisitList();
            if (StateSerialNumber.Count==0)
            {
                System.Windows.Forms.MessageBox.Show("请选择先勾选！");
            }
            else
            {
                if (IQuery == 0)
                {
                    if (dvlTemp.fB_BSetState(3, StateSerialNumber))
                    {
                        ChangeScoue(3, "VL_I_State");
                        //GetGridData(BarCodeResult,back);
                        System.Windows.Forms.MessageBox.Show("请指导病人取胶片");
                        //胶片打印状态设置成功
                    }
                    else
                    {
                        //胶片打印状态设置为假
                        System.Windows.MessageBox.Show("记录修改失败，请重试！");
                    }
                    BarCodeResult = iscardlegal.Fb_getBCR(StateSerialNumber, ProcessNumber);
                    BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                    GetGridData(BarCodeResult, SerialNumber, ProcessNumber);
                    ChangeControl();
                }
                else if (IQuery == 1)
                {

                    if (dvlTemp.fB_BSetState_ago(3, StateSerialNumber))
                    {
                        ChangeScoue(3, "VL_I_State");
                        //GetGridData(BarCodeResult,back);
                        System.Windows.Forms.MessageBox.Show("请指导病人取胶片");
                        //胶片打印状态设置成功
                    }
                    else
                    {
                        //胶片打印状态设置为假
                        System.Windows.MessageBox.Show("记录修改失败，请重试！");
                    }
                    BarCodeResult = iscardlegal.Fb_getBCR_agotwo(StateSerialNumber, ProcessNumber);
                    BarCodeResult = JsonConvert.DeserializeObject(BarCodeResult).ToString();
                    GetGridDataTwoMonth(BarCodeResult, SerialNumber, ProcessNumber);
                }
            }          
        }
        //发卡大按钮
        private void sendcard_Click(object sender, RoutedEventArgs e)
        {
            stater_ItemClick(null, null);
        }

        private void keys_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.Return))
            {
                ser_Click(null, null);
            }
        }
    }
    /// <summary>
    /// 表格根据列值改变行颜色
    /// </summary>
    public class Conv : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                //if (!(value is int)) return new SolidColorBrush(Colors.White);
                int OjValueI = 0;
                try
                {
                    OjValueI = (int)value;
                }
                catch (Exception)
                {

                }
                if (OjValueI == 1) return new SolidColorBrush(Color.FromRgb(175, 240, 178));
                return new SolidColorBrush(Colors.Gray);
                // return new SolidColorBrush(Colors.White);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

