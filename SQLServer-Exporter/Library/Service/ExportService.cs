using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLServerExporter.Library.Service
{
    public class ExportService
    {
        public event Action<int> ProgressChanged; 

        public ExportService() { }

        // Helper Methods
        protected virtual void OnProgressChanged(int progress) // method to monitor async progress
        {
            ProgressChanged?.Invoke(progress); // Invoke the event, if any subscribers are attached
        }
        private string GetUniqueFileName(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath = filePath;

            while (File.Exists(newFilePath))
            {
                newFilePath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
                counter++;
            }
            return newFilePath;
        }
        private string GetOutPath()
        {
            string outputPath = string.Empty;
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;

                if (folderDialog.ShowDialog() == DialogResult.No) { return null; }

                outputPath = folderDialog.SelectedPath;
            }
            return GetUniqueFileName(outputPath);
        }
        private string GetSaveFilePath(string defaultFileName, string filter, out bool save)
        {
            save = false;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                saveFileDialog.OverwritePrompt = false;
                saveFileDialog.Filter = filter;
                saveFileDialog.FileName = defaultFileName;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    save = true;
                    return GetUniqueFileName(saveFileDialog.FileName);
                }
            }
            return null;
        }
        private string GetExcelSaveFilePath(string defaultFileName, out bool save)
        {
            return GetSaveFilePath(defaultFileName, "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*", out save);
        }
        private string GetCsvSaveFilePath(string defaultFileName, out bool save)
        {
            return GetSaveFilePath(defaultFileName, "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*", out save);
        }
        private string GetTextSaveFilePath(string defaultFileName, out bool save)
        {
            return GetSaveFilePath(defaultFileName, "Text Files (*.txt)|*.txt|All Files (*.*)|*.*", out save);
        }

        // CSV and Text Saving Logic
        private void SaveToFile(DataTable dataTable, string filename, string delimiter, out bool success)
        {
            try 
            {
                success = false;
                if (dataTable != null) { return; }
                string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                string defaultFileName = (delimiter == ",")? $"{filename}_{todayDate}.csv" : $"{filename}_{todayDate}.txt";
                string filePath = (delimiter == ",") ? GetCsvSaveFilePath(defaultFileName, out bool save): GetTextSaveFilePath(defaultFileName, out save);

                if (!save) { return; }

                byte[] tabByte = new byte[] { 0x09 }; // 0x09 is the ASCII value for a tab
                string tab = Encoding.ASCII.GetString(tabByte);
                delimiter = (delimiter == "\\t")? tab : delimiter;

                StringBuilder fileContent = new StringBuilder();
                // Add the column headers
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    fileContent.Append(dataTable.Columns[i].ColumnName);
                    if (i < dataTable.Columns.Count - 1)
                        fileContent.Append(delimiter);
                }
                fileContent.AppendLine();

                // Add the rows
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                    
                        string cellValue = row[i].ToString();
                        if (cellValue.Contains(delimiter))
                        {
                            // Add quotes around values containing the delimiter
                            cellValue = $"\"{cellValue}\""; 
                        }
                        fileContent.Append(cellValue);
                        if (i < dataTable.Columns.Count - 1)
                            fileContent.Append(delimiter);
                    }
                    fileContent.AppendLine();
                }

                File.WriteAllText(filePath, fileContent.ToString());
                success = true;
            }
            catch(Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                success = false;
            }
        }
        private async Task<bool> SaveToFileAsync(DataTable dataTable, string filename, string delimiter)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                string defaultFileName = (delimiter == ",") ? $"{filename}_{todayDate}.csv" : $"{filename}_{todayDate}.txt";
                string filePath = (delimiter == ",") ? GetCsvSaveFilePath(defaultFileName, out bool save) : GetTextSaveFilePath(defaultFileName, out save);

                if (!save) { return false; }

                byte[] tabByte = new byte[] { 0x09 }; // 0x09 is the ASCII value for a tab
                string tab = Encoding.ASCII.GetString(tabByte);
                delimiter = (delimiter == "\\t") ? tab : delimiter;

                StringBuilder fileContent = new StringBuilder();

                int currenttask = 1;
                await Task.Run(() =>
                {
                    // Add the column headers
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        fileContent.Append(dataTable.Columns[i].ColumnName);
                        if (i < dataTable.Columns.Count - 1)
                            fileContent.Append(delimiter);
                        currenttask = currenttask + (dataTable.Rows.Count / dataTable.Columns.Count);
                        OnProgressChanged(currenttask);
                        Debug.WriteLine($">> [{methodName}] Column {i + 1} autosized of {dataTable.Columns.Count} [{currenttask} of {dataTable.Rows.Count * 2}]");
                    }
                    fileContent.AppendLine();

                    // Add the rows
                    int rowIndex = 0;
                    foreach (DataRow row in dataTable.Rows)
                    {
                        
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {

                            string cellValue = row[i].ToString();
                            if (cellValue.Contains(delimiter))
                            {
                                // Add quotes around values containing the delimiter
                                cellValue = $"\"{cellValue}\"";
                            }
                            fileContent.Append(cellValue);
                            if (i < dataTable.Columns.Count - 1)
                                fileContent.Append(delimiter);
                        }
                        fileContent.AppendLine();
                        Debug.WriteLine($">> [{methodName}] Row {rowIndex + 1} added of {dataTable.Rows.Count} [{currenttask} of {dataTable.Rows.Count * 2}]");
                        OnProgressChanged(currenttask);
                        currenttask++;
                        rowIndex++;
                    }

                    Debug.WriteLine($">> [{methodName}] Saving");
                    File.WriteAllText(filePath, fileContent.ToString());
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                return false;
            }
        }

        // Export Methods
        public void ExportToExcel(DataTable dataTable, string filename, out bool success)
        {
            try
            {
                success = false;
                string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                string defaultFileName = $"{filename}_{todayDate}.xlsx";

                string excelFilePath = GetExcelSaveFilePath(defaultFileName, out bool save);
                if (!save) { return;}

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("Sheet1");

                // Create a cell style for centering text
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;

                // Add column headers
                IRow headerRow = sheet.CreateRow(0);
                headerRow.Height = 500; 
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(dataTable.Columns[i].ColumnName);
                    cell.CellStyle = headerStyle;
                }

                // Add rows
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(i + 1);
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        row.CreateCell(j).SetCellValue(dataTable.Rows[i][j].ToString());
                    }
                }

                // Auto-size the columns to fit the content (especially column headers)
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                sheet.CreateFreezePane(0, 1);  // 0 columns, 1 row

                // Save the Excel file
                using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fileStream);
                }
                success = true;
            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                success = false;
            }
        }
        public async Task<bool> ExportToExcelAsync(DataTable dataTable, string filename)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                string defaultFileName = $"{filename}_{todayDate}.xlsx";

                string excelFilePath = GetExcelSaveFilePath(defaultFileName, out bool save);
                if (!save) { return false; }

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("Sheet1");

                // Create a cell style for centering text
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;

                int currenttask = 1;

                await Task.Run(() =>
                {
                    // Add column headers asynchronously
                    IRow headerRow = sheet.CreateRow(0);
                    headerRow.Height = 500;
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        ICell cell = headerRow.CreateCell(i);
                        cell.SetCellValue(dataTable.Columns[i].ColumnName);
                        cell.CellStyle = headerStyle;
                    }
                    Debug.WriteLine($">> [{methodName}] Header added");

                    // Set column widths based on the header text
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        sheet.SetColumnWidth(i, (dataTable.Columns[i].ColumnName.Length + 10) * 256);
                        currenttask = currenttask + (dataTable.Rows.Count / dataTable.Columns.Count);
                        OnProgressChanged(currenttask);
                        Debug.WriteLine($">> [{methodName}] Column {i + 1} autosized of {dataTable.Columns.Count} [{currenttask} of {dataTable.Rows.Count * 2}]");
                    }

                    // Add rows asynchronously
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        IRow row = sheet.CreateRow(i + 1);
                        for (int j = 0; j < dataTable.Columns.Count; j++)
                        {
                            row.CreateCell(j).SetCellValue(dataTable.Rows[i][j].ToString());
                        }
                        Debug.WriteLine($">> [{methodName}] Row {i + 1} added of {dataTable.Rows.Count} [{currenttask} of {dataTable.Rows.Count * 2}]");
                        OnProgressChanged(currenttask);
                        currenttask++;
                    }

                    sheet.CreateFreezePane(0, 1); // Freeze the header row
                    Debug.WriteLine($">> [{methodName}] Selection freezed");

                    // Save the Excel file asynchronously
                    using (FileStream fileStream = new FileStream(excelFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        Debug.WriteLine($">> [{methodName}] Saving");
                        workbook.Write(fileStream);
                        Debug.WriteLine($">> [{methodName}] saved");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                return false;
            }
        }

        public void ExportToCsv(DataTable dataTable, string filename, out bool success)
        {
            SaveToFile(dataTable, filename, ",", out success);
        }
        public async Task<bool> ExportToCsvAsync(DataTable dataTable, string filename)
        {
            return await SaveToFileAsync(dataTable, filename, ",");
        }

        public void ExportToText(DataTable dataTable, string filename, string delimiter, out bool success)
        {
            SaveToFile(dataTable, filename, delimiter, out success);
        }
        public async Task<bool> ExportToTextAsync(DataTable dataTable, string filename, string delimiter)
        {
            return await SaveToFileAsync(dataTable, filename, delimiter);
        }
    }
}
