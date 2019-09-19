// gcam: SyncroSim Base Package for the Global Change Assessment Model (GCAM).
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

namespace SyncroSim.GCAM
{
    static class Shared
    {
        public static string APPLICATION_DATAFEED_NAME = "gcam_Application";
        public static string APPLICATION_FOLDER_COLUMN_NAME = "Directory";

        public static string RUN_CONTROL_DATAFEED_NAME = "gcam_RunControl";
        public const string MIN_ITERATION_COLUMN_NAME = "MinimumIteration";
        public const string MAX_ITERATION_COLUMN_NAME = "MaximumIteration";
        public const string MIN_TIMESTEP_COLUMN_NAME = "MinimumTimestep";
        public const string MAX_TIMESTEP_COLUMN_NAME = "MaximumTimestep";

        public static string INPUT_FILE_DATAFEED_NAME = "gcam_InputFile";
        public static string INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME = "ConfigurationFile";      
        public static string INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME = "PolicyTargetFile";          
    }
}
