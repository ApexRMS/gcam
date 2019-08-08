// gcam: SyncroSim Base Package for the Global Change Assessment Model (GCAM).
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

using System;
using System.IO;
using System.Data;
using SyncroSim.Core;
using System.Collections.Specialized;

namespace SyncroSim.GCAM
{
    class Runtime : Transformer
    {
        public override void Transform()
        {
            StringDictionary env = new StringDictionary();
            env.Add("SSIM_USER_INTERACTIVE", "True");

            string ExeFolderName = this.GetGCAMExeFolderName();
            string ConfigurationFileName = this.GetXMLConfigFileName();
            string RunModelBatchFileName = this.GetRunModelBatchFilename();

            this.CreateRunGCAMBatchFile(ExeFolderName, ConfigurationFileName, RunModelBatchFileName);
            this.ExecuteProcess(RunModelBatchFileName, null, false, env);
        }

        private string GetGCAMAppFolderName()
        {
            DataSheet ds = this.Library.GetDataSheet("gcam_AppDirectory");
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr["Path"] == DBNull.Value)
            {
                throw new ArgumentException("The GCAM application directory is not specified.");
            }

            string f = Convert.ToString(dr["Path"]);

            if (!Directory.Exists(f))
            {
                throw new ArgumentException("The GCAM application directory does not exist: " + f);
            }

            return f;
        }

        private string GetGCAMExeFolderName()
        {
            string f = this.GetGCAMAppFolderName();
            string e = Path.Combine(f, "exe");

            if (!Directory.Exists(e))
            {
                throw new ArgumentException("The GCAM exe directory was not found: " + e);
            }

            return e;
        }

        private string GetXMLConfigFileName()
        {
            DataSheet ds = this.ResultScenario.GetDataSheet("gcam_InputFile");
            DataRow dr = ds.GetDataRow();

            if (dr == null || dr["XMLConfigFile"] == DBNull.Value)
            {
                throw new ArgumentException("The XML configuration file is not specified.");
            }

            string f = Convert.ToString(dr["XMLConfigFile"]);

            if (!File.Exists(f))
            {
                throw new ArgumentException("The XML configuration file does not exist: " + f);
            }

            return f;
        }

        private string GetRunModelBatchFilename()
        {
            DataSheet ds = this.ResultScenario.GetDataSheet("gcam_InputFile");
            string p = this.Library.GetFolderName(LibraryFolderType.Input, ds, true);

            return Path.Combine(p, "run-gcam.bat");
        }

        private void CreateRunGCAMBatchFile(string exeFolder, string configFileName, string batchFileName)
        {
            using (StreamWriter t = new StreamWriter(batchFileName))
            {
                t.WriteLine("@echo off");
                t.WriteLine("cd \"{0}\"", exeFolder);

                t.WriteLine(@"SET CLASSPATH=..\libs\jars\*;XMLDBDriver.jar");
                t.WriteLine("IF NOT DEFINED JAVA_HOME FOR /F \"delims=\" %%O IN ('java XMLDBDriver --print-java-home') DO @SET JAVA_HOME=%%O");
                t.WriteLine("IF DEFINED JAVA_HOME (");

                t.WriteLine(@"SET PATH=%JAVA_HOME%\bin;%JAVA_HOME%\bin\server");
                t.WriteLine("Objects-Main.exe -C \"{0}\"", configFileName);
                t.WriteLine(")");

                t.WriteLine("pause");
            }
        }
    }
}
