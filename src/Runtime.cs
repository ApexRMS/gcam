// gcam: SyncroSim Base Package for the Global Change Assessment Model (GCAM).
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

using System;
using System.IO;
using System.Data;
using SyncroSim.Core;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Specialized;

namespace SyncroSim.GCAM
{
    class Runtime : Transformer
    {
        private DataSheet m_StratumDataSheet;
        private DataSheet m_StateLabelXDataSheet;
        private DataSheet m_StateLabelYDataSheet;
        private DataSheet m_StateLabelZDataSheet;
        private DataSheet m_InputFileDataSheet;
        private DataSheet m_LandAllocationDataSheet;
        private string m_GCAMAppFolderName;
        private string m_GCAMExeFolderName;
        private string m_GCAMDatabaseFolderName;
        private string m_GCAMConfigurationFileName;
        private string m_GCAMConfigurationFileNameSource;
        private string m_GCAMPolicyTargetFileName;
        private string m_GCAMRunModelBatchFileName;
        private string m_GCAMDetailedLandAllocationQueryFileName;
        private string m_GCAMDetailedLandAllocationQueryXMLBatchFileName;
        private string m_GCAMDetailedLandAllocationQueryBatchFileName;
        private string m_OutputFolderName;
        private bool m_IsUserInteractive;

        public override void Transform()
        {
            this.InitDataSheets();
            this.ValidateDefinitions();
            this.InitGCAMPaths();
            this.InitOutputFolder();
            this.InitUserInteractive();
            this.NormalizeInputFiles();
            this.InitInputFiles();
            this.InitRuntimeFiles();
            this.ConfigureRunControl();
            this.RunGCAMBatchFiles();
            this.ImportDetailedLandAllocationData();
        }

