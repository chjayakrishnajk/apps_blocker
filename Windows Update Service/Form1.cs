using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace w10us
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public Form1()
        {
            InitializeComponent();

            int id = 0;     
            RegisterHotKey(this.Handle, id, 6, Keys.Z.GetHashCode()); 
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                Form form = new Form();              
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       
                int id = m.WParam.ToInt32();                                       
                if(this.Opacity > 0.5)
                {
                    this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
                    this.Opacity = 0;
                    this.Hide();
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.Opacity = 1;
                    this.Show();
                }
            }
        }
        public bool work = true;
        public List<keyword> get_words()
        {
            var list = new List<keyword>();
            try
            {
                var lines = File.ReadAllLines("keywords.sys");
                foreach (var line in lines)
                {
                    string[] ssize = line.Split('|');
                    list.Add(new keyword { keyword_name = ssize[0], start = ssize[1], end = ssize[2], allow_block = ssize[3] });
                }
            }
            catch (Exception)
            {
            }
            return list;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            try
            {
                try
                {
                    File.Delete("keywords.txt");
                }
                catch
                {

                }
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\OurSettings", true);
                    if (key != null)
                    {
                        if (key.GetValue("Stop_Work").ToString() == "no")
                        {
                            work = true;
                            button3.Text = "Stop";
                        }
                        else
                        {
                            work = false;
                            button3.Text = "Start";
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex != null)
                    {
                        work = true;
                        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OurSettings");
                        key.SetValue("Stop_Work", "no");
                        key.Close();
                    }
                }
                Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.SetValue("w10us", Application.ExecutablePath.ToString());
                try
                {
                    var lines = File.ReadAllLines("url.sys");
                    textBox2.Text = lines[0];
                }
                catch
                {
                }
                comboBox1.SelectedIndex = 0;
                var list = get_words();
                List<string> arr = new List<string>();
                for (int i = 0; i <= list.Count - 1; ++i)
                {
                    arr.Add(list[i].keyword_name + "|" + list[i].start + "|" + list[i].end + "|" + list[i].allow_block);
                }
                richTextBox1.Lines = arr.ToArray();               
                File.WriteAllLines("testing.txt", arr);              
                Thread TMM = new Thread(Check_Keywords);
                TMM.SetApartmentState(ApartmentState.STA);
                CheckForIllegalCrossThreadCalls = false;
                TMM.Start();
                Thread THM = new Thread(Monitor_Exit);
                THM.SetApartmentState(ApartmentState.STA);
                CheckForIllegalCrossThreadCalls = false;
                THM.Start();
                Thread TOM = new Thread(check_txt);
                TOM.SetApartmentState(ApartmentState.STA);
                CheckForIllegalCrossThreadCalls = false;
                TOM.Start();               
            }
            catch(Exception ex)
            {
                add_text_last(ex.ToString());
            }
            SendKeys.SendWait("^(+(Z))");
            Thread.Sleep(50);
            SendKeys.SendWait("^(+(Z))");
        }
        private void add_text_last(string text)
        {
            string filename = "testing.txt";
            string readText = File.ReadAllText(filename);
            File.WriteAllText(filename, readText + text);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);
        private static string GetCaptionOfActiveWindow()
        {
            var strTitle = string.Empty;
            var handle = GetForegroundWindow();
            var intLength = GetWindowTextLength(handle) + 1;
            var stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }
            return strTitle;
        }
        private void Monitor_Exit()
        {
            while (true)
            {
                Thread.Sleep(50);
                while (work)
                {
                    Thread.Sleep(1000);
                    var keywords = get_words();
                    var app_name = GetCaptionOfActiveWindow();
                    foreach (var item in keywords)
                    {
                        //if (item.allow_block == "Block") bug
                        if(true)
                        {
                            if (app_name.ToLower().Contains(item.keyword_name.ToLower()))
                            {
                                DateTime now = DateTime.Now;
                                DateTime start = DateTime.Now;
                                DateTime.TryParseExact(item.start, "HH:mm",
             CultureInfo.InvariantCulture,
             DateTimeStyles.None, out start);
                                DateTime end = DateTime.Now;
                                DateTime.TryParseExact(item.end, "HH:mm",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None, out end);
                                if (now.TimeOfDay > start.TimeOfDay && now.TimeOfDay < end.TimeOfDay)
                                {
                                    exit_app(item.keyword_name);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void exit_app(string app_name)
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!String.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.ToLower().Contains(app_name.ToLower()))
                {
                    p.Kill();
                }
            }
        }            
        private void Check_Keywords()
        {
        }      
        public void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var list = get_words();
            var matches = list.Where(p => p.keyword_name.ToLower() == textBox1.Text.ToLower());
            var matchess = matches.ToList();
            if ((textBox1.Text != null || textBox1.Text != "") && matchess.Count == 0)
            {
                richTextBox1.Clear();
                using (FileStream aFile = new FileStream("keywords.sys", FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(aFile))
                {
                    string text = textBox1.Text + "|" + start.Text + "|" + end.Text+ "|" + comboBox1.Text;
                    sw.WriteLine(text);
                }
                var new_list = get_words();
                List<string> arr = new List<string>();
                for (int i = 0; i <= new_list.Count - 1; ++i)
                {
                    arr.Add(new_list[i].keyword_name + "|" + new_list[i].start + "|" + new_list[i].end + "|" + new_list[i].allow_block);
                }
                richTextBox1.Lines = arr.ToArray();
            }
            else if ((textBox1.Text != null || textBox1.Text != "") && matchess.Count > 0)
            {
                richTextBox1.Clear();
                int line_num = get_line_num();
                string text = textBox1.Text + "|" + start.Text + "|" + end.Text + "|" + comboBox1.Text;
                lineChanger(text, "keywords.sys", line_num);
                var new_list = get_words();
                List<string> arr = new List<string>();
                for (int i = 0; i <= new_list.Count - 1; ++i)
                {
                    arr.Add(new_list[i].keyword_name + "|" + new_list[i].start + "|" + new_list[i].end + "|" + new_list[i].allow_block);
                }
                richTextBox1.Lines = arr.ToArray();
            }
        }
        private int get_line_num()
        {
            var list = get_words();
            var matches = list.Where(p => p.allow_block == textBox1.Text && p.allow_block != comboBox1.Text);
            string text = textBox1.Text + "|" + start.Text + "|" + end.Text + "|" + comboBox1.Text;
            for (int i = 0; i <= list.Count - 1; ++i)
            {
                string stext = list[i].keyword_name;
                if (text.ToLower().Contains(stext.ToLower()))
                {
                    return i + 1;
                }
            }
            return 0;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Opacity = 0;
            ShowInTaskbar = false;
            Form form1 = new Form();
            form1.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            form1.ShowInTaskbar = false;
            this.Hide();
        }   
        public void check_txt()
        {
            while (true)
            {
                Thread.Sleep(30000);
                if (File.Exists("keywords.sys"))
                {
                    try
                    {
                        var url = File.ReadAllLines("url.sys");
                        var client = new WebClient();
                        if (File.Exists("url_keywords.sys"))
                        {
                            File.Delete("url_keywords.sys");
                        }
                        Thread.Sleep(100);
                        client.DownloadFile(url[0], "url_keywords.sys");
                        client.Dispose();
                        var lines = File.ReadAllLines("url_keywords.sys");
                        Thread.Sleep(100);
                        var list = new List<keyword>();
                        for (int i = 1; i <= lines.Length - 1; ++i)
                        {
                            string[] ssize = lines[i].Split('|');
                            list.Add(new keyword { keyword_name = ssize[0], start = ssize[1], end = ssize[2], allow_block = lines[0] });
                        }
                        var keywords = get_words();
                        for (int i = 0; i <= list.Count - 1; ++i)
                        {
                            var online_keyword = list[i].keyword_name.ToLower();
                            for (int j = 0; j <= keywords.Count - 1; ++j)
                            {
                                keywords = get_words();
                                var local_keyword = keywords[j].keyword_name;
                                if (local_keyword.ToLower() == online_keyword.ToLower())
                                {
                                    lineChanger(list[i].keyword_name + "|" + list[i].start + "|" + list[i].end + "|" + list[i].allow_block, "keywords.sys", j + 1);
                                    keywords = get_words();
                                }
                                else
                                {
                                    var duplicate = check_duplicate(keywords, online_keyword);
                                    int num = 0;
                                    for (int s = 0; s <= keywords.Count - 1; s++)
                                    {
                                        if (keywords[s].keyword_name.ToLower() == online_keyword)
                                        {
                                            num = s + 1;
                                            break;
                                        }
                                    }
                                    if (duplicate != null)
                                    {
                                        add_text(list[i].keyword_name + "|" + list[i].start + "|" + list[i].end + "|" + list[i].allow_block);
                                        keywords = get_words();
                                    }
                                    else
                                    {
                                        lineChanger(list[i].keyword_name + "|" + list[i].start + "|" + list[i].end + "|" + list[i].allow_block, "keywords.sys", num);
                                        keywords = get_words();
                                    }
                                }
                            }
                        }

                        var new_list = get_words();
                        List<string> arr = new List<string>();
                        for (int i = 0; i <= new_list.Count - 1; ++i)
                        {
                            arr.Add(new_list[i].keyword_name + "|" + new_list[i].start + "|" + new_list[i].end + "|" + new_list[i].allow_block);
                        }
                        richTextBox1.Lines = arr.ToArray();
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    var url = File.ReadAllLines("url.sys");
                    var client = new WebClient();
                    if (File.Exists("url_keywords.sys"))
                    {
                        File.Delete("url_keywords.sys");
                    }
                    Thread.Sleep(100);
                    client.DownloadFile(url[0], "url_keywords.sys");
                    client.Dispose();
                    var lines = File.ReadAllLines("url_keywords.sys");
                    Thread.Sleep(100);
                    var list = new List<string>();                   
                    //if(lines[0] == "Block")
                    if(true)
                    {
                        for (int i = 1; i <= lines.Length; ++i)
                        {
                            list.Add(lines[i] + "|Block");
                        }
                        var arr = list.ToArray();
                        File.WriteAllLines("keywords.sys", arr);
                    }
                }
            }
        }
        private string check_duplicate(List<keyword> list, string local_keyword)
        {
            string text = "";
            for (int k = 0; k <= list.Count - 1; ++k)
            {
                if (list[k].keyword_name.ToLower().Contains(local_keyword.ToLower()))
                {
                    return null;
                }
                else
                {
                    text = list[k].keyword_name + "|" + list[k].start + "|" + list[k].end + "|" + list[k].allow_block;
                }
            }
            return text;
        }
        private void add_text(string asd)
        {            
                string filename = "keywords.sys";
                string readText = File.ReadAllText(filename);
                File.WriteAllText(filename, readText+ asd);                
        }
        private void button2_Click(object sender, EventArgs e)
        {
            using (StreamWriter sw = new StreamWriter("url.sys"))
            {
                sw.WriteLine(textBox2.Text);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if(button3.Text == "Stop")
            {
                work = false;
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OurSettings");
                key.SetValue("Stop_Work", "yes");
                key.Close();
                button3.Text = "Start";
            }
            else
            {
                work = true;
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\OurSettings");
                key.SetValue("Stop_Work", "no");
                key.Close();
                button3.Text = "Stop";
            }
        }
    }
}
