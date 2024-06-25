namespace Microsoft.Azure.SpaceFx.HostServices.Link;
public static class Models {
    public class APP_CONFIG : Core.APP_CONFIG {
        [Flags]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum PluginPermissions {
            NONE = 0,
            LOG_MESSAGE_RECEIVED = 1 << 0,
            TELEMETRY_LOG_MESSAGE_RECEIVED = 1 << 1,
            TELEMETRY_METRIC_MESSAGE_RECEIVED = 1 << 2,
            PRE_WRITE_TO_LOG_FILE = 1 << 1,
            POST_WRITE_TO_LOG_FILE = 1 << 2,
            ALL = LOG_MESSAGE_RECEIVED | TELEMETRY_LOG_MESSAGE_RECEIVED | TELEMETRY_METRIC_MESSAGE_RECEIVED | PRE_WRITE_TO_LOG_FILE | POST_WRITE_TO_LOG_FILE
        }
        public class PLUG_IN : Core.Models.PLUG_IN {
            [JsonConverter(typeof(JsonStringEnumConverter))]

            public PluginPermissions CALCULATED_PLUGIN_PERMISSIONS {
                get {
                    PluginPermissions result;
                    System.Enum.TryParse(PLUGIN_PERMISSIONS, out result);
                    return result;
                }
            }

            public PLUG_IN() {
                PLUGIN_PERMISSIONS = "";
                PROCESSING_ORDER = 100;
            }
        }

        public int FILEMOVER_POLLING_MS { get; set; }
        public string LEAVE_SOURCE_FILE_PROPERTY_VALUE { get; set; }
        public string ALL_XFER_DIR { get; set; }

        public APP_CONFIG() : base() {
            FILEMOVER_POLLING_MS = int.Parse(Core.GetConfigSetting("filemoverpollingms").Result);
            LEAVE_SOURCE_FILE_PROPERTY_VALUE = Core.GetConfigSetting("leavesourcefilepropertyvalue").Result;
            ALL_XFER_DIR = System.Path.Combine(Core.GetConfigSetting("spacefx_cache").Result, Core.GetConfigSetting("allxferdirectory").Result);
        }
    }
}