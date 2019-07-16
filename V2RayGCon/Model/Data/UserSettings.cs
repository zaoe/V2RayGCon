﻿using System.Collections.Generic;

namespace V2RayGCon.Model.Data
{
    class UserSettings
    {
        #region public properties

        // FormOption->Defaults->Mode
        public bool CustomDefImportSsShareLink { get; set; }
        public int CustomDefImportMode { get; set; } // Model.Data.Enum.ProxyTypes
        public string CustomDefImportIp { get; set; }
        public int CustomDefImportPort { get; set; }

        // FormOption->Defaults->Speedtest
        public string CustomSpeedtestUrl { get; set; }
        public bool IsUseCustomSpeedtestSettings { get; set; }
        public int CustomSpeedtestCycles { get; set; }
        public int CustomSpeedtestExpectedSize { get; set; }
        public int CustomSpeedtestTimeout { get; set; }

        // FormDownloadCore
        public bool isDownloadWin32V2RayCore { get; set; } = true;
        public List<string> V2RayCoreDownloadVersionList = null;

        public int ServerPanelPageSize { get; set; }
        public bool isEnableStat { get; set; } = false;
        public bool isUseV4Format { get; set; }
        public bool CfgShowToolPanel { get; set; }
        public bool isPortable { get; set; }
        public bool isCheckUpdateWhenAppStart { get; set; }
        public bool isUpdateUseProxy { get; set; }

        public string ImportUrls { get; set; }
        public string DecodeCache { get; set; }
        public string SubscribeUrls { get; set; }

        public string PluginInfoItems { get; set; }
        public string PluginsSetting { get; set; }

        public string Culture { get; set; }
        public string CoreInfoList { get; set; }
        public string PacServerSettings { get; set; }
        public string SysProxySetting { get; set; }
        public string ServerTracker { get; set; }
        public string WinFormPosList { get; set; }
        #endregion

        public UserSettings()
        {
            // FormOption -> Defaults -> Mode
            CustomDefImportSsShareLink = true;
            CustomDefImportMode = (int)Enum.ProxyTypes.HTTP;
            CustomDefImportIp = VgcApis.Models.Consts.Webs.LoopBackIP;
            CustomDefImportPort = VgcApis.Models.Consts.Webs.DefaultProxyPort;

            // FormOption -> Defaults -> Speedtest
            CustomSpeedtestUrl = VgcApis.Models.Consts.Webs.GoogleDotCom;
            IsUseCustomSpeedtestSettings = false;
            CustomSpeedtestCycles = 3;
            CustomSpeedtestExpectedSize = 0;
            CustomSpeedtestTimeout = VgcApis.Models.Consts.Intervals.SpeedTestTimeout;

            ServerPanelPageSize = 7;

            isCheckUpdateWhenAppStart = false;

            isUpdateUseProxy = false;
            isUseV4Format = true;
            CfgShowToolPanel = true;
            isPortable = true;

            ImportUrls = string.Empty;
            DecodeCache = string.Empty;
            SubscribeUrls = string.Empty;

            PluginInfoItems = string.Empty;
            PluginsSetting = string.Empty;

            Culture = string.Empty;
            CoreInfoList = string.Empty;
            PacServerSettings = string.Empty;
            SysProxySetting = string.Empty;
            ServerTracker = string.Empty;
            WinFormPosList = string.Empty;
        }
    }
}