        private void InitDataSheets()
        {
            this.m_StratumDataSheet = this.Project.GetDataSheet(Shared.STRATUM_DATASHEET_NAME);
            this.m_StateLabelXDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_X_DATASHEET_NAME);
            this.m_StateLabelYDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_Y_DATASHEET_NAME);
            this.m_StateLabelZDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_Z_DATASHEET_NAME);
            this.m_InputFileDataSheet = this.ResultScenario.GetDataSheet(Shared.INPUT_FILE_DATASHEET_NAME);
            this.m_LandAllocationDataSheet = this.ResultScenario.GetDataSheet(Shared.DETAILED_LAND_ALLOCATION_DATASHEET_NAME);
        }

        private void ValidateDefinitions()
        {
            if (this.m_StratumDataSheet.GetData().Rows.Count == 0 ||
                this.m_StateLabelXDataSheet.GetData().Rows.Count == 0 ||
                this.m_StateLabelYDataSheet.GetData().Rows.Count == 0 ||                
                this.m_StateLabelZDataSheet.GetData().Rows.Count == 0)
            {
                throw new ArgumentException("The GCAM definitions are missing.");
            }
        }

        private void InitGCAMPaths()
        {
            this.m_GCAMAppFolderName = this.GetGCAMAppFolderName();
            this.m_GCAMExeFolderName = this.GetGCAMExeFolderName();
            this.m_GCAMDatabaseFolderName = this.GetGCAMDatabaseFolderName();
        }

        private void InitUserInteractive()
        {
            DataSheet ds = this.Library.GetDataSheet(Shared.APPLICATION_DATASHEET_NAME);
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr[Shared.APPLICATION_DATASHEET_USER_INTERACTIVE_COLUMN_NAME] == DBNull.Value)
            {
                this.m_IsUserInteractive = false;
            }
            else
            {
                this.m_IsUserInteractive = Booleans.BoolFromValue(
                    dr[Shared.APPLICATION_DATASHEET_USER_INTERACTIVE_COLUMN_NAME]);
            }
        }

        private void InitOutputFolder()
        {
            string f = this.Library.GetFolderName(LibraryFolderType.Output, this.ResultScenario, true);

            f = Path.Combine(f, "GCAM");

            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
            }

            this.m_OutputFolderName = f;
        }

        private void NormalizeInputFiles()
        {
            bool HasChanges = false;
            DataRow dr = this.m_InputFileDataSheet.GetDataRow();

            if (dr == null)
            {
                DataTable dt = this.m_InputFileDataSheet.GetData();

                dr = dt.NewRow();
                dt.Rows.Add(dr);
            }

            if (dr[Shared.INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME] == DBNull.Value)
            {
                dr[Shared.INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME] = Path.Combine(this.m_GCAMExeFolderName, "configuration_usa.xml");
                HasChanges = true;
            }

            if (HasChanges)
            {
                this.m_InputFileDataSheet.Changes.Add(new ChangeRecord(this, "Normalized"));
            }
        }

        private void InitInputFiles()
        {
            DataRow dr = this.m_InputFileDataSheet.GetDataRow();

            this.m_GCAMConfigurationFileNameSource = Convert.ToString(dr[Shared.INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME]);

            if (!File.Exists(this.m_GCAMConfigurationFileNameSource))
            {
                throw new ArgumentException("The configuration file does not exist: " + this.m_GCAMConfigurationFileNameSource);
            }

            if (dr[Shared.INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME] != DBNull.Value)
            {
                this.m_GCAMPolicyTargetFileName = Convert.ToString(dr[Shared.INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME]);

                if (!File.Exists(this.m_GCAMPolicyTargetFileName))
                {
                    throw new ArgumentException("The policy target file does not exist: " + this.m_GCAMPolicyTargetFileName);
                }
            }
        }

        private void InitRuntimeFiles()
        {
            this.m_GCAMConfigurationFileName = this.CreateGCAMConfigurationFile();
            this.m_GCAMRunModelBatchFileName = this.CreateRunGCAMBatchFile();
            this.m_GCAMDetailedLandAllocationQueryFileName = this.CreateDetailedLandAllocationQuery();
            this.m_GCAMDetailedLandAllocationQueryXMLBatchFileName = this.CreateDetailedLandAllocationBatchFile();
            this.m_GCAMDetailedLandAllocationQueryBatchFileName = this.CreateDetailedLandAllocationCMDFile();
        }

        private string CreateGCAMConfigurationFile()
        {
            string CopyConfigFileName = Path.Combine(this.m_OutputFolderName, Path.GetFileName(this.m_GCAMConfigurationFileNameSource));

            using (StreamReader s = new StreamReader(this.m_GCAMConfigurationFileNameSource))
            {
                string line;

                using (StreamWriter t = new StreamWriter(CopyConfigFileName))
                {
                    while ((line = s.ReadLine()) != null)
                    {
                        if (this.m_GCAMPolicyTargetFileName != null)
                        {
                            if (line.Contains("policy-target-file"))
                            {
                                line = string.Format(CultureInfo.InvariantCulture,
                                    "		<Value name=\"policy-target-file\">{0}</Value>",
                                    this.m_GCAMPolicyTargetFileName);
                            }
                        }
                   
                        t.WriteLine(line);
                    }
                }
            }

            return CopyConfigFileName;
        }

        private string CreateRunGCAMBatchFile()
        {
            string BatchFileName = Path.Combine(this.m_OutputFolderName, "run-gcam.bat");

            using (StreamWriter t = new StreamWriter(BatchFileName))
            {
                t.WriteLine("@echo off");
                t.WriteLine("cd \"{0}\"", this.m_GCAMExeFolderName);

                t.WriteLine(@"SET CLASSPATH=..\libs\jars\*;XMLDBDriver.jar");
                t.WriteLine("IF NOT DEFINED JAVA_HOME FOR /F \"delims=\" %%O IN ('java XMLDBDriver --print-java-home') DO @SET JAVA_HOME=%%O");
                t.WriteLine("IF DEFINED JAVA_HOME (");

                t.WriteLine(@"SET PATH=%JAVA_HOME%\bin;%JAVA_HOME%\bin\server");
                t.WriteLine("Objects-Main.exe -C \"{0}\"", this.m_GCAMConfigurationFileName);
                t.WriteLine(")");

                if (this.m_IsUserInteractive)
                {
                    t.WriteLine("pause");
                }
            }

            return BatchFileName;
        }

        private string CreateDetailedLandAllocationQuery()
        {
            string QueryFileName = Path.Combine(this.m_OutputFolderName, "detailed_land_allocation_query.xml");

            using (StreamWriter t = new StreamWriter(QueryFileName))
            {
                t.WriteLine("<queries>");
                t.WriteLine("  <aQuery>");
                t.WriteLine("    <region name=\"USA\"/>");
                t.WriteLine("    <query title=\"detailed land allocation\">");
                t.WriteLine("      <axis1 name=\"LandLeaf\">LandLeaf[@name]</axis1>");
                t.WriteLine("      <axis2 name=\"Year\">land-allocation[@year]</axis2>");
                t.WriteLine("      <xPath buildList=\"true\" dataName=\"LandLeaf\" group=\"false\" sumAll=\"false\">/LandNode[@name='root' or @type='LandNode' (:collapse:)]//land-allocation/text()</xPath>");
                t.WriteLine("      <comments/>");
                t.WriteLine("    </query>");
                t.WriteLine("  </aQuery>");
                t.WriteLine("</queries>");
            }

            return QueryFileName;
        }

        private string CreateDetailedLandAllocationBatchFile()
        {
            string BatchFileName = Path.Combine(this.m_OutputFolderName, "detailed_land_allocation_batch.xml");
            string CSVFileName = Path.Combine(this.m_OutputFolderName, "detailed_land_allocation.csv");

            using (StreamWriter t = new StreamWriter(BatchFileName))
            {
                t.WriteLine("<ModelInterfaceBatch>");
                t.WriteLine("  <class name=\"ModelInterface.ModelGUI2.DbViewer\">");
                t.WriteLine("    <command name=\"XMLDB Batch File\">");
                t.WriteLine("      <queryFile>{0}</queryFile>", this.m_GCAMDetailedLandAllocationQueryFileName);
                t.WriteLine("      <outFile>{0}</outFile>", CSVFileName);
                t.WriteLine("      <xmldbLocation>{0}</xmldbLocation>", this.m_GCAMDatabaseFolderName);
                t.WriteLine("    </command>");
                t.WriteLine("  </class>");
                t.WriteLine("</ModelInterfaceBatch>");
            }

            return BatchFileName;
        }

        private string CreateDetailedLandAllocationCMDFile()
        {
            string AppFolderName = this.m_GCAMAppFolderName;
            string CMDFileName = Path.Combine(this.m_OutputFolderName, "detailed_land_allocation.cmd");

            using (StreamWriter t = new StreamWriter(CMDFileName))
            {
                t.WriteLine(@"SET CLASSPATH={0}\libs\jars\*;{0}\output\modelinterface\ModelInterface.jar", AppFolderName, AppFolderName);
                t.WriteLine("java ModelInterface.InterfaceMain -b \"{0}\"", this.m_GCAMDetailedLandAllocationQueryXMLBatchFileName);

                if (this.m_IsUserInteractive)
                {
                    t.WriteLine("pause");
                }
            }

            return CMDFileName;
        }

        private void ConfigureRunControl()
        {
            string ModelInputFileName = Path.Combine(this.m_GCAMAppFolderName, @"input\gcamdata\xml\modeltime.xml");

            if (!File.Exists(ModelInputFileName))
            {
                throw new ArgumentException("The model input file does not exist: " + ModelInputFileName);
            }

            XDocument doc = XDocument.Load(ModelInputFileName);
            XElement ScenarioElement = doc.Element("scenario");
            XElement ModelTimeElement = ScenarioElement.Element("modeltime");
            XElement StartYearElement = ModelTimeElement.Element("start-year");
            XElement EndYearElement = ModelTimeElement.Element("end-year");

            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.RUN_CONTROL_DATASHEET_NAME);
            DataRow dr = ds.GetData().NewRow();

            dr[Shared.RUN_CONTROL_MIN_ITERATION_COLUMN_NAME] = "1";
            dr[Shared.RUN_CONTROL_MAX_ITERATION_COLUMN_NAME] = "1";
            dr[Shared.RUN_CONTROL_MIN_TIMESTEP_COLUMN_NAME] = Convert.ToInt32(StartYearElement.Value.ToString());
            dr[Shared.RUN_CONTROL_MAX_TIMESTEP_COLUMN_NAME] = Convert.ToInt32(EndYearElement.Value.ToString());

            ds.GetData().Rows.Add(dr);
            ds.Changes.Add(new ChangeRecord(this, "Configured Run Control"));
        }

        private void RunGCAMBatchFiles()
        {
            StringDictionary e = null;

            if (this.m_IsUserInteractive)
            {
                e = new StringDictionary();
                e.Add("SSIM_USER_INTERACTIVE", "True");
            }

            string ptf = "NULL";

            if (this.m_GCAMPolicyTargetFileName != null)
            {
                ptf = this.m_GCAMPolicyTargetFileName;
            }

            this.RecordStatus(StatusType.Status, "Configuration file: " + this.m_GCAMConfigurationFileNameSource);
            this.RecordStatus(StatusType.Status, "Policy target file: " + ptf);
                             
            this.ExecuteProcess(this.m_GCAMRunModelBatchFileName, null, false, e);
            this.ExecuteProcess(this.m_GCAMDetailedLandAllocationQueryBatchFileName, null, false, e);
        }

        private void ImportDetailedLandAllocationData()
        {
            string CSVFileName = Path.Combine(this.m_OutputFolderName, "detailed_land_allocation.csv");

            if (!File.Exists(CSVFileName))
            {
                throw new DataException("Cannot find the detailed land allocation data: " + CSVFileName);
            }

            //Example CSV file:

            //detailed land allocation
            //scenario, region, LandLeaf,1990,2005,2010,2015,2020,2025,2030,2035,2040,2045,2050,2055,2060,2065,2070,2075,2080,2085,2090,2095,2100,Units,
            //"GCAM-USA_Ref,date=2019-20-9T13:48:08-07:00",USA,Corn_ArkWhtRedR_IRR_hi,2.40186,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,thous km2,
            //"GCAM-USA_Ref,date=2019-20-9T13:48:08-07:00",USA,Corn_ArkWhtRedR_IRR_lo,2.40186,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,thous km2,
            //"GCAM-USA_Ref,date=2019-20-9T13:48:08-07:00",USA,Corn_ArkWhtRedR_RFD_hi,1.33969,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,0.0,thous km2,

            DataTable dt = this.m_LandAllocationDataSheet.GetData();

            using (StreamReader s = new StreamReader(CSVFileName))
            {
                string line;
                string[] TimestepsLineSplit;

                line = s.ReadLine(); //Skip title
                TimestepsLineSplit = s.ReadLine().Split(',');

                int FirstTimestepIndex = 3;
                int LastTimestepIndex = TimestepsLineSplit.Length - 3; //The last column is the units and there is a comma at the end of each line...

                while ((line = s.ReadLine()) != null)
                {
                    string[] LineValues = line.Split(',');
                    string LandLeaf = LineValues[3];  //3 because there is a comma in the scenario name

                    string StratumName = null;
                    string StateLabelXName = null;
                    string StateLabelYName = null;
                    string StateLabelZName = null;

                    this.ParseLandLeafNames(
                        LandLeaf, 
                        ref StratumName, 
                        ref StateLabelXName, 
                        ref StateLabelYName,
                        ref StateLabelZName);

                    object StratumID = null;
                    object StateLabelXID = null;
                    object StateLabelYID = null;
                    object StateLabelZID = null;

                    if (!GetStratumId(StratumName, ref StratumID) ||
                        !GetStateLabelXID(StateLabelXName, ref StateLabelXID) ||
                        !GetStateLabelYID(StateLabelYName, ref StateLabelYID) ||
                        !GetStateLabelZID(StateLabelZName, ref StateLabelZID))
                    {
                        continue;
                    }

                    for (int i = FirstTimestepIndex; i <= LastTimestepIndex; i++)
                    {
                        object Timestep = TimestepsLineSplit[i];
                        object Amount = LineValues[i + 1]; //Shifted by one because there is a comma in the scenario name

                        DataRow dr = dt.NewRow();

                        dr[Shared.DETAILED_LAND_ALLOCATION_ITERATION_COLUMN_NAME] = 1;
                        dr[Shared.DETAILED_LAND_ALLOCATION_TIMESTEP_COLUMN_NAME] = Timestep;
                        dr[Shared.DETAILED_LAND_ALLOCATION_STRATUMID_COLUMN_NAME] = StratumID;
                        dr[Shared.DETAILED_LAND_ALLOCATION_STATE_LABEL_X_ID_COLUMN_NAME] = StateLabelXID;
                        dr[Shared.DETAILED_LAND_ALLOCATION_STATE_LABEL_Y_ID_COLUMN_NAME] = StateLabelYID;
                        dr[Shared.DETAILED_LAND_ALLOCATION_STATE_LABEL_Z_ID_COLUMN_NAME] = StateLabelZID;
                        dr[Shared.DETAILED_LAND_ALLOCATION_AMOUNT_COLUMN_NAME] = Amount;

                        dt.Rows.Add(dr);
                    }
                }
            }
        }

        private void ParseLandLeafNames(
            string landLeaf, 
            ref string stratumName, 
            ref string stateLabelXName, 
            ref string stateLabelYName, 
            ref string stateLabelZName)
        {
            //Example LandLeaf Values: 

            //Corn_ArkWhtRedR_IRR_hi
            //biomass_tree_ArkWhtRedR_RFD_lo
            //Forest_ArkWhtRedR

            //Stratum........... ArkWhtRedR
            //StateLabelX....... Corn
            //StateLabelY....... IRR
            //StateLabelZ....... hi

            //Note that there can be either 1, 3, or 4 separators...

            stateLabelYName = null; //can be null
            stateLabelZName = null; //can be null

            string[] LandLeafValues = landLeaf.Split('_');

            if (LandLeafValues.Length != 2 &&
                LandLeafValues.Length != 4 &&
                LandLeafValues.Length != 5)
            {
                throw new DataException("Cannot parse LandLeaf: " + landLeaf);
            }

            if (LandLeafValues.Length == 2)
            {
                stratumName = LandLeafValues[1];
                stateLabelXName = LandLeafValues[0];
            }
            else if (LandLeafValues.Length == 4)
            {
                stratumName = LandLeafValues[1];
                stateLabelXName = LandLeafValues[0];
                stateLabelYName = LandLeafValues[2];
                stateLabelZName = LandLeafValues[3];
            }
            else
            {
                stratumName = LandLeafValues[2];
                stateLabelXName = LandLeafValues[0] + "_" + LandLeafValues[1];
                stateLabelYName = LandLeafValues[3];
                stateLabelZName = LandLeafValues[4];
            }
        }

        private bool GetStratumId(string name, ref object id)
        {
            if (this.m_StratumDataSheet.ValidationTable.ContainsValue(name))
            {
                id = this.m_StratumDataSheet.ValidationTable.GetValue(name);
                return true;
            }
            else
            {
                this.RecordStatus(StatusType.Warning, "Import data contains missing Stratum: " + name);
                return false;
            }
        }

        private bool GetStateLabelXID(string name, ref object id)
        {
            if (this.m_StateLabelXDataSheet.ValidationTable.ContainsValue(name))
            {
                id = this.m_StateLabelXDataSheet.ValidationTable.GetValue(name);
                return true;
            }
            else
            {
                this.RecordStatus(StatusType.Warning, "Import data contains missing State Label X: " + name);
                return false;
            }
        }

        private bool GetStateLabelYID(string name, ref object id)
        {
            if (name == null)
            {
                id = DBNull.Value;
                return true;
            }
            else if (this.m_StateLabelYDataSheet.ValidationTable.ContainsValue(name))
            {
                id = this.m_StateLabelYDataSheet.ValidationTable.GetValue(name);
                return true;
            }
            else
            {
                this.RecordStatus(StatusType.Warning, "Import data contains missing State Label Y: " + name);
                return false;
            }
        }

        private bool GetStateLabelZID(string name, ref object id)
        {
            if (name == null)
            {
                id = DBNull.Value;
                return true;
            }
            else if (this.m_StateLabelZDataSheet.ValidationTable.ContainsValue(name))
            {
                id = this.m_StateLabelZDataSheet.ValidationTable.GetValue(name);
                return true;
            }
            else
            {
                this.RecordStatus(StatusType.Warning, "Import data contains missing State Label Z: " + name);
                return false;
            }
        }

        private string GetGCAMAppFolderName()
        {
            DataSheet ds = this.Library.GetDataSheet(Shared.APPLICATION_DATASHEET_NAME);
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr[Shared.APPLICATION_DATASHEET_FOLDER_COLUMN_NAME] == DBNull.Value)
            {
                throw new ArgumentException("The GCAM application directory is not specified.");
            }

            string f = Convert.ToString(dr[Shared.APPLICATION_DATASHEET_FOLDER_COLUMN_NAME]);

            if (!Directory.Exists(f))
            {
                throw new ArgumentException("The GCAM application directory does not exist: " + f);
            }

            return f;
        }

        private string GetGCAMExeFolderName()
        {
            string f = Path.Combine(this.m_GCAMAppFolderName, "exe");

            if (!Directory.Exists(f))
            {
                throw new ArgumentException("The GCAM exe directory was not found: " + f);
            }

            return f;
        }

        private string GetGCAMDatabaseFolderName()
        {
            string f = Path.Combine(this.m_GCAMAppFolderName, @"output\database_basexdb");

            if (!Directory.Exists(f))
            {
                throw new ArgumentException("The GCAM database directory was not found: " + f);
            }

            return f;
        }
    }
}
