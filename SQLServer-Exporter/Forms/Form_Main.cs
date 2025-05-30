
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static NPOI.HSSF.Util.HSSFColor;
using SQLServerExporter.Library.Data;
using SQLServerExporter.Library.DataAccess;
using SQLServerExporter.Library.Service;
using SQLServerExporter.Forms;

namespace SQLServerExporter
{
    public partial class Form_Main : Form
    {
        private readonly SQLServerDB database;
        private readonly SQLServerDA dbTables;
        private readonly ExportService exportService;
        private readonly Dictionary<string, string> ExportTypes = new Dictionary<string, string>();
        private readonly Dictionary<string, string> Delimiters = new Dictionary<string, string>();
        public Form_Main()
        {
            InitializeComponent();
            database = new SQLServerDB();
            dbTables = new SQLServerDA(database);
            exportService = new ExportService();

            ExportTypes = new Dictionary<string, string> 
            {
                {".xlxs", "Excel" },
                {".csv", "CSV" },
                {".txt", "Text" }
            };
            Delimiters = new Dictionary<string, string>
            {
                {"Tab", "\\t" },
                {"Comma", ","},
                {"Colon", ":"},
                {"Equals Sign", "="},
                {"Semicolon", ";"},
                {"Space", " "},
            };

            DatabaseTextBox.DataBindings.Add("Text", database, nameof(database.database));

            dateTimePicker1.Value = new DateTime(2019, 1, 29);
            dateTimePicker2.Value = new DateTime(2023, 1, 31);
            BindComboBox(ExportComboBox, ExportTypes, "Key", "Value");
            BindComboBox(DelimiterComboBox, Delimiters, "Key", "Value");
            ExportComboBox.SelectedIndex = 0;
            DelimiterComboBox.SelectedIndex = 0;
            exportService.ProgressChanged += UpdateProgressBar;
            RedirectConsoleToTextBox();
        }

        // Methods
        private void UpdateProgressBar(int value)
        {
            if (ExportProgressBar.InvokeRequired)
            {
                ExportProgressBar.Invoke(new Action(() => ExportProgressBar.Value = value));
            }
            else
            {
                ExportProgressBar.Value = value;
            }
        }
        public void RedirectConsoleToTextBox()
        {
            TextBoxWriter writer = new TextBoxWriter(DebugTextBox); 
            Debug.Listeners.Clear(); 
            Debug.Listeners.Add(new TextWriterTraceListener(writer)); 
            Debug.AutoFlush = true;
        }
        private void BindListBox(List<string> list, ListBox listbox)
        {
            listbox.ClearSelected();
            listbox.DataSource = null;

            listbox.DataSource = list;
        }
        private void BindDataGridView(DataTable dataTable,DataGridView datagridView)
        {
            datagridView.DataSource = null;
            datagridView.Rows.Clear();
            datagridView.Columns.Clear();
            datagridView.DataSource = dataTable;
        }
        private void BindComboBox(ComboBox comboBox, BindingSource bindingSource, string displayMember, string valueMember)
        {
            comboBox.Items.Clear();
            comboBox.DataSource = bindingSource;
            comboBox.DisplayMember = displayMember;
            comboBox.ValueMember = valueMember;
        }
        private void BindComboBox(ComboBox comboBox, DataTable dataTable, string displayMember, string valueMember)
        {
            BindingSource bindingSource = new BindingSource(dataTable, null);
            BindComboBox(comboBox, bindingSource, displayMember, valueMember);
        }
        private void BindComboBox(ComboBox comboBox, Dictionary<string, string> dictionary, string displayMember, string valueMember)
        {
            BindingSource bindingSource = new BindingSource(dictionary, null);
            BindComboBox(comboBox, bindingSource, displayMember, valueMember);
        }
        private void BindComboBox(ComboBox comboBox, DataTable dataTable, string displayMember)
        {
            BindComboBox(comboBox, dataTable, displayMember, displayMember);
        }

