using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace EC_Country_Budget_Editor
{
    public partial class MainForm : Form
    {
        private string ModPath;
        private string[] CountryFiles;
        private string Sceanrio_2013 = "2013.11.1";
        private Dictionary<string, string> CountryNamesWithMoney = new Dictionary<string, string>();

        //private string ModPath { get => ModPath; set => ModPath = value; }
        /*private string ModPath1
        {
            get { return ModPath1 };
            set { ModPath1 = value};
        }
        */
        public MainForm()
        {
            InitializeComponent();
        }

        

        private void MainForm_Load(object sender, EventArgs e)
        {
            version_label.Text = "v. " + Application.ProductVersion;
        }

        private void SetModPath_Btn_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                string DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod");
                string MyDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DirectoryInfo ModFolderDir = new DirectoryInfo(DefaultPath);
                              
                if (ModFolderDir.Exists)
                {
                    dialog.SelectedPath = DefaultPath;
                }
                else
                {
                    dialog.SelectedPath = MyDocumentsPath;
                }

                dialog.Description = "Please set the right directory of the EC mod";
                dialog.ShowDialog();
                ModPath = dialog.SelectedPath;
                ModPath_TB.Text = ModPath;

            }
        }

        private void Load_Btn_Click(object sender, EventArgs e)
        {          
            try
            {
                ReadData();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }           

            foreach (var item in CountryNamesWithMoney)
            {                
                dataGridView1.Rows.Add(item.Key, item.Value, Sceanrio_2013);
            }
        }

        private void ReadData()
        {
            CountryFiles = Directory.GetFiles(ModPath + @"\history\countries\", "*.txt", SearchOption.TopDirectoryOnly);          

            if(progressBar1.Value > 0)
            {
                progressBar1.Value = 0;
            }

            string[] buffer;
            string money_value = "500"; //Default

            int count = 0;
            foreach (var file in CountryFiles)
            {               
                buffer = File.ReadAllLines(file);
                
                foreach (var line in buffer)
                {
                    if(line.Contains("variable") && line.Contains("money"))
                    {
                        money_value = line.Split('=')[2];
                        money_value = money_value.Substring(0, money_value.IndexOf('}'));
                        money_value = money_value.Trim();
                        break;
                    }
                    else
                    {
                        money_value = "500";
                    }
                }
                CountryNamesWithMoney.Add(Path.GetFileNameWithoutExtension(file), money_value);

                ProgressBarChange(count++, CountryFiles.Length);           
            }            
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            CountryFiles = Directory.GetFiles(ModPath + @"\history\countries\", "*.txt", SearchOption.TopDirectoryOnly);          
            string money_value = "500"; //Default

            int count = 0;
            foreach (var file in CountryFiles)
            {
                string country_name = Path.GetFileNameWithoutExtension(file);
                money_value = CountryNamesWithMoney[country_name];

                if (!HasMoneyString(file))
                {
                    AddMoneyString(file);
                }

                ChangeMoney(file, money_value);
                ProgressBarChange(count++, CountryFiles.Length);
            }
        }

        private void ChangeMoney(string file, string money_value)
        {       
            string[] buffer = File.ReadAllLines(file);

            for(var i=0; i<buffer.Length; i++)
            {
                if (buffer[i].Contains("variable") && buffer[i].Contains("money"))
                {
                    buffer[i] = $"\tadd_to_variable = {{ money = {money_value} }}";
                }
            }

            File.WriteAllLines(file, buffer);        
        }

        private void AddMoneyString(string file, string money_value="500")
        {
            string[] buffer = File.ReadAllLines(file);
            List<string> list = new List<string>(buffer);

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Contains(Sceanrio_2013) && list[i].Contains("=") && list[i].Contains("{"))
                {                   
                    list.Insert(i + 1, $"\tadd_to_variable = {{ money = {money_value} }}");                   
                    break;
                }
            }
            
            string[] new_buffer = list.GetRange(0, list.Count).ToArray();

            File.WriteAllLines(file, new_buffer);           
        }

        private bool HasMoneyString(string file)
        {
            string[] buffer = File.ReadAllLines(file);          
            bool hasMoneyString = false;

            foreach(var line in buffer)
            {           
                if (line.Contains("variable") && line.Contains("money"))
                {
                    hasMoneyString = true;
                    break;
                }
            }           

            return hasMoneyString;
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string NewValue = dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString();
            string Key = dataGridView1[e.ColumnIndex - 1, e.RowIndex].Value.ToString();

            NewValue = NewValue.RemoverStrs(new[] { " ", ",", ".", "\\", "/" });            
            CountryNamesWithMoney[Key] = NewValue;
        }

        private void Clear_Btn_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            CountryNamesWithMoney.Clear();
            progressBar1.Value = 0;
        }

        private void ProgressBarChange(int currentValue, int maxValue)
        {
            if(progressBar1.Value == maxValue)
            {
                progressBar1.Value = 0;
            }

            progressBar1.Maximum = maxValue;
            progressBar1.Value = currentValue;
        }       
    }


    public static class StringHelper
    {
        public static string RemoverStrs(this string str, string[] removeStrs)
        {
            foreach (var removeStr in removeStrs)
                str = str.Replace(removeStr, "");
            return str;
        }
    }
}
