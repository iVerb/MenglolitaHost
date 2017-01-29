#region CopyRight
//Host更新工具 by 北斗家老司机
#endregion CopyRight

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MenglolitaHost
{
    public partial class MainForm : Form
    {
        //private static String _url = "https://hosts-smounives.rhcloud.com/hosts";
        //https://coding.net/u/scaffrey/p/hosts/git/blob/master/hosts
        //https://raw.githubusercontent.com/racaljk/hosts/master/hosts
        private static String _url = "https://raw.githubusercontent.com/racaljk/hosts/master/hosts";
        //https://github.com/racaljk/hosts/blob/master/hosts";
        private static String _path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
        private Thread _thread = null;
        private System.Timers.Timer _timer = new System.Timers.Timer(40 * 1000);

        public MainForm()
        {
            InitializeComponent();
            _timer.AutoReset = false;
            _timer.Enabled = false;
            _timer.Elapsed += TimeOut;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            if (_thread != null)
            {
                return;
            }

            _thread = new Thread(new ThreadStart(DoUpdate));
            _thread.Start();
            _timer.Enabled = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(Path + @"\" + "yoursHosts"))
            {
                testhoost.Enabled = true;
                //hostlk.Enabled = false;
            }
            else
            {
                testhoost.Enabled = false;
                hostlk.Enabled = true;
            }

            if (Directory.Exists(Path + @"\" + "Hostbak"))
            {
                restore.Enabled = true;
            }
            else
            {
               restore.Enabled = false;
            }
         
            if (File.Exists(Path + "Mhost.sl"))
            {
                zhuabao.Enabled = true;
                WinPcap.Enabled = true;
            }
            else
            {
                zhuabao.Enabled = false;
                WinPcap.Enabled = false;
            }
            //检测授权文件是否存在并且启用

            if (!IsAdministrator())
            {
                MessageBox.Show("请右键管理员运行", "出错了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //panelUAC.Dock = DockStyle.Fill;
                //panelUAC.BringToFront();
                //panelUAC.Visible = true;
                Application.Exit();
            }
            else
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    this.Text += "--" + "hosts更新工具";
                }

                MakeWriteable(_path);
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_thread != null && _thread.IsAlive)
            {
                try
                {
                    _thread.Abort();
                }
                catch { }
            }
        }

        private void btnStartWithAdmin_Click(object sender, EventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = Application.StartupPath;
            processStartInfo.FileName = Path.GetFileName(Application.ExecutablePath);
            processStartInfo.Verb = "runas";
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1)
            {
                String text = "";
                for (Int32 i = 1; i < commandLineArgs.Length; i++)
                {
                    text += commandLineArgs[i];
                }
                processStartInfo.Arguments = text;
            }
            try
            {
                Process.Start(processStartInfo);
                this.Close();
            }
            catch
            {
                this.Close();
            }
        }

        private void DoUpdate()
        {
            try
            {
                this.SafeInvoke(() => { this.btnUpdate.Text = "正在更新..."; });
                var req = (HttpWebRequest)WebRequest.Create(_url);
                req.ServicePoint.Expect100Continue = false;
                req.Method = "GET";
                req.KeepAlive = true;
                req.UserAgent = "Menglolita Host 1.3";
                req.Timeout = 30 * 1000;

                // 以字符流的方式读取HTTP响应
                using (var rsp = (HttpWebResponse)req.GetResponse())
                {
                    var encoding = Encoding.GetEncoding(rsp.CharacterSet);
                    using (var stream = rsp.GetResponseStream())
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        var content = reader.ReadToEnd();
                        File.WriteAllText(_path, content, Encoding.UTF8);
                        MessageBox.Show("更新成功", "已完工", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnUpdate.Enabled = true;
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TM出错了", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _thread = null;
                _timer.Enabled = false;
                this.SafeInvoke(() => { this.btnUpdate.Text = "更新已结束"; });
            }
        }

        void TimeOut(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_thread != null && _thread.IsAlive)
            {
                if (MessageBox.Show("TM！看起来超时了！是否终止！", "你不要搞事情", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    try
                    {
                        this.SafeInvoke(() => { this.btnUpdate.Text = "更新已结束"; });
                        _thread.Abort();
                    }
                    catch { }
                }
                else
                {
                    _timer.Start();
                }
            }
        }

        public static Boolean IsAdministrator()
        {
            Boolean result;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
                result = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                result = true;
            }

            return result;
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName("MLSniffer");
            foreach (System.Diagnostics.Process p in process)
            {
                p.Kill();
            }*/
            Process[] thepro = Process.GetProcessesByName("MLSniffer");
            if (thepro.Length > 0)
            //如果进程曾在或者不止一个 
            {
                //逐个结束 
                for (int i = 0; i < thepro.Length; i++)
                {
                    //如果还没有结束就关闭他 
                    if (!thepro[i].CloseMainWindow()) thepro[i].Kill();
                }
            }
           //從輸出流取得命令執行結果
            Process[] process = Process.GetProcesses();
            foreach (Process prc in process)
            {
                Console.WriteLine(prc.ProcessName);
                if (prc.ProcessName == "MLSniffer")
                    //if (prc.ProcessName == "MLSniffer")
                    prc.Kill();
            }
            Console.ReadLine();
        }
        private static void MakeWriteable(String fileName)
        {
            if (File.Exists(fileName))
            {
                FileSystemInfo info = new FileInfo(fileName);
                if (info != null)
                    info.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        private void bakhost_Click(object sender, EventArgs e)
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            string Path2 = AppDomain.CurrentDomain.BaseDirectory + "Hostbak";
            if (!System.IO.Directory.Exists(Path + @"\" + "Hostbak"))
            {
                // 目录不存在，建立目录
                System.IO.Directory.CreateDirectory(Path + @"\" + "Hostbak");
            }
            String sourcePath = "C:\\Windows\\System32\\drivers\\etc\\hosts"; ;
            String targetPath = Path2 + @"\" + "hosts";
            bool isrewrite = true; //覆盖已存在的同名文件,false则反之
            System.IO.File.Copy(sourcePath, targetPath, isrewrite);
            MessageBox.Show("Host备份完成\r\n备份所在位置：" + Path2 + "\r\nHosts默认位置：C:\\Windows\\System32\\drivers\\etc", "你尽情的搞事情吧", MessageBoxButtons.OK, MessageBoxIcon.Information);
            restore.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path + "Mhost.sl"))
            {
                this.Text = "（" + "当前为开发者模式" + ")"; ;
            }
            else
            {
                MessageBox.Show("这个功能用不了哦", "文件...好像不存在", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            //启动抓包工具
            if (File.Exists(Path + "bin" + @"\" + "MLSniffer.exe"))
            {
                Process proc = new Process();
                proc.StartInfo.FileName = Path + "bin" + @"\" + "MLSniffer.exe";
                proc.Start();
            }
            else
            {
                MessageBox.Show("这个功能用不了哦", "文件...好像不存在", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void qh_Click(object sender, EventArgs e)
        {
            Form aptget = new aptget();
            aptget.ShowDialog();
        }

        private void testhoost_Click(object sender, EventArgs e)
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            string Path2 = AppDomain.CurrentDomain.BaseDirectory + "yoursHosts";
            if (File.Exists(Path2 + @"\" + "hosts"))
            {
                String winhosts = "C:\\Windows\\System32\\drivers\\etc\\hosts"; ;
                String yourshosts = Path2 + @"\" + "hosts";
                bool isrewrite = true; //覆盖已存在的同名文件,false则反之
                System.IO.File.Copy(yourshosts, winhosts, isrewrite);
                MessageBox.Show("Hosts写入完成，看看能不能正常工作吧", "搞定", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("你TM在搞事情吗？\r\n哪来的hosts文件\r\n当我瞎吗", "年轻人不要搞事情", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void restore_Click(object sender, EventArgs e)
        {
            /*string Path = AppDomain.CurrentDomain.BaseDirectory;
            string Path2 = AppDomain.CurrentDomain.BaseDirectory + "Hostbak";
            if (!System.IO.Directory.Exists(Path + @"\" + "Hostbak"))
            {
                // 目录不存在，建立目录
                MessageBox.Show("备份的文件被你吃了嘛", "你别骗我");
            }
            String oldPath = "C:\\Windows\\System32\\drivers\\etc\\hosts"; ;
            String bakPath = Path2 + @"\" + "hosts";
            bool isrewrite = true; //覆盖已存在的同名文件,false则反之
            System.IO.File.Copy(bakPath, oldPath, isrewrite);
            MessageBox.Show("Hosts恢复完成", "又和以前一样啦");*/
            Form restore = new restore();
            restore.ShowDialog();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("这是个很危(wu)险(liao)的操作\r\n该操作不可逆", "年轻人不要想着搞个大新闻", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            string Path2 = AppDomain.CurrentDomain.BaseDirectory + "yoursHosts";
            if (!System.IO.Directory.Exists(Path + @"\" + "yoursHosts"))
            {
                // 目录不存在，建立目录
                System.IO.Directory.CreateDirectory(Path + @"\" + "yoursHosts");
                testhoost.Enabled = true;
                System.Diagnostics.Process.Start(Path2);
            }
            else
            {
                System.Diagnostics.Process.Start(Path2);
            }
            
        }

        private void label6_Click(object sender, EventArgs e)
        {
            MessageBox.Show("感谢参与这个工具的所有人", "程序基于GPLv3协议发布");
        }

        private void WinPcap_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            //安装WinPcap驱动用于访问底层网络
            if (File.Exists(Path + "Confion" + @"\" + "WinPcap.exe"))
            {
                Process proc = new Process();
                proc.StartInfo.FileName = Path + "Confion" + @"\" + "WinPcap.exe";
                proc.Start();
            }
            else
            {
                MessageBox.Show("你肯定把这东西吃了", "你就等着用不了吧", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {
            MessageBox.Show("这TM绝对是最后一个版本\r\n不是我就是*(bi)*(bi)", "说不定我马上就反悔了");
        }
    }

        public static class ControlExtention
        {
            public delegate void InvokeHandler();

            public static void SafeInvoke(this Control control, InvokeHandler handler)
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(handler);
                }
                else
                {
                    handler();
                }
            }
        }
    }