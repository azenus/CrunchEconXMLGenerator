using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace CreateEconomyFiles
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private string _csvFilePath;

        public string CSVFilePath
        {
            get { return _csvFilePath; }
            set { _csvFilePath = value; OnPropertyChanged(); }
        }

        private DataTable _dataTable;

        public DataTable DataTable
        {
            get { return _dataTable; }
            set { _dataTable = value; OnPropertyChanged(); }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                CSVFilePath = openFileDialog.FileName;

                // Read the CSV file into a DataTable
                DataTable = new DataTable();
                using (var reader = new StreamReader(CSVFilePath))
                {
                    var header = true;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        if (header)
                        {
                            foreach (var value in values)
                            {
                                DataTable.Columns.Add(value.Trim());
                            }
                            header = false;
                        }
                        else if (values.Length == DataTable.Columns.Count) // Handle rows with different number of columns than the header
                        {
                            var row = DataTable.NewRow();
                            for (int i = 0; i < values.Length; i++)
                            {
                                row[i] = values[i].Trim();
                            }
                            DataTable.Rows.Add(row);
                        }
                    }
                }

                // Bind the DataTable to the DataGrid
                CSVDataGrid.ItemsSource = DataTable.DefaultView;

                // Update the text box with the selected file name
                CSVFilePathTextBox.Text = Path.GetFileName(CSVFilePath);
            }
        }



        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var rootElementName = ((ComboBoxItem)RootElementComboBox.SelectedItem).Content.ToString();
            var prolog = "?xml version=\"1.0\" encoding=\"UTF-8\"?>"; // Set the default prolog
            var csvFilePath = CSVFilePath;

            // Read the CSV file
            var lines = File.ReadAllLines(csvFilePath);
            var headers = lines[0].Split(',');
            var records = lines.Skip(1).ToList();

            // Generate an XML file for each row in the CSV file
            foreach (var record in records)
            {
                var values = record.Split(',');

                var xmlDocument = new XmlDocument();

                var declaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDocument.AppendChild(declaration);

                var rootElement = xmlDocument.CreateElement(rootElementName);
                rootElement.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                rootElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                xmlDocument.AppendChild(rootElement);

                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i] != "")
                    {
                        var node = xmlDocument.CreateElement(headers[i]);
                        if (values[i] != null && values[i] != "")
                        {
                            node.InnerText = values[i];
                        }
                        rootElement.AppendChild(node);
                    }
                }

                // Save the XML file
                var fileName = (rootElementName == "SellOrder") ? values[3] : values[1]; // Use the value in column D for SellOrder root element
                var xmlFileName = fileName + ".xml";
                var xmlFilePath = Path.Combine(Path.GetDirectoryName(csvFilePath), xmlFileName);
                xmlDocument.Save(xmlFilePath);
            }

            MessageBox.Show("XML files generated successfully.");
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var lines = new List<string>();

            // Add the headers to the CSV file
            var headers = string.Join(",", DataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName));
            lines.Add(headers);

            // Add the data rows to the CSV file
            foreach (DataRow row in DataTable.Rows)
            {
                var values = row.ItemArray.Select(o => o.ToString()).ToArray();
                var line = string.Join(",", values);
                lines.Add(line);
            }

            // Write the lines to the CSV file
            File.WriteAllLines(CSVFilePath, lines);

            MessageBox.Show("Changes saved successfully.");
        }



        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataTable != null)
            {
                var row = DataTable.NewRow();
                DataTable.Rows.Add(row);
            }
        }

        private void RemoveRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataTable != null && CSVDataGrid.SelectedItem != null)
            {
                var rowView = CSVDataGrid.SelectedItem as DataRowView;
                if (rowView != null)
                {
                    DataTable.Rows.Remove(rowView.Row);
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

