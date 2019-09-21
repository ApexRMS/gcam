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

        public override void Transform()
        {
            StringDictionary env = new StringDictionary();
            env.Add("SSIM_USER_INTERACTIVE", "True");

            this.InitDataSheets();
            this.ConfigureRunControl();

            string RunModelBatchFileName = this.CreateRunGCAMBatchFile();
            string DetailedLandAllocationQuery = this.CreateDetailedLandAllocationQuery();
            string DetailedLandAllocationBatchFileName = this.CreateDetailedLandAllocationBatchFile(DetailedLandAllocationQuery);
            string DetailedLandAllocationCMDFileName = this.CreateDetailedLandAllocationCMDFile(DetailedLandAllocationBatchFileName);

            this.ExecuteProcess(RunModelBatchFileName, null, false, env);
            this.ExecuteProcess(DetailedLandAllocationCMDFileName, null, false, env);
            this.ImportDetailedLandAllocationData();
        }

        private void InitDataSheets()
        {
            this.m_StratumDataSheet = this.Project.GetDataSheet(Shared.STRATUM_DATASHEET_NAME);
            this.m_StateLabelXDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_X_DATASHEET_NAME);
            this.m_StateLabelYDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_Y_DATASHEET_NAME);
            this.m_StateLabelZDataSheet = this.Project.GetDataSheet(Shared.STATE_LABEL_Z_DATASHEET_NAME);
        }

        private string GetGCAMFolderName()
        {
            DataSheet ds = this.Library.GetDataSheet(Shared.APPLICATION_DATAFEED_NAME);
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr[Shared.APPLICATION_FOLDER_COLUMN_NAME] == DBNull.Value)
            {
                throw new ArgumentException("The GCAM application directory is not specified.");
            }

            string f = Convert.ToString(dr[Shared.APPLICATION_FOLDER_COLUMN_NAME]);

            if (!Directory.Exists(f))
            {
                throw new ArgumentException("The GCAM application directory does not exist: " + f);
            }

            return f;
        }

        private string GetGCAMExeFolderName()
        {
            string f = this.GetGCAMFolderName();
            string e = Path.Combine(f, "exe");

            if (!Directory.Exists(e))
            {
                throw new ArgumentException("The GCAM exe directory was not found: " + e);
            }

            return e;
        }

        private string GetUserInputFileName(string columnName)
        {
            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.INPUT_FILE_DATASHEET_NAME);
            string ColumnTitle = ds.Columns[columnName].DisplayName;
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr[columnName] == DBNull.Value)
            {
                string m = string.Format(CultureInfo.InvariantCulture, "The '{0}' file is not specified.", ColumnTitle);
                throw new ArgumentException(m);
            }

            string InputFileName = Convert.ToString(dr[columnName]);

            if (!File.Exists(InputFileName))
            {
                string m = string.Format(CultureInfo.InvariantCulture, "The '{0}' file does not exist.", InputFileName);
                throw new ArgumentException(m);
            }

            return InputFileName;
        }

        private string GetGCAMOutputFolderName()
        {
            string p = this.Library.GetFolderName(LibraryFolderType.Output, this.ResultScenario, true);

            p = Path.Combine(p, "GCAM");

            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
            }

            return p;
        }

        private string GetGCAMOutputFolderName(string baseFolderName)
        {
            string p = this.GetGCAMOutputFolderName();
            p = Path.Combine(p, baseFolderName);

            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
            }

            return p;
        }

        private string GetGCAMOutputFileName(string baseFileName)
        {
            string p = this.GetGCAMOutputFolderName();
            return Path.Combine(p, baseFileName);
        }

        private string GetGCAMDatabaseFolder()
        {
            string p = this.GetGCAMFolderName();
            return Path.Combine(p, @"output\database_basexdb");
        }

        private void ConfigureRunControl()
        {
            string AppFolderName = this.GetGCAMFolderName();
            string ModelInputFileName = Path.Combine(AppFolderName, @"input\gcamdata\xml\modeltime.xml");
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

        private string CreateConfigurationFile()
        {
            string InputConfigFileName = this.GetUserInputFileName(Shared.INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME);
            string CopyConfigFileName = this.GetGCAMOutputFileName(Path.GetFileName(InputConfigFileName));
            string PolicyTargetFileName = this.GetUserInputFileName(Shared.INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME);

            using (StreamReader s = new StreamReader(InputConfigFileName))
            {
                string line;

                using (StreamWriter t = new StreamWriter(CopyConfigFileName))
                {
                    while ((line = s.ReadLine()) != null)
                    {
                        if (line.Contains("policy-target-file"))
                        {
                            line = string.Format(CultureInfo.InvariantCulture,
                                "		<Value name=\"policy-target-file\">{0}</Value>",
                                PolicyTargetFileName);
                        }
                    
                        t.WriteLine(line);
                    }
                }
            }

            return CopyConfigFileName;
        }

        private string CreateRunGCAMBatchFile()
        {
            string ExeFolderName = this.GetGCAMExeFolderName();
            string BatchFileName = this.GetGCAMOutputFileName("run-gcam.bat");
            string ConfigFileName = this.CreateConfigurationFile();

            using (StreamWriter t = new StreamWriter(BatchFileName))
            {
                t.WriteLine("@echo off");
                t.WriteLine("cd \"{0}\"", ExeFolderName);

                t.WriteLine(@"SET CLASSPATH=..\libs\jars\*;XMLDBDriver.jar");
                t.WriteLine("IF NOT DEFINED JAVA_HOME FOR /F \"delims=\" %%O IN ('java XMLDBDriver --print-java-home') DO @SET JAVA_HOME=%%O");
                t.WriteLine("IF DEFINED JAVA_HOME (");

                t.WriteLine(@"SET PATH=%JAVA_HOME%\bin;%JAVA_HOME%\bin\server");
                t.WriteLine("Objects-Main.exe -C \"{0}\"", ConfigFileName);
                t.WriteLine(")");

                t.WriteLine("pause");
            }

            return BatchFileName;
        }

        private string CreateDetailedLandAllocationQuery()
        {
            string QueryFileName = this.GetGCAMOutputFileName("detailed_land_allocation_query.xml");

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

        private string CreateDetailedLandAllocationBatchFile(string queryFileName)
        {
            string BatchFileName = this.GetGCAMOutputFileName("detailed_land_allocation_batch.xml");
            string CSVFileName = this.GetGCAMOutputFileName("detailed_land_allocation.csv");
            string GCAMDatabaseFolderName = this.GetGCAMDatabaseFolder();

            using (StreamWriter t = new StreamWriter(BatchFileName))
            {
                t.WriteLine("<ModelInterfaceBatch>");
                t.WriteLine("  <class name=\"ModelInterface.ModelGUI2.DbViewer\">");
                t.WriteLine("    <command name=\"XMLDB Batch File\">");
                t.WriteLine("      <queryFile>{0}</queryFile>", queryFileName);
                t.WriteLine("      <outFile>{0}</outFile>", CSVFileName);
                t.WriteLine("      <xmldbLocation>{0}</xmldbLocation>", GCAMDatabaseFolderName);
                t.WriteLine("    </command>");
                t.WriteLine("  </class>");
                t.WriteLine("</ModelInterfaceBatch>");
            }

            return BatchFileName;
        }

        private string CreateDetailedLandAllocationCMDFile(string batchFileName)
        {
            string AppFolderName = this.GetGCAMFolderName();
            string CMDFileName = this.GetGCAMOutputFileName("detailed_land_allocation.cmd");

            using (StreamWriter t = new StreamWriter(CMDFileName))
            {
                t.WriteLine(@"SET CLASSPATH={0}\libs\jars\*;{0}\output\modelinterface\ModelInterface.jar", AppFolderName, AppFolderName);
                t.WriteLine("java ModelInterface.InterfaceMain -b \"{0}\"", batchFileName);
                t.WriteLine("pause");
            }

            return CMDFileName;
        }

        private void ImportDetailedLandAllocationData()
        {
            string GCAMOutputFolder = this.GetGCAMOutputFolderName();
            string CSVFileName = Path.Combine(GCAMOutputFolder, "detailed_land_allocation.csv");

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

            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.DETAILED_LAND_ALLOCATION_DATASHEET_NAME);
            DataTable dt = ds.GetData();

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
    }
}
