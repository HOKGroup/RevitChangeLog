using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Helpers;
using Newtonsoft.Json;
using RevitLogger.Helpers;
using RevitLogger.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace RevitLogger
{
    [Transaction(TransactionMode.ReadOnly)]
    class SelectFrom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                //Getting The Active UIDocument :
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;
                Document doc = uIDocument.Document;
                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Error", "Sorry this seams like family document which isn't supported");
                    return Result.Cancelled;
                }

                //list of elements unique id to be selected
                List<ElementId> elementsIdsList = new List<ElementId>();

                // Create OpenFileDialog
                OpenFileDialog dlg = new OpenFileDialog();
                // Set filter for file extension and default file extension
                dlg.DefaultExt = ".jsonl";
                dlg.Filter = "JSONL documents (.jsonl)|*.jsonl";
                dlg.Multiselect = true;

                // Display OpenFileDialog by calling ShowDialog method
                var result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox
                if (result == DialogResult.OK)
                {
                    var files = dlg.FileNames;
                    foreach (var file in files)
                    {
                        string[] lines = System.IO.File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            LoggedElementObject loggedElementObject = JsonConvert.DeserializeObject<LoggedElementObject>(line);
                            string ElementUniqueId = loggedElementObject.ObjectIds[0];
                            Element element = doc.GetElement(ElementUniqueId);
                            if (element != null)
                            {
                                if (!elementsIdsList.Contains(element.Id))
                                elementsIdsList.Add(element.Id);
                            }

                        }

                    }
                    if (elementsIdsList.Count > 0)
                    {
                    uIDocument.Selection.SetElementIds(elementsIdsList);
                        Settings.Settings.SelectFromOnOff = true;
                    }
                
                }

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
