using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;
using Microsoft.Win32;

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

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                CSVFilePath = openFileDialog.FileName;

                // Read the CSV file into a DataTable
                var dataTable = new DataTable();
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
                                dataTable.Columns.Add(value.Trim());
                            }
                            header = false;
                        }
                        else if (values.Length == dataTable.Columns.Count) // Handle rows with different number of columns than the header
                        {
                            var row = dataTable.NewRow();
                            for (int i = 0; i < values.Length; i++)
                            {
                                row[i] = values[i].Trim();
                            }
                            dataTable.Rows.Add(row);
                        }
                    }
                }

                // Bind the DataTable to the DataGrid
                CSVDataGrid.ItemsSource = dataTable.DefaultView;

                // Update the text box with the selected file name
                CSVFilePathTextBox.Text = Path.GetFileName(CSVFilePath);
            }
        }



        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var rootElementName = RootElementNameTextBox.Text;
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
                    var node = xmlDocument.CreateElement(headers[i]);
                    node.InnerText = values[i];
                    rootElement.AppendChild(node);
                }

                // Save the XML file
                var xmlFileName = values[1] + ".xml"; // Use the value in column B for the file name
                var xmlFilePath = Path.Combine(Path.GetDirectoryName(csvFilePath), xmlFileName);
                xmlDocument.Save(xmlFilePath);
            }

            MessageBox.Show("XML files generated successfully.");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
