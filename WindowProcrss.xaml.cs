using Base.Common;
using SUNC_Main_DoctorProcess.BLL;
using SUNC_Main_DoctorProcess.DAl;
using SUNC_Main_DoctorProcess.MDL;
using SUNC_Main_DoctorProcess.TheThirdPart;
using SUNC_NCL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// windowjindutiao.xaml 的交互逻辑
    /// </summary>
    public partial class WindowProcrss : Window
    {
        HttpClientHelper httpClientHelper = new HttpClientHelper();
        public bool SendCardB = false;
        HttpWebResponseHelper httpWebResponseHelper = new HttpWebResponseHelper();
        /// <summary>
        /// 就诊卡号
        /// </summary>
        public string  BarCodeResult { get; set; }
        /// <summary>
        /// 流水号
        /// </summary>
        public string ProcessNumber{ get; set; }
        /// <summary>
        /// 注册号
        /// </summary>
        public List<string> SerialNumber {  get; set; }



        #region //----------------------------------------------------
        private static int FileListCount = 0;
        /// <summary>
        /// 源文件路径
        /// </summary>
        private static string sourcePath = string.Empty;
        /// <summary>
        /// 目标文件路径
        /// </summary>
        private static string desPath = string.Empty;

        private static string Copying = string.Empty;

        private static int ProgressVa = 0;
        private static string descfile = "";
        private static string comfile = "";
        private static string filename = "";
        #endregion

        public WindowProcrss()
        {
            InitializeComponent();
          

        }
        
       
        //委托事件CopyFile
        public delegate bool CopyFile(string BarCodeResult, List<string> SerialNumber);
        //委托事件Closefrom
        public delegate void CloseFormDelegate();




        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private void Window_Loaded(object sender, RoutedEventArgs e)
        { 
            //窗体按钮的隐藏
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            //异步处理
            BegionProcess();
        }

        private void BegionProcess()
        {
            CopyFile CopyFileD = new CopyFile(SendCardAndCopyDate);
            IAsyncResult result = CopyFileD.BeginInvoke(BarCodeResult.ToString(), SerialNumber, null, null);
            progressBar1.Value = 2;
            new Thread(new ThreadStart(() => {

                while (!result.IsCompleted)
                {
                    Thread.Sleep(50);
                    ProgressVa++;
                    this.Dispatcher.Invoke(() =>
                    {
                        label2.Content = $"文件总数：1";
                        progressBar1.Maximum = 500;
                        label3.Content = $"从："+ descfile;
                        label4.Content = $"到："+ comfile;
                        label1.Content = $"正在复制的文件："+ filename;
                        label5.Content = $"当前时间：{ DateTime.Now.ToString(" HH:mm:ss")}";
                        progressBar1.Value = ProgressVa;
                        if (ProgressVa==500)
                        {
                            ProgressVa = 0;
                        }
                    });
                }
                if (CopyFileD.EndInvoke(result))
                {
                    SendCardB = true;
                    MessageBox.Show("发卡成功!");
                }
                this.Dispatcher.Invoke(() => { this.Close(); });
            })).Start();
        }

        public bool SendCardAndCopyDate(string BarCodeResult, List<string> SerialNumber)
        {
            BLL.Bll_TBL_B_VisitList bPatientInfo = new BLL.Bll_TBL_B_VisitList();
            BindingList<MDL.Mdl_MPatientInfo> PatientInfoList = new BindingList<Mdl_MPatientInfo>();
            if (MainWindow.IQuery==0)
            {
                //获得查询结果
                 PatientInfoList = bPatientInfo.fM_SelectSerialNumber(BarCodeResult, SerialNumber);
            }
            else
            {
                BindingList<Mdl_OFFLINE_PATIENT_INFOR> PatientInfoList_ago = bPatientInfo.fM_SelectSerialNumber_ago(BarCodeResult, SerialNumber);
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
            }          
            Task<bool> bXml = f_ReturnsAllInformation(PatientInfoList);//生成XML报告并保存      
            Task<bool> bCopyPicture = CopyPicture(PatientInfoList); //拷贝图片
            Task.WaitAll();
            if (!bXml.Result || !bCopyPicture.Result)
            {
               // MessageBox.Show("U卡数据拷贝出现问题！检查是否插入卡！请重新插入U卡！");
                return false;
            }
            return true;
        }


        /// <summary>
        /// 生成XML报告并保存
        /// </summary>
        /// <returns></returns>
        private async Task<bool> f_ReturnsAllInformation(BindingList<MDL.Mdl_MPatientInfo> PatientInfoList)
        {
                     
            Ttp_TCreateXmlFile CXF = new Ttp_TCreateXmlFile();
            //生成XML报告
            bool CopyXmlB = await CXF.XmlFileAsync(PatientInfoList);
            ////复制DCOM图片
            if (CopyXmlB) return true;
            return false;
     
      }
        /// <summary>
        /// MD5码
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        private static string getMD5Hash(string pathName)
        {
            string strResult = "";
            string strHashData = "";

            byte[] arrbytHashValue;
            System.IO.FileStream oFileStream = null;
            System.Security.Cryptography.MD5CryptoServiceProvider oMD5Hasher = new System.Security.Cryptography.MD5CryptoServiceProvider();

            try
            {
                oFileStream = new System.IO.FileStream(pathName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                arrbytHashValue = oMD5Hasher.ComputeHash(oFileStream);//计算指定Stream 对象的哈希值
                oFileStream.Close();
                //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”
                strHashData = System.BitConverter.ToString(arrbytHashValue);
                //替换-
                strHashData = strHashData.Replace("-", "");
                strResult = strHashData;
            }
            catch (System.Exception ex)
            {
                return ex.ToString();
            }

            return strResult;
        }
        /// <summary>
        /// 拷贝图片
        /// </summary>
        /// <param name="PatientInfoList"></param>
        public async Task<bool> CopyPicture(BindingList<MDL.Mdl_MPatientInfo> mPatientInfo)
        {
            bool bif = false;
            BLL.BLL_BIsEnough bEnough = new BLL.BLL_BIsEnough();
            string Reel = bEnough.f_listFiles();
            List<MDL.Mdl_MPatientInfo> PatientInfoList1 = new List<MDL.Mdl_MPatientInfo>();
            MDL.Mdl_MPatientInfo mPatientInfoll = new MDL.Mdl_MPatientInfo();
            string FileFath = Directory.GetCurrentDirectory() + "\\INIConfig\\OptionsSave.ini";
            INIClass iniClass = new INIClass(FileFath);
            if (string.IsNullOrEmpty(Reel))
            {
                return false;
            }
            try
            {
                string Md5result;
                if (!MainWindow.IsTwoINFO)
                {
                    for (int i = 0; i < mPatientInfo.Count; i++)
                    {
                       
                        #region ---------压缩文件返回MD5码
                        try
                        {
                            filename ="......";
                            descfile = "正在压缩，请等待";
                            comfile = "正在压缩，请等待";
                            string CompressUrl = string.Format("api/File/GetCompress_bool?&ProcessNumber={0}", mPatientInfo[i].VL_V_ProcessNumber);
                            Md5result = httpClientHelper.GetResponse(CompressUrl);
                            if (Md5result == "false")
                            {
                                return false;
                            }                         
                        }
                        catch (Exception ex)
                        {
                            return false;
                        }
                        #endregion
                        #region---------下载文件
                        string url = @"api/File/GetDownload_bool";
                        descfile = url;
                        string ProcessNumber = $"'{mPatientInfo[i].VL_V_ProcessNumber}'";
                        filename = mPatientInfo[i].VL_V_ProcessNumber + ".zip";

                        string UCardName = Tool.UCardCheck.UCardName;//优盘识别
                        if (string.IsNullOrEmpty(UCardName))
                        {
                            return false;
                        }
                        try
                        {
                            //优盘中文件下载位置
                            string localfile = $"{UCardName}Procedure\\IMAGE\\DICOM\\{mPatientInfo[i].VL_V_MODALITY}\\{mPatientInfo[i].VL_V_ProcessNumber}.zip";
                            comfile = localfile;
                            System.Net.HttpWebResponse response = httpWebResponseHelper.HttpPost(url, ProcessNumber);                          
                            //检查是否存在文件夹
                            string localfilePath = $"{UCardName}Procedure\\IMAGE\\DICOM\\{mPatientInfo[i].VL_V_MODALITY}";
                            if (false == System.IO.Directory.Exists(localfilePath))
                            {
                                //创建文件夹
                                System.IO.Directory.CreateDirectory(localfilePath);
                            }
                            using (Stream readStream = response.GetResponseStream())
                            {
                                bool b = Download(localfile, response.ContentLength, readStream);
                                string md5F = getMD5Hash(localfile);
                                if (md5F == Md5result)
                                {
                                }
                                else
                                {
                                    b = Download(localfile, response.ContentLength, readStream);
                                    string md5S = getMD5Hash(localfile);
                                    if (md5S != Md5result)
                                    {
                                        MessageBox.Show("校验出错,请重试!");
                                        return false;
                                    }
                                }
                                readStream.Close();
                            }
                            response.Close();
                        }
                        catch (Exception ex)
                        {
                            return false;
                        }

                        #endregion
                        #region ---------删除文件
                        try
                        {
                            ProgressVa = 500;
                            string deleteUrl = string.Format("api/File/GetDelete_file?&ProcessNumber={0}", mPatientInfo[i].VL_V_ProcessNumber);
                            string  deledetbool =httpClientHelper.GetResponse(deleteUrl);
                            return Convert.ToBoolean( deledetbool);
                        }
                        catch (Exception ex)
                        {
                            return false;
                        }
                        #endregion
                    }
                }
                else
                {
                    try
                    {
                        //FPT文件下载
                        for (int i = 0; i < mPatientInfo.Count; i++)
                        {
                            PatientInfoList1.Add(mPatientInfo[i]);
                            foreach (var item in PatientInfoList1)
                            {
                                //ProgressVa = 3;
                                filename = item.VL_V_ProcessNumber + ".zip";
                               // mPatientInfoll.VL_V_PathNO = item.VL_V_PathNO;
                                string compresss = iniClass.IniReadValue("DownloadPath", "DownloadPathL");
                                //图片保存的路径
                                string downloadpath = Directory.GetCurrentDirectory() + compresss;

                                DateTime? timedate = item.VL_D_StudyDate;
                                string time = timedate.Value.ToString("yyyyMMdd");
                                string mddality = item.VL_V_MODALITY;
                                string ftp = iniClass.IniReadValue("PACSPicutrePath", "URI");
                                string downlodepath = ftp + "/" + mddality + "/" + time;
                                mPatientInfoll.VL_V_PathNO = System.IO.Path.Combine(downlodepath, item.VL_V_ProcessNumber);
                                string DesPath = string.Format("{0}\\{1}\\{2}\\", downloadpath, item.VL_V_MODALITY, item.VL_V_ProcessNumber);
                                descfile = mPatientInfoll.VL_V_PathNO;
                                if (false == System.IO.Directory.Exists(DesPath))
                                {
                                    //创建文件夹
                                    System.IO.Directory.CreateDirectory(DesPath);
                                }
                                comfile = DesPath;
                                bool PictureCopyB = await PictureCopy(mPatientInfoll.VL_V_PathNO, DesPath, item.VL_V_ProcessNumber, downlodepath);
                                
                                if (!PictureCopyB) return false;

                                //压缩文件
                                FileSystemInfo[] fileinfo = new DirectoryInfo(DesPath).GetFileSystemInfos();
                                List<string> sourceFileList = new List<string>();
                                foreach (FileSystemInfo il in fileinfo)
                                {
                                    sourceFileList.Add(il.FullName);
                                }
                                //CompressHelper ch = new Base.Common.CompressHelper();
                                string TopPathName = iniClass.IniReadValue("DesPathName", "PathName");
                                if (!Directory.Exists(TopPathName))
                                {
                                    Directory.CreateDirectory(TopPathName);
                                }
                                string filenameq = TopPathName + "\\" + item.VL_V_ProcessNumber + ".zip";
                                //ch.CompressMulti(sourceFileList.ToArray(), filenameq);
                                ZipFloClass Zc = new ZipFloClass();
                                //Base.Log.Log4Net.GetLog4Net().WriteLog("DEBUG", "FileURL:" + filename);
                                comfile = filenameq;
                                Zc.ZipFile(DesPath, filenameq);
                                
                                new System.Threading.Thread(delegate () { Directory.Delete(DesPath, true); }).Start();//删除下载的dicm文件

                                //复制文件
                                string UDesPath = string.Format("{0}:\\Procedure\\IMAGE\\DICOM\\{1}\\", Reel, item.VL_V_MODALITY);
                                comfile = UDesPath;
                                if (!Directory.Exists(UDesPath))
                                {
                                    Directory.CreateDirectory(UDesPath);
                                }
                                DirectoryInfo dir = new DirectoryInfo(TopPathName);
                                FileSystemInfo[] fileinfolll = dir.GetFileSystemInfos();
                                foreach (FileSystemInfo ippp in fileinfolll)
                                {
                                    //不是文件夹即复制文件，true表示可以覆盖同名文件                                   
                                    File.Copy(ippp.FullName, UDesPath + "\\" + ippp.Name, true);
                                    ProgressVa = 500;
                                }
                                new System.Threading.Thread(delegate () { File.Delete(filenameq); }).Start();//删除压缩文件
                            }
                        }
                        bif = true;
                    }
                    catch (Exception ex)
                    {
                        bif = false;
                    }
                }             
                return true;
            }
            catch (Exception e)
            { 
                return bif;
            }
        }
        private bool Download(string localfile, long remoteFileLength, Stream readStream)
        {
            bool flag = false;
            long startPosition = 0; // 上次下载的文件起始位置
            FileStream writeStream; // 写入本地文件流对象

            if (remoteFileLength == 745)
            {
                System.Console.WriteLine("远程文件不存在.");
                return false;
            }

            // 判断要下载的文件夹是否存在
            if (File.Exists(localfile))
            {
                writeStream = File.OpenWrite(localfile);             // 存在则打开要下载的文件
                startPosition = writeStream.Length;                  // 获取已经下载的长度

                if (startPosition >= remoteFileLength)
                {
                    System.Console.WriteLine("本地文件长度" + startPosition + "已经大于等于远程文件长度" + remoteFileLength);
                    writeStream.Close();
                    return false;
                }
                else
                {
                    writeStream.Seek(startPosition, SeekOrigin.Current); // 本地文件写入位置定位
                }
            }
            else
            {
                writeStream = new FileStream(localfile, FileMode.Create);// 文件不保存创建一个文件
                startPosition = 0;
            }


            try
            {
                byte[] btArray = new byte[512];// 定义一个字节数据,用来向readStream读取内容和向writeStream写入内容
                int contentSize = readStream.Read(btArray, 0, btArray.Length);// 向远程文件读第一次

                long currPostion = startPosition;

                while (contentSize > 0)// 如果读取长度大于零则继续读
                {
                    currPostion += contentSize;
                    //int percent = (int)(currPostion * 100 / remoteFileLength);
                    //System.Console.WriteLine("percent=" + percent + "%");

                    writeStream.Write(btArray, 0, contentSize);// 写入本地文件
                    contentSize = readStream.Read(btArray, 0, btArray.Length);// 继续向远程文件读取
                }

                //关闭流
                writeStream.Close();
                readStream.Close();

                flag = true;        //返回true下载成功
            }
            catch (Exception ex)
            {
                writeStream.Close();
                flag = false;       //返回false下载失败
            }

            return flag;
        }

        bool BStop = false;
        private void fV_FtpCopyFile(string URI, string serialnumber, string DesPath)
        {
            FtpHelper.fv_SetLink(URI);
            string[] SrgDirList = FtpHelper.GetDirectoryList();

            foreach (string item in SrgDirList)
            {
                if (item==serialnumber)
                {
                    //下载文件
                    if (!Directory.Exists(DesPath)) Directory.CreateDirectory(DesPath);
                    //File.Copy(item.Key, desPath + item.Value, true);

                    List<string> SrgFileList = FtpHelper.GetFile(URI+"/"+ item);
                    FileListCount = SrgFileList.Count;
                    int ICountFile = 0;
                    foreach (string itemFile in SrgFileList)
                    {
                        //ProgressVa = ICountFile;
                        FtpHelper.Download(DesPath, URI + "/" + item, itemFile);
                        ICountFile++;
                    }
                    BStop = true;
                    return;
                    
                }

                if(item !="" && !BStop) FtpHelper.fv_SetLink(URI+"/"+item);
                if (FtpHelper.GetDirectoryList().Length>0 && item!="" && !BStop)
                {
                    fV_FtpCopyFile(URI + "/" + item,  serialnumber, DesPath);
                }

            }


        }

        /// <summary>
        /// 复制文件夹下面的图片
        /// </summary>
        /// <param name="path"></param>
        /// <param name="desPath"></param>
        private async Task<bool> PictureCopy(string path, string DesPath,string serialnumber, string VL_V_PathNO)
        {
           
            bool CopyStaeB = await Task.Run(() => {

                if (!Directory.Exists(path))
                {
                    //到FTP下载
                    string FileFath = Directory.GetCurrentDirectory() + "\\INIConfig\\OptionsSave.ini";
                    INIClass iniClass = new INIClass(FileFath);
                    string URI = VL_V_PathNO; //iniClass.IniReadValue("PACSPicutrePath", "URI");
                    string USERID = iniClass.IniReadValue("PACSPicutrePath", "USERID");
                    string PASSWORD = iniClass.IniReadValue("PACSPicutrePath", "PASSWORD");

                    //string ftpPath = path.Replace("Z:\\", "").Replace(@"\", "/");

                    FtpHelper.fv_SetLink(URI, USERID, PASSWORD);
                    //判断FTP上是否存在目录
                    //if (!FtpHelper.ftpIsExistsFile(ftpPath, URI))
                    //{ return false; }

                    //下载文件
                    fV_FtpCopyFile(URI, serialnumber, DesPath);
                }
                else
                {
                    //服务器下载

                    int i = 0;
                    ProgressVa = 0;
                    Dictionary<string, string> FileList = GetFile(path);
                    FileListCount = FileList.Count;
                    sourcePath = path;
                    desPath = DesPath;
                    foreach (var item in FileList)
                    {
                        Copying = item.Value;
                        try
                        {
                            if (!Directory.Exists(desPath)) Directory.CreateDirectory(desPath);
                            File.Copy(item.Key, desPath + item.Value, true);
                        }
                        catch (Exception ex)
                        {
                            i++;
                            if (i > 5) return false;
                        }
                        ProgressVa++;
                    }
                }
                return true;
            });
        
            return CopyStaeB;         
        }


        /// <summary>
        /// 添加文件路径到列表
        /// </summary>
        /// <param name="SourcePath"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetFile(string SourcePath)
        {
            Dictionary<string, string> FileList = new Dictionary<string, string>();
            DirectoryInfo dir = new DirectoryInfo(SourcePath);
            FileInfo[] fil = dir.GetFiles();
            foreach (FileInfo f in fil)
            {
                FileList.Add(f.FullName, f.Name);//添加文件路径到列表中  
            }
            return FileList;
        }

       
    }
}

