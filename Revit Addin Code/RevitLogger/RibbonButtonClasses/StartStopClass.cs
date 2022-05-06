using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Helpers;
using RevitLogger.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RevitLogger
{
    [Transaction(TransactionMode.ReadOnly)]
    class StartStopClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {

            Document doc = commandData.Application.ActiveUIDocument.Document;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Error","Sorry this seams like family document which isn't supported");
                return Result.Cancelled;
            }
            try
            {
                //Getting The Active UIDocument :
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;
                ExtensibleStorage newExtensibleStorage = new ExtensibleStorage(doc, null, null, null);
                if (ExtensibleStorage.GetSchemaByName("Magnetar") == null)
                {
                    //Initializing ExternalEventHandler Event :
                    ExternalEventHandler.CreateEvent();
                    ExternalEventHandler.HandlerInstance.EventInfo = newExtensibleStorage;
                    ExternalEventHandler.ExternalEventInstance.Raise();

                }
                else if (newExtensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo)[0] == "")
                {
                    
                    MessageBox.Show("You should set project settings first");
                    SettingClass settingClass = new SettingClass();
                    settingClass.Execute(commandData, ref message, elements);
                }

                var test = newExtensibleStorage.GetFieldValue(SchemaField.MagnetarProjectInfo);
                Settings.Settings.AddinOnOff = !Settings.Settings.AddinOnOff;

                





                return Result.Succeeded;

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }

        }

        //Function to get the assembly path of the external command class :
        public static string GetAssemblyPath()
        {
            string codeBase = System.Reflection.Assembly.GetCallingAssembly().CodeBase;
            var assembly = new Uri(codeBase).LocalPath;
            return assembly;
        }
    }
}
