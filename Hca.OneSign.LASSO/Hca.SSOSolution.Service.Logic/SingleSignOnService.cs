using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;
//using Hca.Common.Logging;
using Hca.Common.Logger;
using System.Diagnostics;
using Microsoft.Win32;

namespace Hca.SSOSolution.Service.Logic
{
    public class SingleSignOnService : ISingleSignOnService
    {
        #region publicVariables
        public bool hasSaved = false;
        public string argpath, argAsc, strRoleQuery, parameter;
        public string tab { get; set; }
        public string sortName { get; set; }
        public string buttonName { get; set; }
        public string userName { get; set; }


        String ADGroup;
        String MasterFileLocation;
        String iniFilesPath;
        String backUpFilePath;
        String launchpadPath;
        String SystemDrive = Environment.ExpandEnvironmentVariables("%SystemDrive%");
        CheckedBoxListClass chkLstClass = CheckedBoxListClass.Instance;
        SSODictionary dicRoleFile;
        SSODictionary dicBackupFile;
        SSODictionary dicTabsRoleFile;
        SSODictionary dicTabsBackupFile;
        SSODictionary dicSelectedTabs;
        #endregion

        #region LoadMethod
        public void Load(out string message)
        {
            string directory;
            try
            {
                dicRoleFile = new SSODictionary();
                dicBackupFile = new SSODictionary();
                dicTabsRoleFile = new SSODictionary();
                dicTabsBackupFile = new SSODictionary();
                var masterDictionary = new Dictionary<string, Dictionary<string, string>>();
                var slaveDictionary = new Dictionary<string, string>();
                iniFilesPath = SystemDrive + @"\ProgramData\Launchpad\";
                backUpFilePath = SystemDrive + @"\ProgramData\Launchpad\INIToolBackup\";
                launchpadPath = SystemDrive + @"\Program Files (x86)\Imprivata\OneSign Agent\x64\";

                ProcessCommandLine();
                SsoIniTool_ParseCommandLine();
                ReadLaunchpadIniToFindRoleQuery();
                WriteToConfigFile();
                ReadConfigFile();
                directory = backUpFilePath;
                var filename = "MasterWin7";

                var sb = new StringBuilder();
                sb.Append(directory);
                sb.Append(ADGroup);
                sb.Append(".txt");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                WriteBackupFile(ADGroup);

                if (MasterFileLocation.Substring(0, 1).Contains("�") && MasterFileLocation.Substring(MasterFileLocation.Length - 1, 1).Contains("�"))
                {
                    string temp = MasterFileLocation;
                    MasterFileLocation = temp.Substring(1, temp.Length - 2);
                }

                directory = MasterFileLocation;

                StringBuilder strBlder = new StringBuilder();
                strBlder.Append(directory);

                if (!directory.ToUpper().EndsWith(".INI"))
                {
                    if (!directory.EndsWith("\\"))
                    {
                        directory = directory + "\\";
                    }
                    strBlder.Append(filename);
                    strBlder.Append(".ini");
                }

                MasterFileLocation = strBlder.ToString();

                ReadTabSectionToDictionary(directory, filename, ref masterDictionary, ref slaveDictionary);

                dicRoleFile.SnippetDictionary = masterDictionary;
                dicTabsRoleFile.SsoStringDictionary = slaveDictionary;

                //Read in Backup File
                directory = backUpFilePath;
                filename = ADGroup;
                ReadTabSectionToDictionary(directory, filename, ref masterDictionary, ref slaveDictionary);

                dicBackupFile.SnippetDictionary = masterDictionary;
                dicTabsBackupFile.SsoStringDictionary = slaveDictionary;

                //var extraKeysInBackupFile;
                CompareDictionaries(dicRoleFile.SnippetDictionary, dicBackupFile.SnippetDictionary, out message);

                BuildListFromMasterFileTabs();

                CheckPreviouslySelectedTabs();



            }
            catch (Exception ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                message = "Error!:" + ex.Message + ex.StackTrace.IndexOf("LINE NUMBER".ToUpper()).ToString();
            }
        }

