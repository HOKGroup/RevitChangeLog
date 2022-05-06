using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Media;
using Autodesk.Revit.DB.Events;
using RevitLogger.ExternalEvents;
using System.Windows;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB;
using RevitLogger.UI;
using System.Xml;

namespace RevitLogger
{
    public class Application : IExternalApplication
    {

        //Properties :
        public static string PluginName { get; set; }

        private Document currentActivatedDocument { get; set; }

        //Shut down function (is Executing when the application get shutting down) :
        public Result OnShutdown(UIControlledApplication application)
        {

            return Result.Succeeded;
        }

        //Startup Function to load the Plugin Data :
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {


                //Plugin Information Passing :
                PluginName = application.ActiveAddInId.GetAddInName();

                //Creation instance of the closing handler :
                ActiveDocumentHandler.Instance = new ActiveDocumentHandler(application, PluginName);

                //Creation Tab, Ribbon, Panel and Button : 
                var tabName = "HOK";
                var panelName = "Revit Logger";
                HelperRevitUI.AddRibbonTab(application, tabName);
                List<RibbonPanel> panels = application.GetRibbonPanels(tabName);
                RibbonPanel panel = HelperRevitUI.AddRibbonPanel(application, tabName, panelName, false);





                // add start/stop button
                PushButton Pluginbtn2 = HelperRevitUI.AddPushButton(panel, "Start/Stop", "Start/Stop", typeof(StartStopClass), StartStopClass.GetAssemblyPath());
                Image img1 = Properties.Resources.AppOff;
                ImageSource imgsc1 = GetImageSource(img1);
                Pluginbtn2.Image = imgsc1;
                Pluginbtn2.LargeImage = imgsc1;

                // add notes button
                PushButton Pluginbtn3 = HelperRevitUI.AddPushButton(panel, "Notes", "Notes", typeof(NotesClass), NotesClass.GetAssemblyPath());
                Image img3 = Properties.Resources.NotesOff;
                ImageSource imgsc3 = GetImageSource(img3);
                Pluginbtn3.Image = imgsc3;
                Pluginbtn3.LargeImage = imgsc3;


                // add settings button
                PushButton Pluginbtn = HelperRevitUI.AddPushButton(panel, "Settings", "Settings", typeof(SettingClass), SettingClass.GetAssemblyPath());
                Image img = Properties.Resources.SettingOff;
                ImageSource imgsc = GetImageSource(img);
                Pluginbtn.Image = imgsc;
                Pluginbtn.LargeImage = imgsc;

                // add select from button
                PushButton Pluginbtn4 = HelperRevitUI.AddPushButton(panel, "SelectFrom", "Select From\nLog(s)", typeof(SelectFrom), SelectFrom.GetAssemblyPath());
                Image img4 = Properties.Resources.SelectOff;
                ImageSource imgsc4 = GetImageSource(img4);
                Pluginbtn4.Image = imgsc4;
                Pluginbtn4.LargeImage = imgsc4;







                //attach plugins to open/saveAs/save events 
                application.ControlledApplication.DocumentSaved += new EventHandler<Autodesk.Revit.DB.Events.DocumentSavedEventArgs>(OnDocumentSaved);
                application.ControlledApplication.DocumentSavedAs += new EventHandler<Autodesk.Revit.DB.Events.DocumentSavedAsEventArgs>(OnDocumentFirstSave);
                application.ControlledApplication.DocumentOpened += new EventHandler<Autodesk.Revit.DB.Events.DocumentOpenedEventArgs>(OnDocumentOpened);
                application.ControlledApplication.DocumentSynchronizedWithCentral += new EventHandler<DocumentSynchronizedWithCentralEventArgs>(OnDocumentSynched);

                //Also we attach the plugin to view change event to make sure if the user open more than one document not to cause errors by using other document settings 
                application.ViewActivated += new EventHandler<Autodesk.Revit.UI.Events.ViewActivatedEventArgs>(OnViewChanged);



                return Result.Succeeded;

            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }

        private void OnDocumentSynched(object sender, DocumentSynchronizedWithCentralEventArgs e)
        {
            if (e.Document.IsFamilyDocument)
                return;
            ExtensibleStorage extensibleStorage = new ExtensibleStorage(e.Document, null, null, null);
            var projectinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);
            var loggerinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarRevitLogger);
            if (projectinfo != null && loggerinfo != null)
            {
                bool flag = projectinfo[0] == "" || projectinfo[1] == "" || loggerinfo[0] == "";
                if (Settings.Settings.AddinOnOff && !flag)
                {
                    ShowProgressBarWindow(false, e.Document);
                }
                else if (Settings.Settings.AddinOnOff && flag)
                {
                    MessageBox.Show("failed to log, please check settings first ");
                }
            }
        }

        private void OnViewChanged(object sender, ViewActivatedEventArgs e)
        {
            if (e.Document.IsFamilyDocument)
                return;

            //if the current view document is changed.. we read the setting from the other document 
            if (currentActivatedDocument != e.Document)
            {
                currentActivatedDocument = e.Document;

                ExtensibleStorage extensibleStorage = new ExtensibleStorage(e.Document, null, null, null);

                extensibleStorage.InitializeSetting();
                var projectinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);

                //if the new document has no settings yet we will deactivate the plugin to not cause errors during saving command
                if (projectinfo.Count == 0)
                {
                    Settings.Settings.SettingOnOff = false;
                    Settings.Settings.NotesOnOff = false;
                    Settings.Settings.SelectFromOnOff = false;
                }
                else
                {
                    if (projectinfo.Count != 0)
                        Settings.Settings.SettingOnOff = projectinfo[0] == "" ? false : true;


                }
            }
        }

        private void OnDocumentFirstSave(object sender, DocumentSavedAsEventArgs e)
        {

            if (e.Document.IsFamilyDocument)
                return;

            //if the plugin is not activated we exit logging proccess
            if (!Settings.Settings.AddinOnOff) return;


            //read settings from the files
            ExtensibleStorage extensibleStorage = new ExtensibleStorage(e.Document, null, null, null);
            var projectinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);
            var loggerinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarRevitLogger);


            //if the file has settings already we will porceed 
            if (projectinfo.Count != 0 && loggerinfo.Count != 0)
            {
                //check if the setting is found but it has values to log
                bool flag = projectinfo[0] == "" || projectinfo[1] == "" || loggerinfo[0] == "";

                //if they have values then everything is ok and we will move to the next steps
                if (!flag)
                {
                    ShowProgressBarWindow(true, e.Document);
                }
                else
                {

                    MessageBox.Show("failed to log, please check settings first ");
                }
            }



        }

        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {

            if (e.Document.IsFamilyDocument)
            {
                Settings.Settings.SettingOnOff = false;
                Settings.Settings.NotesOnOff = false;
                Settings.Settings.SelectFromOnOff = false;
                return;
            }
            //assign the opend document to current document
            currentActivatedDocument = e.Document;
            ExtensibleStorage extensibleStorage = new ExtensibleStorage(e.Document, null, null, null);
            //read file settings 
            extensibleStorage.InitializeSetting();
            ImportLogPathFromXMLfile();
            var projectinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);
            var loggerinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarRevitLogger);

            if (projectinfo.Count == 0)
            {
                Settings.Settings.SettingOnOff = false;
                Settings.Settings.NotesOnOff = false;
                Settings.Settings.SelectFromOnOff = false;
            }
            else
            {
                if (projectinfo.Count != 0)
                    Settings.Settings.SettingOnOff = projectinfo[0] == "" ? false : true;
                bool flag = projectinfo[0] == "" || projectinfo[1] == "" || loggerinfo[0] == "";
                if (Settings.Settings.AddinOnOff && !flag)
                {
                    ShowProgressBarWindow(false, e.Document);
                }


            }


        }



        private void OnDocumentSaved(object sender, DocumentSavedEventArgs e)
        {
            if (e.Document.IsFamilyDocument)
                return;
            ExtensibleStorage extensibleStorage = new ExtensibleStorage(e.Document, null, null, null);
            var projectinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);
            var loggerinfo = extensibleStorage.GetFieldValue(SchemaField.MagnetarRevitLogger);
            if (projectinfo != null && loggerinfo != null)
            {
                if (!e.Document.IsWorkshared)
                {
                    bool flag = projectinfo[0] == "" || projectinfo[1] == "" || loggerinfo[0] == "";
                    if (Settings.Settings.AddinOnOff && !flag)
                    {
                        ShowProgressBarWindow(false, e.Document);
                    }
                    else if (Settings.Settings.AddinOnOff && flag)
                    {
                        MessageBox.Show("failed to log, please check settings first ");
                    }
                }

            }




        }

        private void ShowProgressBarWindow(bool FirstSave, Document document)
        {
            Prog progrssWindow = new Prog(document, FirstSave);

            //progrssWindow.ResizeMode = ResizeMode.NoResize;
            //progrssWindow.Height = 250;
            //progrssWindow.Top = 200;
            //progrssWindow.Left = 900;
            progrssWindow.ShowDialog();



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
                var projectName = Settings.Settings.ProjectName;
                var projectNumber = Settings.Settings.ProjectNumber;
                Settings.Settings.LogPath = log;
                Settings.Settings.FullLogPath = log + $@"\{projectName}_{projectNumber}\";
                reader.Close();

            }

        }




        private BitmapSource GetImageSource(Image img)
        {
            BitmapImage bmp = new BitmapImage();

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = null;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            return bmp;
        }
    }
}
