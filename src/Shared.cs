// gcam: SyncroSim Base Package for the Global Change Assessment Model (GCAM).
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

namespace SyncroSim.GCAM
{
    static class Shared
    {
        public static string APPLICATION_DATAFEED_NAME = "gcam_Application";
        public static string APPLICATION_FOLDER_COLUMN_NAME = "Directory";

        public static string RUN_CONTROL_DATAFEED_NAME = "gcam_RunControl";
        public const string RUN_CONTROL_MIN_ITERATION_COLUMN_NAME = "MinimumIteration";
        public const string RUN_CONTROL_MAX_ITERATION_COLUMN_NAME = "MaximumIteration";
        public const string RUN_CONTROL_MIN_TIMESTEP_COLUMN_NAME = "MinimumTimestep";
        public const string RUN_CONTROL_MAX_TIMESTEP_COLUMN_NAME = "MaximumTimestep";

        public static string INPUT_FILE_DATAFEED_NAME = "gcam_InputFile";
        public static string INPUT_FILE_CONFIGURATION_FILE_COLUMN_NAME = "ConfigurationFile";      
        public static string INPUT_FILE_POLICY_TARGET_FILE_COLUMN_NAME = "PolicyTargetFile"; 
        
        public static string DETAILED_LAND_ALLOCATION_DATAFEED_NAME = "gcam_DetailedLandAllocation";
        public static string DETAILED_LAND_ALLOCATION_ITERATION_COLUMN_NAME = "Iteration";
        public static string DETAILED_LAND_ALLOCATION_TIMESTEP_COLUMN_NAME = "Timestep";
        public static string DETAILED_LAND_ALLOCATION_STRATUMID_COLUMN_NAME = "StratumID";
        public static string DETAILED_LAND_ALLOCATION_STATE_LABEL_X_ID_COLUMN_NAME = "StateLabelXID";
        public static string DETAILED_LAND_ALLOCATION_STATE_LABEL_Y_ID_COLUMN_NAME = "StateLabelYID";
        public static string DETAILED_LAND_ALLOCATION_STATE_LABEL_Z_ID_COLUMN_NAME = "StateLabelZID";
        public static string DETAILED_LAND_ALLOCATION_AMOUNT_COLUMN_NAME = "Amount";
    }
}
