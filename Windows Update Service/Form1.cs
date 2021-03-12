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

namespace Windows_Update_Service
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
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
            catch (Exception ex)
            {
            }
            return list;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            reg.SetValue("Windows Update Service", Application.ExecutablePath.ToString());
            try
            {
                var lines = File.ReadAllLines("url.sys");
                textBox2.Text = lines[0];
            }
            catch
            {
            }
            dateTimePicker1.CustomFormat = "HH-mm";
            dateTimePicker2.CustomFormat = "HH-mm";
            comboBox1.SelectedIndex = 0;
            dateTimePicker1 = new DateTimePicker();
            dateTimePicker1.Format = DateTimePickerFormat.Time;
            dateTimePicker1.ShowUpDown = true;
            var list = get_words();
            List<string> arr = new List<string>();
            for (int i = 0; i <= list.Count - 1; ++i)
            {
                arr.Add(list[i].keyword_name + "|" + list[i].start + "|" + list[i].end + "|" + list[i].allow_block);
            }
            richTextBox1.Lines = arr.ToArray();
            Thread TM = new Thread(Show_Form);
            TM.SetApartmentState(ApartmentState.STA);
            CheckForIllegalCrossThreadCalls = false;
            TM.Start();
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
                Thread.Sleep(1000);
                var keywords = get_words();
                foreach (var item in keywords)
                {
                    var app_name = GetCaptionOfActiveWindow();
                    if (item.allow_block == "Block")
                    {
                        if (app_name.ToLower().Contains(item.keyword_name.ToLower()))
                        {
                            DateTime now = DateTime.Now;
                            DateTime start = DateTime.Now;
                            DateTime.TryParseExact(item.start, "HH-mm",
         System.Globalization.CultureInfo.InvariantCulture,
         System.Globalization.DateTimeStyles.None, out start);
                            DateTime end = DateTime.Now;
                            DateTime.TryParseExact(item.end, "HH-mm",
    System.Globalization.CultureInfo.InvariantCulture,
    System.Globalization.DateTimeStyles.None, out end);
                            if (now > start && now < end)
                            {
                                exit_app(item.keyword_name);
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
                if (!String.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Contains(app_name))
                {
                    p.Kill();
                }
            }
        }            
        private void Check_Keywords()
        {
        }
        private void Show_Form()
        {
            var opacity = this.Opacity;
            ShowInTaskbar = false;

            Form form1 = new Form();

            form1.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form1.ShowInTaskbar = false;
            Owner = form1;
            while (true)
            {
                Thread.Sleep(1);
                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && (Keyboard.IsKeyDown(Key.Z)))
                {
                    if (this.Opacity < 0.5)
                    {
                        this.Opacity = 1;
                        Thread.Sleep(500);
                        this.Owner = null;
                    }
                    else
                    {
                        this.Opacity = 0;
                        Thread.Sleep(500);
                        this.Owner = form1;
                    }
                }
            }
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
                    string text = textBox1.Text + "|" + dateTimePicker1.Value.ToString("HH-mm") + "|" + dateTimePicker2.Value.ToString("HH-mm") + "|" + comboBox1.Text;
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
                string text = textBox1.Text + "|" + dateTimePicker1.Value.ToString("HH-mm") + "|" + dateTimePicker2.Value.ToString("HH-mm") + "|" + comboBox1.Text;
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
            string text = textBox1.Text + "|" + dateTimePicker1.Value.ToString("HH-mm") + "|" + dateTimePicker2.Value.ToString("HH-mm") + "|" + comboBox1.Text;
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
            form1.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form1.ShowInTaskbar = false;
            Owner = form1;
        }
        public void check_txt()
        {
            while (true)
            {
                Thread.Sleep(1000);
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
                                var duplicate = check_duplicate(keywords ,online_keyword);
                                int num = 0;
                                for(int s =0; s<=keywords.Count - 1; s++)
                                {
                                    if(keywords[s].keyword_name.ToLower() == online_keyword)
                                    {
                                        num = s + 1;
                                        break;
                                    }
                                }
                                if(duplicate != null)
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
                }
                catch(Exception  ex)
                {
                }
                var new_list = get_words();
                List<string> arr = new List<string>();
                for (int i = 0; i <= new_list.Count - 1; ++i)
                {
                    arr.Add(new_list[i].keyword_name + "|" + new_list[i].start + "|" + new_list[i].end + "|" + new_list[i].allow_block);
                }
                richTextBox1.Lines = arr.ToArray();
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
    }
}
