using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RevitLogger.Settings;
using System.Diagnostics;
using System.Windows.Forms;

using System.Xml;
using MessageBox = System.Windows.MessageBox;
using Autodesk.Revit.DB;
using Color = System.Windows.Media.Color;
using System.IO;
using Helpers;

namespace RevitLogger.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Fields :
        private string demoLink;
        private string projectNote;



        public UIDocument Ui;
        private Document doc { get; set; }
        public static MainWindow CurrentMainWindow { get; internal set; }
        public MainWindow(UIDocument uIDocument)
        {
            InitializeComponent();
            Ui = uIDocument;

            doc = uIDocument.Document;
            DataContext = new ViewModel(this, Ui);
            ReadSettings();
        }

        private void ReadSettings()
        {






            txtProjectName.Text = Settings.Settings.ProjectName;
            txtProjectNumber.Text = Settings.Settings.ProjectNumber;
            txtExternalProjectId.Text = Settings.Settings.externalProjectID;
            if (Settings.Settings.scope == "model")
                rbModelCat.IsChecked = true;
            else if (Settings.Settings.scope == "annotation")
                rbAnnoCat.IsChecked = true;
            else if (Settings.Settings.scope == "all")
                rbAll.IsChecked = true;
            else
            {
                rbModelCat.IsChecked = false;
                rbAnnoCat.IsChecked = false;
                rbAll.IsChecked = false;
                txtCustomScope.Text = Settings.Settings.scope;

            }

            txtLogFolder_LostFocus(this, null);
            txtProjectName_LostFocus(this, null);
            txtProjectNumber_LostFocus(this, null);
            txtExternalProjectId_LostFocus(this, null);
            ImportLogPathFromXMLfile();
            txtLogFolder.Text = Settings.Settings.LogPath;

        }

        private void ImportLogPathFromXMLfile()
        {

            string path = Settings.Settings.SaveSettingsFilePath;

            path = Environment.ExpandEnvironmentVariables(path);

            //check if the file is found
            if (!File.Exists(path + "settings.xml"))
            {

                Settings.Settings.LogPath = "";
                return;
            }



            // save settings XML to the directory

            using (XmlReader reader = XmlReader.Create(path + "settings.xml"))
            {
                reader.ReadStartElement("RevitLogger");
                var log = reader.ReadElementContentAsString();
                Settings.Settings.LogPath = log;
                Settings.Settings.FullLogPath = log + $@"\{txtProjectName.Text}_{txtProjectNumber.Text}\";
                reader.Close();

            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void rbAnnoCat_Checked(object sender, RoutedEventArgs e)
        {
            if (txtCustomScope != null)
                txtCustomScope.Text = "";

        }

        private void rbModelCat_Checked(object sender, RoutedEventArgs e)
        {
            if (txtCustomScope != null)
                txtCustomScope.Text = "";
        }

        private void rbAll_Checked(object sender, RoutedEventArgs e)
        {
            if (txtCustomScope != null)
                txtCustomScope.Text = "";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var scope = "";

            bool errorFlag = false;
            if (rbAnnoCat.IsChecked == true)
            {
                scope = "annotation";

            }
            else if (rbModelCat.IsChecked == true)
                scope = "model";
            else if (rbAll.IsChecked == true)
            {
                scope = "all";

            }
            else if (!string.IsNullOrWhiteSpace(txtCustomScope.Text) && !string.IsNullOrEmpty(txtCustomScope.Text))
            {
                scope = txtCustomScope.Text;
            }
            else
            {
                MessageBox.Show("You must choose a scope to log");
                errorFlag = true;
            }
            if (txtLogFolder.Text == "<pick a location to save Logs> (required)"
                || txtProjectName.Text == "<enter a project name> (required)"
                || txtProjectNumber.Text == "<enter a project number> (required)")

            {
                errorFlag = true;
                MessageBox.Show("Some required fields are missing, please make sure to enter all required fields.");
            }

            if (!errorFlag)
            {
                if (demoLink == null)
                {
                    if (string.IsNullOrEmpty(Settings.Settings.demoLink))
                    {
                        demoLink = "";
                    }
                    else
                    {
                        demoLink = Settings.Settings.demoLink;
                    }

                }

                if (projectNote == null)
                {
                    projectNote = "";
                }


                var ExternalProId = (txtExternalProjectId.Text == "<enter a project external id> (optional)" || txtExternalProjectId.Text == "") ? "" : txtExternalProjectId.Text;
                List<string> projectInfoValues = new List<string>() { txtProjectName.Text, txtProjectNumber.Text, ExternalProId };
                List<string> revitLoggerValues = new List<string>() { scope, demoLink, projectNote };

                ExtensibleStorage extensibleStorage = new ExtensibleStorage(doc, revitLoggerValues, projectInfoValues, SchemaField.both);


                ExternalEventHandler.HandlerInstance.EventInfo = extensibleStorage;
                ExternalEventHandler.ExternalEventInstance.Raise();



                SaveXMLsettingFile();
                Settings.Settings.scope = scope;
                Settings.Settings.LogPath = txtLogFolder.Text;
                Settings.Settings.FullLogPath = txtLogFolder.Text + $@"\{txtProjectName.Text}_{txtProjectNumber.Text}\";
                Settings.Settings.ProjectNote = projectNote;
                Settings.Settings.demoLink = demoLink;
                Settings.Settings.SettingOnOff = true;




                Close();

            }


        }

        private void SaveXMLsettingFile()
        {

            string path = Settings.Settings.SaveSettingsFilePath;

            path = Environment.ExpandEnvironmentVariables(path);

            //check if the directory is found. if not we will create one
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            // save settings XML to the directory

            using (XmlWriter writer = XmlWriter.Create(path + "settings.xml"))
            {
                writer.WriteStartElement("RevitLogger");
                writer.WriteElementString("LogPath", txtLogFolder.Text);

                writer.WriteEndElement();
                writer.Flush();
            }
        }

        private void txtCustomScope_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtCustomScope.Text) && !string.IsNullOrEmpty(txtCustomScope.Text))
            {
                rbAll.IsChecked = false;
                rbAnnoCat.IsChecked = false;
                rbModelCat.IsChecked = false;
                // txtCustomScope.Text = RemoveWhitespace(txtCustomScope.Text);
                //  txtCustomScope.Select(txtCustomScope.Text.Length, 0);

            }
        }

        private string RemoveWhitespace(string str)
        {
            return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        private void BtnSave_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void lblDemoLink_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!String.IsNullOrEmpty(Settings.Settings.demoLink))
            {
                Process.Start(Settings.Settings.demoLink);
            }
            else
            {
                MessageBox.Show("Ooops! There is no Demo Link to navigate");
            }
        }

        private void txtLogFolder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtLogFolder.Text == "")
            {
                txtLogFolder.Text = "<pick a location to save Logs> (required)";

                txtLogFolder.Foreground = new SolidColorBrush(Color.FromRgb(159, 159, 159));

            }
            else

                txtLogFolder.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void txtLogFolder_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtLogFolder.Text == "<pick a location to save Logs> (required)")
            {
                txtLogFolder.Text = "";
                txtLogFolder.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
        }

        private void txtProjectName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtProjectName.Text == "")
            {
                txtProjectName.Text = "<enter a project name> (required)";

                txtProjectName.Foreground = new SolidColorBrush(Color.FromRgb(159, 159, 159));

            }

        }

        private void txtProjectName_GotFocus(object sender, RoutedEventArgs e)
        {

            if (txtProjectName.Text == "<enter a project name> (required)")
            {
                txtProjectName.Text = "";
                txtProjectName.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }

        }

        private void txtProjectNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtProjectNumber.Text == "")
            {
                txtProjectNumber.Text = "<enter a project number> (required)";

                txtProjectNumber.Foreground = new SolidColorBrush(Color.FromRgb(159, 159, 159));

            }

        }

        private void txtProjectNumber_GotFocus(object sender, RoutedEventArgs e)
        {

            if (txtProjectNumber.Text == "<enter a project number> (required)")
            {
                txtProjectNumber.Text = "";
                txtProjectNumber.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }

        }

        private void txtExternalProjectId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtExternalProjectId.Text == "")
            {
                txtExternalProjectId.Text = "<enter a project external id> (optional)";

                txtExternalProjectId.Foreground = new SolidColorBrush(Color.FromRgb(159, 159, 159));

            }



        }

        private void txtExternalProjectId_GotFocus(object sender, RoutedEventArgs e)
        {

            if (txtExternalProjectId.Text == "<enter a project external id> (optional)")
            {
                txtExternalProjectId.Text = "";
                txtExternalProjectId.Foreground = new SolidColorBrush(Color.FromRgb( 0, 0, 0));
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Create  FolderBrowserDialog
            FolderBrowserDialog dlg = new FolderBrowserDialog();



            // Set folder location if already choosen before 
            if (txtLogFolder.Text == "")
                dlg.SelectedPath = txtLogFolder.Text;


            // Display OpenFileDialog by calling ShowDialog method
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == System.Windows.Forms.DialogResult.OK)
            {

                txtLogFolder.Text = dlg.SelectedPath;
                txtLogFolder.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));




            }
        }



        private void btnImportSettings_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog

            OpenFileDialog dlg = new OpenFileDialog();
            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV documents (.csv)|*.csv";

            // Display OpenFileDialog by calling ShowDialog method
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == System.Windows.Forms.DialogResult.OK)
            {




                // path to the csv file
                string path = dlg.FileName;


                string[] lines = System.IO.File.ReadAllLines(path);
                string[] headers = lines[0].Split(',');

                string[] columns = lines[1].Split(',');

                for (int fieldIndex = 0; fieldIndex < columns.Length; fieldIndex++)
                {


                    try
                    {

                        var value = columns[fieldIndex].Replace("\"", "");
                        switch (headers[fieldIndex].ToLower().Replace("\"", ""))
                        {
                            case "logpath":
                                txtLogFolder.Text = value;

                                break;

                            case "projectName":
                                txtProjectName.Text = value;
                                break;

                            case "projectnumber":
                                txtProjectNumber.Text = value;
                                break;
                            case "externalid":
                                if (value == "") break;
                                txtExternalProjectId.Text = value;
                                break;
                            case "scope":
                                var scopeText = value.ToLower();
                                if (scopeText =="all")

                                    rbAll.IsChecked = true;
                                else if (scopeText=="model categories")
                                    rbModelCat.IsChecked = true;
                                else if (scopeText=="annotation categories")
                                    rbAnnoCat.IsChecked = true;
                                else
                                    txtCustomScope.Text = value;

                                break;

                            case "demolink":
                                if (value == "") break;
                                demoLink = value;
                                break;

                            case "projectnote":
                                if (value == "") break;
                                projectNote = value;
                                break;

                            default:
                                break;



                        }



                    }
                    catch (Exception ex)
                    {
                        continue;

                    }

                }


            }
        }
    }
}
