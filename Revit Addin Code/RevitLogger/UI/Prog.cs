﻿using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using Helpers;
using RevitLogger.Helpers;

namespace RevitLogger.UI
{
    public partial class Prog : Form
    {
        private Document _document;
        private bool _firstSave = false;
        public Prog(Document document, bool FirstSave)
        {
            InitializeComponent();
            _document = document;
            _firstSave = FirstSave;
            Globals.progressBarValue = 0;
        }

        private void Prog_Load(object sender, EventArgs e)
        {
         
        }

        private void logElementProgress(bool firstSave)
        {

            Guid id = Guid.NewGuid();
            String sID = id.ToString();
            if (Logger.modelGuid == null)
                Logger.modelGuid = sID;
            Logger logger = new Logger(this, _document, firstSave);
            logger.Log();


        }
        public void UpdateProgressBarValue()
        {
            if (Globals.progressBarValue <= 100)
            {
            var intVal = (int)Globals.progressBarValue;
                progressBar1.Value = intVal;
                lblProg.Text = $"Log Progress.. ({intVal}%)";
            }

            if (Globals.progressBarValue > 100)
            {
                progressBar1.Value = 100;
                lblProg.Text = $"Log Progress.. Done(100%)";
                Task.Delay(1500);
                Close();
                progressBar1.Value = 0;
                Globals.progressBarValue = 0;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            logElementProgress(_firstSave);
        }
    }
}