        // Form events
        private void Form_Main_Load(object sender, EventArgs e)
        {
            using (Form_ConnectionSettings dbConf = new Form_ConnectionSettings())
            {
                dbConf.ShowDialog(this);

                if (dbConf.DialogResult != DialogResult.OK) return;

                database.SetConnectionParameters(dbConf.ServerAddress, dbConf.DatabaseName, dbConf.Password, dbConf.UserName, dbConf.TrustedCertificate);
                database.OpenConnection();
                return;
            }
        }
        private void Form_Main_Shown(object sender, EventArgs e)
        {
            Debug.WriteLine(database.EnsureConnection(database.SqlConn, out bool connected));
            if (!connected) return;

            DataTable Tables = dbTables.GetDBTableList();
            BindComboBox(dbTableComboBox, Tables, "Tables");
            if (Tables != null)
            { 
                SelectTableButton.PerformClick();
                ViewAllCheckBox.Checked = true;
            }
        }
        private void SelectTableButton_Click(object sender, EventArgs e)
        {
            List<string> columnsList = new List<string>();
            DataTable columns = dbTables.GetTableColumns(dbTableComboBox.Text); 

            foreach (DataRow row in columns.Rows)
            {
                columnsList.Add(row["COLUMNS"].ToString());
            }

            BindListBox(columnsList, listBox1);
        }
        private async void SearchButton_Click(object sender, EventArgs e)
        {
            DataTable dataTable;

            if (!ViewAllCheckBox.Checked)
            {
                List<string> selectedColumns = new List<string>();

                foreach (var item in listBox1.SelectedItems)
                {
                    selectedColumns.Add(item.ToString());
                }

                dataTable =  await dbTables.GetTableByDateRangeWithColumnsAsync(dbTableComboBox.Text, dateTimePicker1.Value, dateTimePicker2.Value, selectedColumns);
                BindDataGridView(await dbTables.GetTableByDateRangeWithColumnsAsync(dbTableComboBox.Text, dateTimePicker1.Value, dateTimePicker2.Value, selectedColumns), dataGridView1);
                RowCountTextBox.Text = dataTable.Rows.Count.ToString();
                return;
            }
            dataTable = await dbTables.GetTableByDateRangeAsync(dbTableComboBox.Text, dateTimePicker1.Value, dateTimePicker2.Value);
            RowCountTextBox.Text = dataTable.Rows.Count.ToString();
            BindDataGridView(dataTable, dataGridView1);
        }
        private async void ExportButton_Click(object sender, EventArgs e)
        {
            DataTable datatable = (DataTable)dataGridView1.DataSource;
            if (datatable == null) return;
            string exportType = ExportTypes[ExportComboBox.Text];
            bool success = false;

            int totaltask = datatable.Rows.Count * 2;
            ProgressPanel.Visible = true;
            ExportProgressBar.Maximum = totaltask;
            ExportProgressBar.Value = 0;
            switch (exportType)
            {
                case "Excel":
                    //exportService.ExportToExcel(datatable, "test", out success);
                    success = await exportService.ExportToExcelAsync(datatable, "test");
                    break;
                case "CSV":
                    //exportService.ExportToCsv(datatable, "test", out success);
                    success = await exportService.ExportToCsvAsync(datatable, "test");
                    break;
                case "Text":
                    //exportService.ExportToText(datatable, "test", DelimiterComboBox.SelectedValue.ToString(), out success);
                    success = await exportService.ExportToTextAsync(datatable, "test", DelimiterComboBox.SelectedValue.ToString());
                    break;
            }
            if (success)
            {
                ProgressPanel.Visible = false;
                MessageBox.Show("File saved successfully!", "File Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DebugTextBox.Clear();
        }
        private void ExportComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool tsv = ExportComboBox.SelectedValue.ToString().ToLower() == "text";
            DelimiterComboBox.Visible = DelimiterLabel.Visible = tsv;
        }
        private void ViewAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            listBox1.Enabled = !ViewAllCheckBox.Checked;
        }
    }
}
