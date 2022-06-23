using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Hca.SSOSolution.Service;
using Hca.Common.Logging;
using Hca.SSOSolution.Service.Logic;
using System.Diagnostics;
using System.Timers;
using Microsoft.Win32;

namespace Hca.SSOSolution.LASSO.UI
{
    public partial class LASSOForm : Form
    {
        public LASSOForm()
        {
            InitializeComponent();
        }
        SingleSignOnService ssoService = new SingleSignOnService();
        
        
        private void LASSO_Load(object sender, EventArgs e)
        {
            CheckGreyOut();
            ssoService.argAsc = Program.argParam;
            ssoService.argpath = Program.argPath;
            string message = "";

            string errorMessage = "Please provide a path to the master file location:" + "\n" + "\n" +
                    "i.e. ButtonManager.exe \"\\\\uncshare\\folder\\\\\""+ "\n" + "\n" +
                    "Optionally, you can specify AppParams=ASC to sort alphabetically." + "\n" + "\n" +
                    "i.e. ButtonManager.exe \"\\\\uncshare\\folder\\\\\" \"AppParams=ASC\""+ "\n" + "\n" +
                    "It should work with or without AppParams being specified."; 

            if (ssoService.argpath == null)
            {
                MessageBox.Show(errorMessage, "Button Manager", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Application.Exit();
            }
            CheckedBoxListClass chkListClass = CheckedBoxListClass.Instance;   
            chkListClass.chkLstBox = chklstTabList;
            try
            {
                ssoService.Load(out message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                Application.Exit();
            }
            if (message.Trim() != "")
            {
                if (message == "Unable to run program on genshare PC's. Exiting.")
                {
                    MessageBox.Show(message.ToString());
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show(message.ToString());
                }
            }
        }

        private void CheckGreyOut()
        {
            if (ssoService.hasSaved == false)
            {
                //buttonSave.Enabled = false;
            }
            else if (ssoService.hasSaved == true)
            {
                //buttonSave.Enabled = true;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                ssoService.Save();
                //LockButtonForOneSecond();
                //System.Threading.Thread.Sleep(2000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in saving: " + ex.Message);
            }
            buttonCancel.Text = "Cancel";
            //buttonSave.Enabled = false;
        }

        System.Timers.Timer time = new System.Timers.Timer();
        private void LockButtonForOneSecond()
        {
            buttonSave.Enabled = false;
            
            time.Elapsed+=new ElapsedEventHandler(OnTimedEvent);
            time.Interval = 1000;
            time.Enabled = true;
            
        }

        delegate void SetTextCallback(bool enabled);
        private void SetText(bool enabled)
        {
            this.buttonSave.Enabled = enabled;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            time.Enabled = false;

            if (this.buttonSave.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { true });
            }
            else
            {
                buttonSave.Enabled = true;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            string message = ssoService.ComposeHelpMessage();
            MessageBox.Show(message, "How to use Button Manager");
            
        }

        private void chklstTabList_Click(object sender, EventArgs e)
        {
            ssoService.hasSaved = true;
            CheckGreyOut();
        }

        private void chklstTabList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.CurrentValue == CheckState.Indeterminate) { e.NewValue = CheckState.Indeterminate; } 
        }
    }
}
