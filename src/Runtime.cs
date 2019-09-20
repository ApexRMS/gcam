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
        public override void Transform()
        {
            StringDictionary env = new StringDictionary();
            env.Add("SSIM_USER_INTERACTIVE", "True");
                
            string RunModelBatchFileName = this.CreateRunGCAMBatchFile();
            //this.ExecuteProcess(RunModelBatchFileName, null, false, env);

            this.ConfigureRunControl();

            string DetailedLandAllocationQuery = this.CreateDetailedLandAllocationQuery();
            string DetailedLandAllocationBatchFileName = this.CreateDetailedLandAllocationBatchFile(DetailedLandAllocationQuery);
            string DetailedLandAllocationCMDFileName = this.CreateDetailedLandAllocationCMDFile(DetailedLandAllocationBatchFileName);

            //this.ExecuteProcess(DetailedLandAllocationCMDFileName, null, false, env);
            this.ImportGCAMOutput();
        }

        private string GetAppFolderName()
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

        private string GetExeFolderName()
        {
            string f = this.GetAppFolderName();
            string e = Path.Combine(f, "exe");

            if (!Directory.Exists(e))
            {
                throw new ArgumentException("The GCAM exe directory was not found: " + e);
            }

            return e;
        }

        private string GetUserInputFileName(string columnName)
        {
            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.INPUT_FILE_DATAFEED_NAME);
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

        private void ConfigureRunControl()
        {
            string AppFolderName = this.GetAppFolderName();
            string ModelInputFileName = Path.Combine(AppFolderName, @"input\gcamdata\xml\modeltime.xml");
            XDocument doc = XDocument.Load(ModelInputFileName);
            XElement ScenarioElement = doc.Element("scenario");
            XElement ModelTimeElement = ScenarioElement.Element("modeltime");
            XElement StartYearElement = ModelTimeElement.Element("start-year");
            XElement EndYearElement = ModelTimeElement.Element("end-year");

            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.RUN_CONTROL_DATAFEED_NAME);
            DataRow dr = ds.GetData().NewRow();

            dr[Shared.RUN_CONTROL_MIN_ITERATION_COLUMN_NAME] = "0";
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
            string GCAMDatabaseFolderName = this.GetGCAMOutputFolderName(Shared.GCAM_DATABASE_FOLDER_NAME);

            Directory.CreateDirectory(GCAMDatabaseFolderName);

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
                        else if (line.Contains("xmldb-location"))
                        {
                            line = string.Format(CultureInfo.InvariantCulture,
                                "		<Value write-output=\"1\" append-scenario-name=\"0\" name=\"xmldb - location\">{0}</Value>",
                                GCAMDatabaseFolderName);
                        }
                      
                        t.WriteLine(line);
                    }
                }
            }

            return CopyConfigFileName;
        }

        private string CreateRunGCAMBatchFile()
        {
            string ExeFolderName = this.GetExeFolderName();
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
            string DBFolderName = this.GetGCAMOutputFolderName(Shared.GCAM_DATABASE_FOLDER_NAME);

            using (StreamWriter t = new StreamWriter(BatchFileName))
            {
                t.WriteLine("<ModelInterfaceBatch>");
                t.WriteLine("  <class name=\"ModelInterface.ModelGUI2.DbViewer\">");
                t.WriteLine("    <command name=\"XMLDB Batch File\">");
                t.WriteLine("      <queryFile>{0}</queryFile>", queryFileName);
                t.WriteLine("      <outFile>{0}</outFile>", CSVFileName);
                t.WriteLine("      <xmldbLocation>{0}</xmldbLocation>", DBFolderName);
                t.WriteLine("    </command>");
                t.WriteLine("  </class>");
                t.WriteLine("</ModelInterfaceBatch>");
            }

            return BatchFileName;
        }

        private string CreateDetailedLandAllocationCMDFile(string batchFileName)
        {
            string AppFolderName = this.GetAppFolderName();
            string CMDFileName = this.GetGCAMOutputFileName("detailed_land_allocation.cmd");

            using (StreamWriter t = new StreamWriter(CMDFileName))
            {
                t.WriteLine(@"SET CLASSPATH={0}\libs\jars\*;{0}\output\modelinterface\ModelInterface.jar", AppFolderName, AppFolderName);
                t.WriteLine("java ModelInterface.InterfaceMain -b \"{0}\"", batchFileName);
                t.WriteLine("pause");
            }

            return CMDFileName;
        }

        private void ImportGCAMOutput()
        {
            string GCAMOutputFolder = this.GetGCAMOutputFolderName();
            string GCAMDatabaseFolderName = this.GetGCAMOutputFolderName(Shared.GCAM_DATABASE_FOLDER_NAME);

        }
    }
}
