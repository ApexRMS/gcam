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

            this.ConfigureRunControl();  
                    
            string RunModelBatchFileName = this.CreateRunGCAMBatchFile();
            this.ExecuteProcess(RunModelBatchFileName, null, false, env);
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

        private string GetInputFileName(string columnName)
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

        private string GetInputFileCopyName(string baseFileName)
        {
            DataSheet ds = this.ResultScenario.GetDataSheet(Shared.INPUT_FILE_DATAFEED_NAME);
            string p = this.Library.GetFolderName(LibraryFolderType.Input, ds, true);

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

            dr[Shared.MIN_ITERATION_COLUMN_NAME] = "0";
            dr[Shared.MAX_ITERATION_COLUMN_NAME] = "1";
            dr[Shared.MIN_TIMESTEP_COLUMN_NAME] = Convert.ToInt32(StartYearElement.Value.ToString());
            dr[Shared.MAX_TIMESTEP_COLUMN_NAME] = Convert.ToInt32(EndYearElement.Value.ToString());

            ds.GetData().Rows.Add(dr);
            ds.Changes.Add(new ChangeRecord(this, "Configured Run Control"));
        }

        private string CreateConfigurationFile()
        {
            string InputConfigFileName = this.GetInputFileName(Shared.INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME);
            string CopyConfigFileName = this.GetInputFileCopyName(Path.GetFileName(InputConfigFileName));
            string PolicyTargetFileName = this.GetInputFileName(Shared.INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME);

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
            string ExeFolderName = this.GetExeFolderName();
            string BatchFileName = this.GetInputFileCopyName("run-gcam.bat");
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
    }
}
