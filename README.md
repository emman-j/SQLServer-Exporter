# SQLServer-Exporter

**SQLServer-Exporter** is a C# application designed to export data from Microsoft SQL Server databases to Excel (`.xlsx`) and CSV (`.csv`) formats. It features a user-friendly interface for selecting tables, filtering by date ranges, customizing columns, and specifying export paths.

![main](https://raw.githubusercontent.com/emman-j/SQLServer-Exporter/refs/heads/main/Images/main.png)
![export1](https://raw.githubusercontent.com/emman-j/SQLServer-Exporter/refs/heads/main/Images/exportProgress.png)
![export2](https://raw.githubusercontent.com/emman-j/SQLServer-Exporter/refs/heads/main/Images/exportProgress2.png)
## Features

- **Data Export to Excel and CSV**  
  Export full or filtered table data directly to Excel or CSV.

- **Date Range Filtering**  
  Select a date range to limit the data retrieved based on a table’s datetime column.

- **Custom Column Selection**  
  Retain only desired columns or exclude specific columns during export.

- **Output File Customization**  
  Choose output folder and specify the filename via a dialog prompt.

- **Auto Column Width Adjustment**  
  Excel columns auto-adjusted to fit header text; limited to header width.

- **Null and DBNull Handling**  
  Safe export handling for null database values (e.g., `DBNull.Value` in .NET).

- **Command and Resource Management**  
  All SQL commands are disposable and properly cleaned up.

- **Extendable Export Logic**  
  Export functionality is encapsulated in a reusable `ExportService` class.

## Getting Started

### Prerequisites

- .NET Framework 4.7.2 or higher  
- Visual Studio 2019 or later  
- SQL Server instance  
- NPOI (via NuGet) for Excel export

### Installation

Clone the repository:

```bash
git clone https://github.com/emman-j/SQLServer-Exporter.git
```

Then:

- Open `SQLServer-Exporter.sln` in Visual Studio  
- Restore NuGet packages  
- Build the solution  

## How to Use

1. Launch the application.  
2. Select a SQL table from the dropdown.  
3. Choose a date range using the two DateTimePickers.  
4. Select columns to include from the ListBox (multi-select supported).  
5. Click "Search" to preview the data.  
6. Click "Export" to export the filtered data.  
7. When prompted, select the folder and specify the filename.

## Attribution

This project uses **NPOI** for Excel file generation.  
NPOI is the .NET version of the popular Java POI library.  
GitHub: https://github.com/tonyqus/npoi  
Licensed under the Apache License 2.0.

## Notes

- The application uses parameterized SQL queries to avoid injection, but table name injection should be validated externally.
- Exported Excel files are saved using the `.xlsx` format and CSV files with `.csv`.
- Column widths in Excel are adjusted to fit only the header text length.

## License

This project is open-source and available under the MIT License.
