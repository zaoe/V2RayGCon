using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    public class Setting :
        Model.BaseClass.SingletonService<Setting>,
        VgcApis.Models.IServices.ISettingsService
    {
        Model.Data.UserSettings userSettings;

        // Singleton need this private ctor.
        Setting()
        {
            userSettings = LoadUserSettings();
        }

        #region Properties
        public string AllPluginsSetting
        {
            get => userSettings.PluginsSetting;
            set
            {
                userSettings.PluginsSetting = value;
                LazySaveUserSettings();
            }
        }

        public VgcApis.Models.Datas.Enum.ShutdownReasons ShutdownReason { get; set; } =
            VgcApis.Models.Datas.Enum.ShutdownReasons.CloseByUser;

        public bool isDownloadWin32V2RayCore
        {
            get => userSettings.isDownloadWin32V2RayCore;
            set
            {
                userSettings.isDownloadWin32V2RayCore = value;
                LazySaveUserSettings();
            }
        }

        public string decodeCache
        {
            get
            {
                return userSettings.DecodeCache;
            }
            set
            {
                userSettings.DecodeCache = value;
                LazySaveUserSettings();
            }
        }

        public bool isEnableStatistics
        {
            get => userSettings.isEnableStat;
            set
            {
                userSettings.isEnableStat = value;
                LazySaveUserSettings();
            }
        }

        public bool isUseV4
        {
            get => userSettings.isUseV4Format;
            set
            {
                userSettings.isUseV4Format = value;
                LazySaveUserSettings();
            }
        }

        public int CustomDefImportMode
        {
            get => VgcApis.Libs.Utils.Clamp(userSettings.CustomDefImportMode, 0, 4);
            set
            {
                userSettings.CustomDefImportMode =
                    VgcApis.Libs.Utils.Clamp(value, 0, 4);
            }
        }

        public string CustomDefImportIp
        {
            get => userSettings.CustomDefImportIp;
            set => userSettings.CustomDefImportIp = value;
        }

        public int CustomDefImportPort
        {
            get => userSettings.CustomDefImportPort;
            set => userSettings.CustomDefImportPort = value;
        }


        public string CustomSpeedtestUrl
        {
            get => userSettings.CustomSpeedtestUrl;
            set
            {
                userSettings.CustomSpeedtestUrl = value;
                LazySaveUserSettings();
            }
        }

        public int CustomSpeedtestExpectedSizeInKib
        {
            get => userSettings.CustomSpeedtestExpectedSize;
            set
            {
                userSettings.CustomSpeedtestExpectedSize = value;
                LazySaveUserSettings();
            }
        }

        public int CustomSpeedtestCycles
        {
            get => userSettings.CustomSpeedtestCycles;
            set
            {
                userSettings.CustomSpeedtestCycles = value;
                LazySaveUserSettings();
            }
        }

        public bool isUseCustomSpeedtestSettings
        {
            get => userSettings.IsUseCustomSpeedtestSettings;
            set
            {
                userSettings.IsUseCustomSpeedtestSettings = value;
                LazySaveUserSettings();
            }
        }

        public bool isUpdateUseProxy
        {
            get => userSettings.isUpdateUseProxy;
            set
            {
                userSettings.isUpdateUseProxy = value;
                LazySaveUserSettings();
            }
        }

        public bool isCheckUpdateWhenAppStart
        {
            get => userSettings.isCheckUpdateWhenAppStart;
            set
            {
                userSettings.isCheckUpdateWhenAppStart = value;
                LazySaveUserSettings();
            }
        }

        public bool isPortable
        {
            get
            {
                return userSettings.isPortable;
            }
            set
            {
                userSettings.isPortable = value;
                LazySaveUserSettings();
            }
        }

        public bool isServerTrackerOn = false;

        public int serverPanelPageSize
        {
            get
            {
                var size = userSettings.ServerPanelPageSize;
                return Lib.Utils.Clamp(size, 1, 101);
            }
            set
            {
                userSettings.ServerPanelPageSize = Lib.Utils.Clamp(value, 1, 101);
                LazySaveUserSettings();
            }
        }

        public CultureInfo orgCulture = null;

        VgcApis.Libs.Sys.QueueLogger qLogger = new VgcApis.Libs.Sys.QueueLogger();
        public long GetLogTimestamp() => qLogger.GetTimestamp();
        public string GetLogContent() => qLogger.GetLogAsString(true);
        public void SendLog(string log) => qLogger.Log(log);

        public Model.Data.Enum.Cultures culture
        {
            get
            {
                var cultures = Model.Data.Table.Cultures;
                var c = userSettings.Culture;

                if (!cultures.ContainsValue(c))
                {
                    return Model.Data.Enum.Cultures.auto;
                }

                return cultures.Where(s => s.Value == c).First().Key;
            }

            set
            {
                var cultures = Model.Data.Table.Cultures;
                var c = Model.Data.Enum.Cultures.auto;
                if (cultures.ContainsKey(value))
                {
                    c = value;
                }
                userSettings.Culture = Model.Data.Table.Cultures[c];
                LazySaveUserSettings();
            }
        }

        public bool isShowConfigerToolsPanel
        {
            get
            {
                return userSettings.CfgShowToolPanel == true;
            }
            set
            {
                userSettings.CfgShowToolPanel = value;
                LazySaveUserSettings();
            }
        }

        public const int maxLogLines = 1000;

        #endregion

        #region public methods
        public void SaveV2RayCoreVersionList(List<string> versions)
        {
            // clone version list
            userSettings.V2RayCoreDownloadVersionList =
                new List<string>(versions);
            LazySaveUserSettings();
        }

        public ReadOnlyCollection<string> GetV2RayCoreVersionList()
        {
            var result = userSettings.V2RayCoreDownloadVersionList ??
                new List<string> { "v3.48", "v3.47", "v3.46" };
            return result.AsReadOnly();
        }

        // ISettingService thing
        bool isShutdown = false;
        public bool IsShutdown() => isShutdown;
        public bool SetIsShutdown(bool isShutdown) => this.isShutdown = isShutdown;

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public string GetPluginsSetting(string pluginName)
        {
            var pluginsSetting = DeserializePluginsSetting();

            if (pluginsSetting != null
                && pluginsSetting.ContainsKey(pluginName))
            {
                return pluginsSetting[pluginName];
            }
            return null;
        }

        public void SavePluginsSetting(string pluginName, string value)
        {
            if (string.IsNullOrEmpty(pluginName))
            {
                return;
            }

            var pluginsSetting = DeserializePluginsSetting();
            pluginsSetting[pluginName] = value;

            try
            {
                userSettings.PluginsSetting =
                    JsonConvert.SerializeObject(pluginsSetting);
            }
            catch { }
            LazySaveUserSettings();
        }

        VgcApis.Libs.Tasks.Bar saveUserSettingsBar = new VgcApis.Libs.Tasks.Bar();
        public void SaveUserSettingsNow()
        {
            if (!saveUserSettingsBar.Install())
            {
                return;
            }

            var serializedUserSettings = JsonConvert.SerializeObject(userSettings);
            if (userSettings.isPortable)
            {
                DebugSendLog("Try save settings to file.");
                SaveUserSettingsToFile(serializedUserSettings);
            }
            else
            {
                DebugSendLog("Try save settings to properties");
                SetUserSettingFileIsPortableToFalse();
                SaveUserSettingsToProperties(serializedUserSettings);
            }
            saveUserSettingsBar.Remove();
        }

        /*
         * string something;
         * if(something == null){} // Boom!
         */
        Lib.Sys.CancelableTimeout lazyGCTimer = null;
        public void LazyGC()
        {
            // Create on demand.
            if (lazyGCTimer == null)
            {
                lazyGCTimer = new Lib.Sys.CancelableTimeout(
                    () => GC.Collect(),
                    VgcApis.Models.Consts.Intervals.LazyGcDelay);
            }

            lazyGCTimer.Start();
        }

        public void SaveServerTrackerSetting(Model.Data.ServerTracker serverTrackerSetting)
        {
            userSettings.ServerTracker =
                JsonConvert.SerializeObject(serverTrackerSetting);
            LazySaveUserSettings();
        }

        public Model.Data.ServerTracker GetServerTrackerSetting()
        {
            var empty = new Model.Data.ServerTracker();
            Model.Data.ServerTracker result;
            try
            {
                result = JsonConvert
                    .DeserializeObject<Model.Data.ServerTracker>(
                        userSettings.ServerTracker);
                if (result != null && result.serverList == null)
                {
                    result.serverList = new List<string>();
                }
            }
            catch
            {
                return empty;
            }
            return result ?? empty;
        }

        public List<VgcApis.Models.Datas.CoreInfo> LoadCoreInfoList()
        {
            List<VgcApis.Models.Datas.CoreInfo> coreInfos = null;
            try
            {
                coreInfos = JsonConvert
                    .DeserializeObject<List<VgcApis.Models.Datas.CoreInfo>>(
                        userSettings.CoreInfoList);
            }
            catch { }

            if (coreInfos == null)
            {
                return new List<VgcApis.Models.Datas.CoreInfo>();
            }

            // make sure every config of server can be parsed correctly
            var result = coreInfos.Where(c =>
             {
                 try
                 {
                     return JObject.Parse(c.config) != null;
                 }
                 catch { }
                 return false;
             }).ToList();

            return result;
        }

        public void SaveFormRect(Form form)
        {
            var key = form.GetType().Name;
            var list = GetWinFormRectList();
            list[key] = new Rectangle(form.Left, form.Top, form.Width, form.Height);
            userSettings.WinFormPosList = JsonConvert.SerializeObject(list);
            LazySaveUserSettings();
        }

        public void RestoreFormRect(Form form)
        {
            var key = form.GetType().Name;
            var list = GetWinFormRectList();

            if (!list.ContainsKey(key))
            {
                return;
            }

            var rect = list[key];
            var screen = Screen.PrimaryScreen.WorkingArea;

            form.Width = Math.Max(rect.Width, 300);
            form.Height = Math.Max(rect.Height, 200);
            form.Left = Lib.Utils.Clamp(rect.Left, 0, screen.Right - form.Width);
            form.Top = Lib.Utils.Clamp(rect.Top, 0, screen.Bottom - form.Height);
        }

        public List<Model.Data.ImportItem> GetGlobalImportItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.ImportItem>>(
                        userSettings.ImportUrls);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.ImportItem>();
        }

        public void SaveGlobalImportItems(string options)
        {
            userSettings.ImportUrls = options;
            LazySaveUserSettings();
        }

        public List<Model.Data.PluginInfoItem> GetPluginInfoItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.PluginInfoItem>>(
                        userSettings.PluginInfoItems);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.PluginInfoItem>();
        }

        /// <summary>
        /// Feel free to pass null.
        /// </summary>
        /// <param name="itemList"></param>
        public void SavePluginInfoItems(
            List<Model.Data.PluginInfoItem> itemList)
        {
            string json = JsonConvert.SerializeObject(
                itemList ?? new List<Model.Data.PluginInfoItem>());

            userSettings.PluginInfoItems = json;
            LazySaveUserSettings();
        }

        public List<Model.Data.SubscriptionItem> GetSubscriptionItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.SubscriptionItem>>(
                        userSettings.SubscribeUrls);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.SubscriptionItem>();
        }

        public void SaveSubscriptionItems(string options)
        {
            userSettings.SubscribeUrls = options;
            LazySaveUserSettings();
        }

        public void SaveServerList(List<VgcApis.Models.Datas.CoreInfo> coreInfoList)
        {
            string json = JsonConvert.SerializeObject(
                coreInfoList ?? new List<VgcApis.Models.Datas.CoreInfo>());

            userSettings.CoreInfoList = json;
            LazySaveUserSettings();
        }
        #endregion

        #region private method
        Dictionary<string, string> DeserializePluginsSetting()
        {
            var empty = new Dictionary<string, string>();
            Dictionary<string, string> pluginsSetting = null;

            try
            {
                pluginsSetting = JsonConvert
                    .DeserializeObject<Dictionary<string, string>>(
                        userSettings.PluginsSetting);
            }
            catch { }
            if (pluginsSetting == null)
            {
                pluginsSetting = empty;
            }

            return pluginsSetting;
        }

        void SetUserSettingFileIsPortableToFalse()
        {
            DebugSendLog("Read user setting file");

            var mainUsFilename = Constants.Strings.MainUserSettingsFilename;
            var bakUsFilename = Constants.Strings.BackupUserSettingsFilename;
            if (!File.Exists(mainUsFilename) && !File.Exists(bakUsFilename))
            {
                DebugSendLog("setting file not exists");
                return;
            }

            DebugSendLog("set portable to false");
            userSettings.isPortable = false;
            try
            {
                var serializedUserSettings = JsonConvert.SerializeObject(userSettings);
                File.WriteAllText(mainUsFilename, serializedUserSettings);
                File.WriteAllText(bakUsFilename, serializedUserSettings);
                DebugSendLog("set portable option done");
                return;
            }
            catch { }

            if (ShutdownReason == VgcApis.Models.Datas.Enum.ShutdownReasons.CloseByUser)
            {
                // this is important do not use task
                var msg = string.Format(I18N.UnsetPortableModeFail, mainUsFilename);
                MessageBox.Show(msg);
            }
        }

        void SaveUserSettingsToProperties(string content)
        {
            try
            {
                Properties.Settings.Default.UserSettings = content;
                Properties.Settings.Default.Save();
            }
            catch
            {
                DebugSendLog("Save user settings to Properties fail!");
            }
        }

        void SaveUserSettingsToFile(string content)
        {
            try
            {
                File.WriteAllText(Constants.Strings.MainUserSettingsFilename, content);
                File.WriteAllText(Constants.Strings.BackupUserSettingsFilename, content);
                return;
            }
            catch { }

            if (ShutdownReason == VgcApis.Models.Datas.Enum.ShutdownReasons.CloseByUser)
            {
                if (isShutdown)
                {
                    // 兄弟只能帮你到这了
                    VgcApis.Libs.Sys.NotepadHelper.ShowMessage(content, Properties.Resources.PortableUserSettingsFilename);
                }

                // this is important do not use task!
                MessageBox.Show(I18N.SaveUserSettingsToFileFail);
            }
        }

        Model.Data.UserSettings LoadUserSettingsFromPorperties()
        {
            try
            {
                var serializedUserSettings = Properties.Settings.Default.UserSettings;
                var us = JsonConvert.DeserializeObject<Model.Data.UserSettings>(serializedUserSettings);
                if (us != null)
                {
                    DebugSendLog("Read user settings from Properties.Usersettings");
                    return us;
                }
            }
            catch { }

            return null;
        }

        Model.Data.UserSettings LoadUserSettingsFromFile()
        {
            // try to load userSettings.json
            var result = VgcApis.Libs.Utils.LoadAndParseJsonFile<Model.Data.UserSettings>(
                Constants.Strings.MainUserSettingsFilename);

            // try to load userSettings.bak
            if (result == null)
            {
                result = VgcApis.Libs.Utils.LoadAndParseJsonFile<Model.Data.UserSettings>(
                    Constants.Strings.BackupUserSettingsFilename);
            }

            if (result != null && result.isPortable)
            {
                return result;
            }
            return null;
        }

        Model.Data.UserSettings LoadUserSettings()
        {
            var mainUsFile = Constants.Strings.MainUserSettingsFilename;
            var bakUsFile = Constants.Strings.BackupUserSettingsFilename;

            var result = LoadUserSettingsFromFile() ?? LoadUserSettingsFromPorperties();
            if (result == null
                && (File.Exists(mainUsFile) || File.Exists(bakUsFile))
                && !Lib.UI.Confirm(I18N.ConfirmLoadDefaultUserSettings))
            {
                ShutdownReason = VgcApis.Models.Datas.Enum.ShutdownReasons.Abort;
            }

            return result ?? new Model.Data.UserSettings();
        }

        Lib.Sys.CancelableTimeout lazySaveUserSettingsTimer = null;
        void LazySaveUserSettings()
        {
            if (lazySaveUserSettingsTimer == null)
            {
                lazySaveUserSettingsTimer = new Lib.Sys.CancelableTimeout(
                    SaveUserSettingsNow,
                    VgcApis.Models.Consts.Intervals.LazySaveUserSettingsDelay);
            }

            lazySaveUserSettingsTimer.Start();
        }

        Dictionary<string, Rectangle> winFormRectListCache = null;
        Dictionary<string, Rectangle> GetWinFormRectList()
        {
            if (winFormRectListCache != null)
            {
                return winFormRectListCache;
            }

            try
            {
                winFormRectListCache = JsonConvert
                    .DeserializeObject<Dictionary<string, Rectangle>>(
                        userSettings.WinFormPosList);
            }
            catch { }

            if (winFormRectListCache == null)
            {
                winFormRectListCache = new Dictionary<string, Rectangle>();
            }

            return winFormRectListCache;
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            lazyGCTimer?.Release();
            lazySaveUserSettingsTimer?.Release();
            if (ShutdownReason == VgcApis.Models.Datas.Enum.ShutdownReasons.CloseByUser)
            {
                SaveUserSettingsNow();
            }
            qLogger.Dispose();
        }
        #endregion

        #region debug
        void DebugSendLog(string content)
        {
#if DEBUG
            SendLog($"(Debug) {content}");
#endif
        }
        #endregion
    }
}