        private void CompareDictionaries(Dictionary<string, Dictionary<string, string>> roleFile, Dictionary<string, Dictionary<string, string>> backupFile, out string returnMessage)
        {
            var intersection = roleFile.Keys.Intersect(backupFile.Keys);
            var extraKeysInBackupFile = backupFile.Keys.Except(intersection);
            var test = extraKeysInBackupFile.Count();
            if (test > 0)
            {
                var message = new StringBuilder();
                message.AppendLine("The following buttons have been removed from lauchpad by your system administrator: ");
                foreach (var item in extraKeysInBackupFile)
                {
                    message.AppendLine(item);
                }
                returnMessage = message.ToString();
            }
            else
            {
                returnMessage = "";
            }
        }
        #endregion

        public void Save()
        {
            try
            {
                GetSelectedTabsFromCheckBoxListToDictionary();
                WriteSelectedTabsToFile(dicSelectedTabs.SsoStringDictionary, ADGroup);
                WriteBackupFile(ADGroup);
                hasSaved = true;
                KillLaunchpad();
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }
       
        #region SaveMethods
        private void GetSelectedTabsFromCheckBoxListToDictionary()
        {
            dicSelectedTabs = new SSODictionary
            {
                SsoStringDictionary = new Dictionary<string, string>()
            };

            //ALWAYS ADD THE INI TOOL
            //dicSelectedTabs.SsoStringDictionary.Add("SSOiniTool", GetTabNameByDisplayNameFromDictionary("SSOiniTool"));
            
            try
            {
                foreach (string item in this.chkLstClass.chkLstBox.CheckedItems)
                {
                    try
                    {
                        if (!dicSelectedTabs.SsoStringDictionary.ContainsKey(item.ToString()))
                        {
                            dicSelectedTabs.SsoStringDictionary.Add(item.ToString(), GetTabNameByDisplayNameFromDictionary(item.Trim()));
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                        logger.Error(ex, "Fatal Error");
                        throw ex;
                    }
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }
        private void CopyTabsFromMasterFile(string ADGroup, out StringBuilder stringBuilt)
        {
            StringBuilder sb = new StringBuilder();
            string temp;
            if (MasterFileLocation.Substring(0, 1).Contains("�") && MasterFileLocation.Substring(MasterFileLocation.Length - 1, 1).Contains("�"))
            {
                temp = MasterFileLocation;
                MasterFileLocation = temp.Substring(1, temp.Length - 2);
            }
            sb.Append(MasterFileLocation);
            //sb.Append("Master.ini");
            string masterPath = sb.ToString();
            stringBuilt = new StringBuilder();
            try
            {
                using (StreamReader file = new StreamReader(masterPath))
                {
                    string line = file.ReadLine();
                    while (!file.EndOfStream)
                    {
                        line = line.Trim();
                        if (line.StartsWith("[") && !line.ToUpper().Equals("[TABS]"))
                        {
                            stringBuilt.AppendLine(line);
                            while (!file.EndOfStream)
                            {
                                line = file.ReadLine();
                                stringBuilt.AppendLine(line);
                            }
                        }
                        line = file.ReadLine();
                    }
                    file.Close();
                    file.Dispose();
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }

        private void WriteBackupFile(string ADGroup)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(backUpFilePath);
            sb.Append(ADGroup);
            sb.Append(".txt");
            string path = sb.ToString();
            sb = new StringBuilder();
            sb.Append(iniFilesPath);
            sb.Append(ADGroup);
            sb.Append(".ini");
            string path2 = sb.ToString();

            try
            {
                File.Copy(path2, path, true);
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }

        private void WriteSelectedTabsToFile(Dictionary<string, string> _SelectedTabs, string ADGroup)
        {
            StringBuilder stringBuilt;
            CopyTabsFromMasterFile(ADGroup, out stringBuilt);
            StringBuilder sb = new StringBuilder();
            sb.Append(iniFilesPath);
            sb.Append(ADGroup);
            sb.Append(".ini");
            string path = sb.ToString();
            int count = 1;
            sb = new StringBuilder();
            try
            {
                // Write single line to new file
                using (StreamWriter sw = new StreamWriter(path, false, Encoding.ASCII))
                {
                    sb.AppendLine("[TABS]");

                    foreach (var pair in _SelectedTabs)
                    {
                        string temp = "";
                        sb.Append(count.ToString());
                        sb.Append(" = ");
                        if (pair.Value.Length > 0)
                        {
                            temp = pair.Value.Substring(1, pair.Value.Length - 2);
                        }
                        sb.AppendLine(temp);
                        count++;
                    }

                    sw.Write(sb.ToString() + stringBuilt.ToString());
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }
        #endregion

        #region LoadMethods

        private void ReadTabSectionToDictionary(string directory, string filename, ref Dictionary<string, Dictionary<string, string>> masterDictionary, ref Dictionary<string, string> slaveDictionary)
        {
            masterDictionary = new Dictionary<string, Dictionary<string, string>>();
            slaveDictionary = new Dictionary<string, string>();
            //changed to use "ADGROUP".ini instead of MASTER.ini
            StringBuilder sb = new StringBuilder();
            sb.Append(directory);
            
            if (directory.ToUpper().Contains("INITOOLBACKUP"))
            {
                sb.Append(filename);
                sb.Append(".txt");
            }
            else
            {
                if (!directory.ToUpper().Contains(".INI"))
                {
                    sb.Append(filename);
                    sb.Append(".ini");
                }
            }
            string masterPath = sb.ToString();
            string path = masterPath;
            if (argAsc.Equals("Y"))
            {
                path = backUpFilePath + @"AF.txt";
                ParseFileAscending(masterPath);
            }
            //MessageBox.Show("Path is " + path);
            bool doneWithTabs = false;

            masterDictionary.Add("[TABS]", slaveDictionary);
            try
            {
                using (StreamReader file = new StreamReader(path))
                {
                    string line = file.ReadLine();
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        line = line.Trim();

                        int formerIndex;
                        int index = 0;
                        string[] splitLine;
                        string tab = "";
                        while (!line.StartsWith("[") && !line.ToUpper().Contains("TABS") && doneWithTabs == false)
                        {

                            if (!line.StartsWith(";") && line.Contains("="))
                            {

                                splitLine = line.Split('=');

                                if (Int32.TryParse(splitLine[0].Trim().ToString(), out index))
                                {
                                    formerIndex = index;
                                    index = Convert.ToInt32(splitLine[0].Trim());
                                }
                                else
                                {
                                    break;
                                }

                                //check to see if number is sequencial.
                                if (index == formerIndex)
                                {
                                    //number is sequencial, continue processing
                                }
                                else
                                {
                                    //number is not sequencial, break out, because launchpad isn't formatted properly.
                                    break;
                                }
                                

                                tab = splitLine[1].Trim().ToString();

                                if (!slaveDictionary.ContainsKey(index.ToString()))
                                {
                                    slaveDictionary.Add(index.ToString().Trim(), tab.Trim());
                                }

                            }
                            line = file.ReadLine();
                        }
                        doneWithTabs = true;
                        if (line.StartsWith("[") && !line.ToUpper().Contains("TABS"))
                        {
                            slaveDictionary = new Dictionary<string, string>();
                        }
                        string readLine = line.ToString();
                        if (readLine.Contains("=") || readLine == "")
                        {
                            if (readLine != "" && !readLine.StartsWith(";"))
                            {
                                splitLine = readLine.Split('=');
                                if (!slaveDictionary.ContainsKey(splitLine[0].ToString().Trim()))
                                {
                                    slaveDictionary.Add(splitLine[0].ToString().Trim(), splitLine[1].ToString().Trim());
                                }
                            }
                        }
                        else if (!line.StartsWith(";"))
                        {
                            if (!masterDictionary.ContainsKey(line.ToString().Trim()))
                            {
                                masterDictionary.Add(line.ToString().Trim(), slaveDictionary);
                            }
                        }
                    }
                    file.Close();
                    file.Dispose();
                }
                if (parameter != null)
                {
                    if (parameter.Equals("Y") || parameter.Equals("y"))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }

        private void WriteToConfigFile()
        {
            try
            {
                if (File.Exists("C:\\ProgramData\\Launchpad\\config.ini"))
                {
                    File.Delete("C:\\ProgramData\\Launchpad\\config.ini");
                }
                StreamWriter AFile = new StreamWriter("C:\\ProgramData\\Launchpad\\config.ini", true, Encoding.ASCII);
                AFile.WriteLine(strRoleQuery);
                AFile.WriteLine(argpath);
                AFile.Close();
            }
            catch(Exception e)
            {
            }
        }

        private void ReadConfigFile()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append(iniFilesPath);
                sb.Append("config.ini");
                TextReader rdr = new StreamReader(sb.ToString());
                ADGroup = rdr.ReadLine();
                MasterFileLocation = rdr.ReadLine();
                rdr.Close();
                if (ADGroup == "" || MasterFileLocation == "")
                {
                    throw new ApplicationException("Error reading configuration file - could not get ADGroup or Master File path from the bridge.");
                    HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                    logger.Error("Fatal Error");
                    MessageBox.Show("Error: No role assigned to user!!");
                    Application.Exit();
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                MessageBox.Show("User not present in the Vault." + "\n" +  "Contact Service Desk to be added to an ADGroup", "Button Manager Error");
                Application.Exit();
            }
        }

        private void ParseTabsInRoleFile()
        {
            dicTabsRoleFile.SsoStringDictionary = new Dictionary<string, string>();

            string filename = ADGroup;
            StringBuilder sb = new StringBuilder();
            sb.Append(MasterFileLocation);

            string path = sb.ToString();
            
            //over-arching try block to catch any exceptions
            try
            {

                using (StreamReader file = new StreamReader(path))
                {
                    while (!file.EndOfStream)
                    {
                        string line = file.ReadLine();
                        line = line.Trim();
                        if (line.StartsWith("[") && !line.Contains("TABS"))
                        {

                            string tab = line.Substring(1, line.Length - 2);

                            string name = GetDisplayName(tab);
                            if (!dicTabsRoleFile.SsoStringDictionary.ContainsKey(tab.Trim()))
                            {
                                dicTabsRoleFile.SsoStringDictionary.Add(tab.Trim(), name.Trim());
                            }

                        }
                        else if (line.StartsWith("[") && !line.ToUpper().Equals("[TABS]"))
                        {
                            break;
                        }
                    }
                    file.Close();
                    file.Dispose();
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }

        public string GetDisplayName(string tab)
        {
            try
            {
                string filename = ADGroup;
                string line;
                string displayName = "MISSING";
                string[] splitString;
                StringBuilder sBuilder = new StringBuilder();
                sBuilder.Append(iniFilesPath);
                sBuilder.Append(filename);
                sBuilder.Append(".ini");
                string path = sBuilder.ToString();
                using (StreamReader file = new StreamReader(path))
                {
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        line = line.Trim();
                        if (line.StartsWith("[") && !line.ToUpper().Equals("[TABS]") && line.Substring(1, line.Length - 2).Trim().ToUpper().Equals(tab.Trim().ToUpper()))
                        {
                            //begin reading until you find another '['
                            //read in the text inside the [] to get the matching tab name:
                            line = file.ReadLine();

                            if (line.Trim().ToUpper().StartsWith("DISPLAYNAME"))
                            {
                                splitString = line.Split('=');
                                displayName = splitString[1].ToString();
                                break;
                            }
                        }
                    }
                    file.Close();
                    file.Dispose();
                }
                return displayName;
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                return "MISSING";
                throw ex;
            }

        }

        public void ParseTabsInBackupFile()
        {
            dicTabsBackupFile.SsoStringDictionary = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            string directory = backUpFilePath;
            sb.Append(directory);
            sb.Append(ADGroup);
            sb.Append(".txt");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (File.Exists(sb.ToString()))
            {
                //continue
            }
            else
            {
                WriteBackupFile(ADGroup);
            }
            int formerIndex;
            int index = 0;
            string path = sb.ToString();
            //over-arching try block to catch any exceptions
            try
            {
                using (StreamReader file = new StreamReader(path))
                {
                    while (!file.EndOfStream)
                    {
                        string line = file.ReadLine();
                        line = line.Trim();
                        if (!line.StartsWith(";") && line.Contains("="))
                        {
                            if (line.StartsWith("[") && !line.ToUpper().Equals("[TABS]"))
                            {
                                break;
                            }
                            string[] splitLine = line.Split('=');

                            try
                            {
                                formerIndex = index;

                                if (Int32.TryParse(splitLine[0].Trim().ToString(), out index))
                                {
                                    index = Convert.ToInt32(splitLine[0].Trim());
                                }
                                else
                                {
                                    break;
                                }

                                //check to see if number is sequencial.
                                if (index - 1 == formerIndex)
                                {
                                    //number is sequencial, continue processing
                                }
                                else
                                {
                                    //number is not sequencial, break out, because launchpad isn't formatted properly.
                                    break;
                                }
                            }
                            catch (ApplicationException ex)
                            {
                                //index is not a number. Catch the error but move on.
                                //LASSOErrorHandler er = new LASSOErrorHandler();
                                //er.LogError(ex);
                            }
                            string tab = splitLine[1].Trim().ToString();
                            if (!dicTabsBackupFile.SsoStringDictionary.ContainsKey(tab))
                            {
                                dicTabsBackupFile.SsoStringDictionary.Add(tab, GetDisplayName(tab));
                            }
                        }
                    }
                    file.Close();
                    file.Dispose();
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        
        }

        public void ParseFileAscending(string Path)
        {
            
            try
            {
                List<AscendFile> AF = new List<AscendFile>();
                bool doneWithTabs = false;
                string TempFile = SystemDrive + @"\ProgramData\Launchpad\INIToolBackup\" + @"\AF.txt";
                //Environment.ExpandEnvironmentVariables("%temp%") + @"\AF.txt";
                StreamWriter AFile = new StreamWriter(TempFile, false, Encoding.ASCII);
                
                using (StreamReader file = new StreamReader(Path))
                {
                    AFile.WriteLine("[TABS]");
                    while (!file.EndOfStream)
                    {
                        string line = file.ReadLine();
                        line = line.Trim();
                        
                        var AscendFile = new AscendFile();
                        if (!doneWithTabs && !line.StartsWith("["))
                        {
                            AFile.WriteLine(line);
                        }

                        if (line.StartsWith("[") && !line.ToUpper().Equals("[TABS]"))
                        {
                            doneWithTabs = true;
                            tab= line;
                            AscendFile.tabName = line;
                            AscendFile.props = "AAAA";
                            AscendFile.ascName = "BBBB";
                            AF.Add(AscendFile); 
                        }
                        else if (!line.StartsWith(";") && line.Contains("=") && doneWithTabs)
                        {
                            if (line.StartsWith("DisplayName"))
                            {
                                sortName = line.Substring(line.IndexOf("=")).Trim();
                                AscendFile.ascName = sortName;
                            }
                            else
                            {
                                AscendFile.ascName = sortName;
                            }
                            AscendFile.tabName = tab;
                            AscendFile.props = line;
                            AF.Add(AscendFile);
                        }
                    }
                    file.Close();
                    file.Dispose();
                }
                AF.Reverse();
                foreach(AscendFile l in AF)
                {
                    if (l.props.StartsWith("DisplayName"))
                    {
                        sortName =l.ascName;
                        buttonName =l.tabName;
                    }
                    if (l.props=="AAAA" && l.tabName == buttonName)
                    {
                        l.ascName=sortName;
                    }
                }
                AF.Reverse();
    
                AF = AF.OrderBy(l => l.ascName).ToList();

                foreach (AscendFile l in AF)
                {
                    if (l.props == "AAAA")
                    {
                        AFile.WriteLine("");
                        AFile.WriteLine(l.tabName);
                    }
                    else
                    {
                        AFile.WriteLine(l.props);
                    }
                }
                AFile.Close();
            }

            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }

        }

        private void BuildListFromMasterFileTabs()
        {
            try
            {
                foreach (var pair in dicRoleFile.SnippetDictionary)
                {
                    if (!pair.Key.ToUpper().Contains("TABS") && pair.Value.ContainsKey("DisplayName"))
                    {
                        if ((pair.Key.ToUpper().Contains("SSOINITOOL")) || (pair.Key.ToUpper().Contains("REQD")))
                        {
                            chkLstClass.chkLstBox.Items.Add(pair.Value["DisplayName"].ToString().Trim(), CheckState.Indeterminate);
                            
                        }
                        else
                        {
                            chkLstClass.chkLstBox.Items.Add(pair.Value["DisplayName"].ToString().Trim());
                        }
                    }
                    
                }
            }
            catch (ApplicationException ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                throw ex;
            }
        }

        private void CheckPreviouslySelectedTabs()
        {
            Dictionary<string, string> tempDictionary = new Dictionary<string,string>();
            dicBackupFile.SnippetDictionary.TryGetValue("[TABS]", out tempDictionary);
            for (int i = 0; i < chkLstClass.chkLstBox.Items.Count; i++)
            {
                foreach (var pair in tempDictionary)
                {
                    if (chkLstClass.chkLstBox.Items[i].ToString().Trim().Equals((GetDisplayName(pair.Value.ToString()).Trim())))
                    {
                        chkLstClass.chkLstBox.SetItemChecked(i, true);
                    }
                }
            }
        }


        private void KillLaunchpad()
        {
            try
            {
                string tempPath;
                string processName = "LP";
                try
                {
                    foreach (Process p in Process.GetProcessesByName(processName))
                    {
                        p.Kill();
                    }
                }
                catch
                {
                    // Log error.
                }
                if (File.Exists(launchpadPath + "LP.exe"))
                {
                    processName = "lp";
                    tempPath = launchpadPath + "LP.exe";
                    
                    Process.Start(tempPath);
                }
                            }
            catch (Exception ex)
            {
                HcaLogger logger = HcaLogger.GetLogger("LASSOLogger");
                logger.Error(ex, "Fatal Error");
                MessageBox.Show("Error restarting Launchpad. You can close and restart Launchpad manually to see your changes. Error: " + ex.Message);
            }
        }

        public string ComposeHelpMessage()
        {
            StringBuilder returningMessage = new StringBuilder();
            returningMessage.AppendLine(
                "Button Manager shows a list of all available bridges represented as checkboxes."
                + " Use the checkboxes to select the bridges you want on your Launchpad."
                + " When you're done, click 'Save & Close'. Launchpad will be restarted so your changes can take effect. " 
                + " If Launchpad does not restart, logout and login again.");
            returningMessage.AppendLine("");
            returningMessage.AppendLine("To exit without saving, click Cancel.");

            string returnMessage = returningMessage.ToString();

            return returnMessage;
        }

        public string ComposeNoGroupMessage()
        {
            StringBuilder returningMessage = new StringBuilder();
            returningMessage.AppendLine(
                "Button Manager did not find a matching role file at C:\\ProgramData\\Launchpad."
                + " If your role files are on a share, please copy the file to"
                + " C:\\ProgramData\\Launchpad folder, logoff and logback in and "
                + " rerun Button Manager.");

            string returnMessage = returningMessage.ToString();

            return returnMessage;
        }

        public void ProcessCommandLine( ) {
	
	    string commandLine_orig;

	    int pos1, pos2;
	    try {
		    commandLine_orig = argpath;        
            if (!commandLine_orig.ToUpper().EndsWith(".INI"))
            {
                if (!commandLine_orig.EndsWith("\\"))
                {
                    commandLine_orig = commandLine_orig + "\\";
                }
            }
		    pos1 = commandLine_orig.IndexOf(".ini");
		    pos2 = commandLine_orig.IndexOf("-");
		    if ((pos1 == -1) && (commandLine_orig == null))
		    {
                argpath = "\\\\hca\\initool\\MasterFile\\MasterWin7.ini";
		    }
		    if ((pos1 == -1) && (commandLine_orig != null))
		    {
			    if (pos2 > -1)
			    {
                    argpath = commandLine_orig.Substring(0, pos2 - 1).Trim() + "MasterWin7.ini";
			    }
			    else
			    {
                    argpath = commandLine_orig + "MasterWin7.ini";
			    }
		    }
		    if ((pos1 > -1))
		    {
                argpath = commandLine_orig.Substring(0, pos1 + 4);
		    }
	    }
	catch (Exception e) {

	}
}

     public void SsoIniTool_ParseCommandLine()
    {
	    var sCommandLine = argAsc;
        string str, tempStr;
        bool bool1, bool2;
        bool1 = sCommandLine.ToLower().Contains("appparams =");
        bool2 = sCommandLine.ToLower().Contains("appparams=");
        str = "N";
	    tempStr = sCommandLine.Substring(sCommandLine.IndexOf("=") + 1, (sCommandLine.Length - sCommandLine.IndexOf("=") - 1)).Trim();
         
        if (bool1 && bool2)
	    {
		    str = "N";
	    }
        
        if (bool1 || bool2)
	    {
            
            if (tempStr.ToUpper().Contains("ASC"))
            {
                str = "Y";
            }
            else
            {
                str = "N";
            }
	    }

        if (!tempStr.ToUpper().Contains("ASC"))
	    {
		    str ="N";
	    }
        argAsc= str;
    }

    public void ReadLaunchpadIniToFindRoleQuery() {

        string masterPath = "C:\\ProgramData\\Launchpad\\Launchpad.ini";
        string path = masterPath, roleQuery;
        StringBuilder stringBuilt = new StringBuilder();
        try
            {
                using (StreamReader file = new StreamReader(path))
                {
                    string line = file.ReadLine();
                    string strParameter;
                    while (!file.EndOfStream)
                    {
                        //line = line.Trim();
                        if(line != "")
                            {
                                if (line.Substring(0, 1) != ";" && line.Substring(0, 1) != ":" && line.IndexOf("=") != -1)
                                {

                                    int equalsIndex = line.IndexOf("=");
                                    strParameter = line.Substring(0, equalsIndex - 1);
                                    if (strParameter.ToUpper() == "ROLEQUERY")
                                    {
                                        roleQuery = line.Substring(equalsIndex + 1, line.Length - (equalsIndex + 1)).Trim();
                                        ReadSSOGroups(roleQuery);
                                        break;
                                    }
                                }
                            }
                            line = file.ReadLine();
                        }
                    file.Close();
                    file.Dispose();
                }

	    }
	    catch (Exception e)
	    {
		    
	    }
    }

   
     public void ReadSSOGroups(string roleQuery)
    {
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "C:\\Program Files (x86)\\Imprivata\\OneSign Agent\\x64\\IsxMenu",
                    Arguments = "getloggedinuser",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                 userName = proc.StandardOutput.ReadLine();
            }
                List<string> userGroups = new List<string>();
                userGroups = GetGroups(userName, roleQuery.ToUpper());
                var groupini = new StringBuilder();
                int x = 0;
                
                for (int i=0; i<userGroups.Count; i++)
                {
                    groupini.Length = 0;
                    groupini.Append(iniFilesPath);
                    groupini.Append(userGroups[i]);
                    groupini.Append(".ini");
                    if (File.Exists(groupini.ToString()))
                    {
                        ADGroup = userGroups[i];
                        strRoleQuery= userGroups[i];
                        x++;
                        break;
                    }

                }
                if(x==0)
                {
                    MessageBox.Show("" + "Button Manager did not find a matching role file at" + "\n" 
                + "C:\\ProgramData\\Launchpad." + "\n\n"
                + " If your role files are on a share, please copy the file to" + "\n"
                + " C:\\ProgramData\\Launchpad folder, logoff and logback " + "\n"
                + " in and rerun Button Manager.", "Button Manager Error");
                    Application.Exit(); 
                }
        }
        catch(Exception e)
        {

        }
    }
    private List<string> GetGroups(string userName, string roleQuery)
    {
            List<string> result = new List<string>();
            WindowsIdentity wi = new WindowsIdentity(userName);
 
            string groupName;
            int pos = 0;
          foreach (IdentityReference group in wi.Groups)
          {
               try
               {
                    groupName = group.Translate(typeof(NTAccount)).ToString();
                    
                    if (groupName.ToUpper().Contains(roleQuery))
                    {
                        pos = groupName.IndexOf("\\");
                        groupName = groupName.Substring(pos + 1);
                        if (groupName.ToUpper() != roleQuery && groupName.ToUpper() !="CORPSSOHCA")
                        {
                            result.Add(groupName.Substring(7));
                        }
                    }
               }
               catch (Exception ex) { }
          }
          result.Sort();
          return result;
    }     

        #endregion

        #region Dictionary Handler
        private string GetTabNameByDisplayNameFromDictionary(string item)
        {
            string value = "";
            foreach (var pair in dicRoleFile.SnippetDictionary)
            {
                if (pair.Key != "[TABS]")
                {
                    foreach (var secondPair in pair.Value)
                    {

                        if (item == secondPair.Value.ToString())
                        {
                            //verify we are looking at display name
                            if (secondPair.Key.ToString() == "DisplayName")
                            {
                                value = pair.Key.ToString();
                                break;
                            }
                        }
                    }
                }
                if (value != "")
                {
                    break;
                }
            }
            return value.Trim();
        }
        #endregion
        
    }
}
