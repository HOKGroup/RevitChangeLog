using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Helpers;
using Newtonsoft.Json;
using RevitLogger.Helpers;
using RevitLogger.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RevitLogger
{
    internal class Logger : ExternalEventInfo
    {
        public static string modelGuid { get; set; }
        Prog progrss1;
        Document _doc;
        bool _firstSave;

        public Logger(Prog progressWindow, Document doc, bool firstSave)
        {
            progrss1 = progressWindow;
            _doc = doc;
            _firstSave = firstSave;
        }
        public void Log()
        {



            //to determine which scope we will take (All - Model Categories - Annotation Categories )
            string scope = Settings.Settings.scope;

            FilteredElementCollector FamilyInstanceCollector = new FilteredElementCollector(_doc);

            //get all elements in the model that is not element type 
            IList<Element> ElementsCollection = FamilyInstanceCollector.WhereElementIsNotElementType().ToElements();

            //lists to obtain the elements according to the scope 
            IList<Element> AllModelElements = new List<Element>();
            IList<Element> AllAnnotationElements = new List<Element>();
            IList<Element> AllCustomElements = new List<Element>();
            List<Element> scopedElements = new List<Element>();



            foreach (Element e in ElementsCollection)
            {
                if (e.Category == null) continue;


                if (e is FamilyInstance)
                {

                }


                // get element geometry to check it's model or not
                var geom = e.get_Geometry(new Options());


                //Get Model Elements in the Revit document
                //
                //check if the element has geometry and its category is model if the scope is (model/all/custom) which surly not annotation  
                if (null != geom && e.Category.CategoryType == CategoryType.Model && scope != "annotation")
                {

                    foreach (var item in geom)
                    {

                        var soild = item as Solid;




                        if (soild != null)
                        {
                            if (soild.Volume > 0 && e.Category.CategoryType == CategoryType.Model && e.Location != null)
                            {
                                if (scope == "model" || scope == "all")
                                {
                                    AllModelElements.Add(e);

                                }
                                else if (scope.Split(';').Length > 0)
                                {
                                    var catArr = scope.Split(';');
                                    var result = catArr.Where(x => x.ToLower() == e.Category.Name.ToLower());

                                    if (result.Count() > 0)
                                        AllCustomElements.Add(e);

                                }
                            }
                        }

                        else if (item.IsElementGeometry)
                        {
                            if (scope == "model" || scope == "all")
                            {
                                AllModelElements.Add(e);

                            }
                            else if (scope.Contains(";") && scope.Split(';').Length > 0)
                            {
                                var catArr = scope.Split(';');
                                var result = catArr.Where(x => x.ToLower() == e.Category.Name.ToLower());

                                if (result.Count() > 0)
                                    AllCustomElements.Add(e);

                            }
                        }






                    }






                }




                // get annotation elements in the model
                //check the ategory type and the scope either.
                if (e.Category.CategoryType == CategoryType.Annotation && e.Location != null && scope != "model")
                {

                    if (scope == "annotation" || scope == "all")
                    {
                        AllAnnotationElements.Add(e);

                    }
                    else if (scope.Contains(";") && scope.Split(';').Length > 0)
                    {
                        var catArr = scope.Split(';');
                        var result = catArr.Where(x => x.ToLower() == e.Category.Name.ToLower());

                        if (result.Count() > 0)
                            AllCustomElements.Add(e);

                    }

                }

            }


            // get the desired elements upone the selected scope by user to be porceed to next steps 
            if (scope == "model")
            {
                scopedElements.AddRange(AllModelElements);
            }
            else if (scope == "annotation")
            {
                scopedElements.AddRange(AllAnnotationElements);

            }
            else if (scope == "all")
            {
                scopedElements.AddRange(AllModelElements);
                scopedElements.AddRange(AllAnnotationElements);
            }
            else
            {
                scopedElements.AddRange(AllCustomElements);
            }



            string loggerFileText = "";

            //we will give a 70% of progresss bar to logg element
            //and 20% comparing modifing elements
            //and 10% to new elements 
            var portion = (double)(50d / (scopedElements.Count + 1));

            Helpers.Globals.progressBarValue = 0;

            var ss = Settings.Settings.LogPath;
            var ss2 = Settings.Settings.FullLogPath;


            //wwe wil loop to each element in our scoped element 
            foreach (var element in scopedElements)
            {
                Helpers.Globals.progressBarValue += portion;
                progrss1.UpdateProgressBarValue();

                logElement logElement = new logElement(element);

                // get the logger text as jsonl format
                var loggerOfElement = logElement.GetLoogerText(LoggerType.log);


                //we check if the logger text is not empty on the empty bounding box. this step is double check not to log any wrong element
                if (loggerOfElement == null)
                    continue;

                // finally we add this json line to the rest of lines to be exported as file in next steps 
                loggerFileText += $"{loggerOfElement}\n";



            }




            //generate UTC time format 
            var UTCdate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            //make sure if the directory of logger path + (projectName_projectNumber) is found, if not we will create it
            bool exist = Directory.Exists(Settings.Settings.FullLogPath);
            if (!exist)
                Directory.CreateDirectory(Settings.Settings.FullLogPath);


            //get external project id
            string ExternalProjectId = Settings.Settings.externalProjectID;
            if (string.IsNullOrEmpty(ExternalProjectId))
                ExternalProjectId = "00001";

            try
            {

                // if we save the project for the first time in save as command so we dont have any changes in change file
                if (_firstSave == true)
                {

                    File.WriteAllText(Settings.Settings.FullLogPath + $"{_doc.Title}_{modelGuid}_{ExternalProjectId}_{UTCdate}_change.jsonl", "");

                }

                //if we save it regurally so we have to check the files before to make cahnge file
                else
                {
                    // we get the change file inner text by comparing the previous files..  if there is no previous files the change file will be empty though.
                    var changeFileText = CompareLogFiles(scopedElements, _doc);

                    //writing the file with the changes
                    File.WriteAllText(Settings.Settings.FullLogPath + $"{_doc.Title}_{modelGuid}_{ExternalProjectId}_{UTCdate}_change.jsonl", changeFileText);

                }

                //finally we write the log file it self .. we print it last to not be get involved in the comparing last log files process as it would considered the last log files
                //so to avoid this by writing extra checking code we print it at last step.
                File.WriteAllText(Settings.Settings.FullLogPath + $"{_doc.Title}_{modelGuid}_{ExternalProjectId}_{UTCdate}_log.jsonl", loggerFileText);
                Helpers.Globals.progressBarValue = 101;
                progrss1.UpdateProgressBarValue();
            }
            catch (Exception ex)
            {

                TaskDialog.Show("error", ex.Message);
            }




        }



        private string CompareLogFiles(List<Element> scopedElements, Document document)
        {

            //we get all the files in the specific directory 
            var allFiles = Directory.GetFiles(Settings.Settings.FullLogPath);

            // a list to contain (.jsonl) files only among any other files in the directory
            var allLogFiles = new List<string>();


            //we go loop through them and filter the files with jsonl extension only
            foreach (var file in allFiles)
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = fileName.Split('.');
                var fileTitle = fileName.Substring(0, document.Title.Length);
                var fileNameSements = fileName.Split('_');


                if (fileExtension[fileExtension.Length - 1].ToLower() == "jsonl"
                        && fileTitle == document.Title
                        && fileNameSements[fileNameSements.Length - 1].ToLower() == "log.jsonl")
                {

                    allLogFiles.Add(file);
                }

            }



            // order the list of files to obtin the last file to be compared with
            allLogFiles = allLogFiles.OrderBy(x => x.Split('_')[3]).ToList();

            // if there is log files so it may be the user has change the folder of logging.
            // so there the change file would be empty and we stop the further steps
            if (allLogFiles.Count == 0)
            {
                Globals.progressBarValue = 100;
                progrss1.UpdateProgressBarValue();
                return "";
            }

            //get the last log file of the list
            var lastLogFile = allLogFiles.Last();

            //list to obtain any element has a change (created/modified/deleted)
            List<logElement> modifiedOrDeletedOrNewElements = new List<logElement>();

            //
            List<string> ElementsUniqueIdofLastLog = new List<string>();

            var text = File.ReadAllLines(lastLogFile);
            //we will give a 50% of progresss bar to logg element
            //and 40% comparing modifing elements
            //and 10% to new elements 
            var portion = 40d / (text.Length + 1);
            foreach (var line in text)
            {
                try
                {
                    Globals.progressBarValue += portion;
                    progrss1.UpdateProgressBarValue();
                    LoggedElementObject loggedElementObject = JsonConvert.DeserializeObject<LoggedElementObject>(line);
                    string lastLoggedElementUniqueId = loggedElementObject.ObjectIds[0];



                    string lastLoggedElementVersionGuid = loggedElementObject.ObjectIds[2];

                    // we try to get all the elements of last log 
                    Element element = document.GetElement(lastLoggedElementUniqueId);

                    // if the element is found and its current VersionGuid differs from last one we logged it so it has been modified in newer file 
                    if (element != null && element.VersionGuid.ToString() != lastLoggedElementVersionGuid)
                    {



                        logElement modifiedLogElement = new logElement(element);

                        modifiedLogElement.objectStatus = "Modified";

                        modifiedOrDeletedOrNewElements.Add(modifiedLogElement);



                    }
                    else if (element == null)
                    {

                        //if it's not found so it's must be deleted for sure
                        logElement modifiedLogElement = new logElement(loggedElementObject);
                        modifiedLogElement.objectStatus = "Deleted";
                        modifiedOrDeletedOrNewElements.Add(modifiedLogElement);
                    }





                    //we add them to compare them with current log to determine if there a new Elements Later
                    // all the elements in the currect model and not found in the last log file they are definitely new ones
                    ElementsUniqueIdofLastLog.Add(lastLoggedElementUniqueId);
                }
                catch (Exception ex)
                {
         

                    //if there any unproper manual edit by user it will lead to an error
                    //also some of elements named in feet and inches would cause a problem and invalidate the jsonl scheme
                    TaskDialog.Show("error", "This error would happen in comparing proccess of the log files.\n\nWe are sorry to stop this process due to this error. " +
                        "Make sure that the log files are not editited manually or any of model Elements names include special charachters such (\") for inch of (\') for feet.");
                    return "";
                }

            }

            // get the elements in the current model and not found in the log files
            // considering if the user changed the scope of the logging all the model elements will be considered new 
            // as the last log file doesn't contain such elements as it has diffrent scope when they was logged
            var newElemtList = scopedElements.Where(x => !ElementsUniqueIdofLastLog.Contains(x.UniqueId));


            //we will give a 70% of progresss bar to logg element
            //and 20% comparing modifing elements
            //and 10% to new elements 
            var lastPortion = 10d / (newElemtList.Count() + 1);

            // we loop through the new elements and add them to the list
            foreach (var newElement in newElemtList)
            {
                Helpers.Globals.progressBarValue += lastPortion;
                progrss1.UpdateProgressBarValue();
                logElement newLogElement = new logElement(newElement);
                newLogElement.objectStatus = "New";
                modifiedOrDeletedOrNewElements.Add(newLogElement);
            }


            //finally we loop through all modified list and log them 
            string changeTxt = "";
            foreach (var logEle in modifiedOrDeletedOrNewElements)
            {
                var loggerText = logEle.GetLoogerText(LoggerType.change);
                if (loggerText != null)
                    changeTxt += loggerText + "\n";
            }




            return changeTxt;

        }

        public override void Execute()
        {
            Log();
        }
    }

    public class logElement
    {
        private Element ele { get; set; }
        private String elementId { get; set; }
        private string elementUniqId { get; set; }
        private string elementVersionGUID { get; set; }


        private BoundingBoxXYZ elementBbx { get; set; }

        private string elementName { get; set; }

        private string elementCategoryName { get; set; }

        private bool elementViewSpecific { get; set; }


        public string objectStatus { get; set; }



        public logElement(Element element)
        {
            ele = element;
            elementId = element.Id.ToString();
            elementUniqId = element.UniqueId;
            elementVersionGUID = element.VersionGuid.ToString();
            elementName = element.Name;
            elementCategoryName = element.Category.Name;
            elementViewSpecific = element.ViewSpecific;



            View view = null;
            if (ele.OwnerViewId != null && ele.OwnerViewId.IntegerValue != -1)
            {
                view = ele.Document.GetElement(ele.OwnerViewId) as View;
            }

            elementBbx = ele.get_BoundingBox(view);


            if (ele is Level)
            {




                Level level = ele as Level;




                View3D Collector3D = new FilteredElementCollector(ele.Document).OfClass(typeof(View3D)).WhereElementIsNotElementType().Cast<View3D>().Where(x => x.IsTemplate == false).FirstOrDefault();
                View collectorView = new FilteredElementCollector(ele.Document).OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().Cast<ViewPlan>().Where(x => x.IsTemplate == false).FirstOrDefault();
                if (Collector3D == null)
                {
                    var Doc = ele.Document;
                    var direction = new XYZ(-1, 1, -1);
                    var collector = new FilteredElementCollector(Doc);
                    var viewFamilyType = collector.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                      .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);



                    using (Transaction ttNew = new Transaction(Doc, "abc"))
                    {
                        ttNew.Start();
                        var view3D = View3D.CreateIsometric(
                                          Doc, viewFamilyType.Id);

                        view3D.SetOrientation(new ViewOrientation3D(
                          direction, new XYZ(0, 1, 1), new XYZ(0, 1, -1)));


                        ttNew.Commit();
                        elementBbx = ele.get_BoundingBox(view3D);

                    }


                }
                else
                {
                    try
                    {
                        elementBbx = ele.get_BoundingBox(ele.Document.ActiveView);
                        elementBbx = ele.get_BoundingBox(Collector3D as View);

                    }
                    catch (Exception e)
                    {

                        TaskDialog.Show("error", e.Message);
                    }
                }


            }

        }

        public logElement(LoggedElementObject loggedElementObject)
        {

            elementUniqId = loggedElementObject.ObjectIds[0];
            elementId = loggedElementObject.ObjectIds[1];
            elementVersionGUID = loggedElementObject.ObjectIds[2];
            elementName = loggedElementObject.ObjectProperties[0];
            elementCategoryName = loggedElementObject.ObjectProperties[1];
            elementViewSpecific = loggedElementObject.ObjectProperties[2].ToLower() == "true" ? true : false;

            BoundingBoxXYZ boundingBox = new BoundingBoxXYZ();
            var bbMinStr = loggedElementObject.BBox[0][0].Replace("(", "").Replace(")", "").Split(',');
            boundingBox.Min = new XYZ(double.Parse(bbMinStr[0]), double.Parse(bbMinStr[1]), double.Parse(bbMinStr[2]));
            var bbMaxStr = loggedElementObject.BBox[1][0].Replace("(", "").Replace(")", "").Split(',');
            boundingBox.Max = new XYZ(double.Parse(bbMaxStr[0]), double.Parse(bbMaxStr[1]), double.Parse(bbMaxStr[2]));

            elementBbx = boundingBox;



        }

        public string GetLoogerText(LoggerType loggerType)
        {

            var projectNotes = Settings.Settings.ProjectNote;
            var userNotes = Settings.Settings.UserNote;
            string objectIds;

            string objectProps;
            string BBox;
            string _notes;
            string _objectStatus;
            string theLoggerText;
            if (elementBbx == null)
                return null;




            if (elementName.Contains("\""))
            {
                objectProps = $"\'ObjectProperties\': [\'{elementName}\',\'{elementCategoryName}\', \'{elementViewSpecific}\']";
                objectIds = $"\'ObjectIds\': [\'{elementUniqId}\',\'{elementId}\', \'{elementVersionGUID}\']";

                BBox = $"\'BBox\': [[\'{elementBbx.Min}\'],[\'{elementBbx.Max}\']]";


                _notes = $"\'Notes\': [\'{userNotes}\', \'{projectNotes}\']";

                _objectStatus = $"\'ObjectStatus\':\'{objectStatus}\'";

            }
            else
            {
                objectProps = $"\"ObjectProperties\": [\"{elementName}\",\"{elementCategoryName}\", \"{elementViewSpecific}\"]";
                objectIds = $"\"ObjectIds\": [\"{elementUniqId}\",\"{elementId}\", \"{elementVersionGUID}\"]";

                BBox = $"\"BBox\": [[\"{elementBbx.Min}\"],[\"{elementBbx.Max}\"]]";


                _notes = $"\"Notes\": [\"{userNotes}\", \"{projectNotes}\"]";

                _objectStatus = $"\"ObjectStatus\":\"{objectStatus}\"";
            }










            // determine if we write the change file or the log file
            if (loggerType == LoggerType.log)
            {
                theLoggerText = $"{{{objectIds}, {objectProps},{BBox}, {_notes}}}";
            }
            else
            {
                theLoggerText = $"{{{objectIds}, {objectProps},{BBox}, {_notes}, {_objectStatus}}}";
            }

            return theLoggerText;
        }


    }


    public enum LoggerType
    {
        log,
        change

    }




}
