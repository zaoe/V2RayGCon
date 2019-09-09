﻿using System.Windows.Forms;

namespace V2RayGCon.Controller.OptionComponent
{
    class TabSetting : OptionComponentController
    {
        Service.Setting setting;
        Service.Servers servers;

        ComboBox cboxLanguage = null, cboxPageSize = null;
        CheckBox chkServAutoTrack = null,
            chkPortableMode = null,
            chkSetUseV4 = null,
            chkSetEnableStat = null,
            chkSetUpdateUseProxy = null,
            chkSetCheckWhenAppStart = null;

        public TabSetting(
            ComboBox cboxLanguage,
            ComboBox cboxPageSize,
            CheckBox chkServAutoTrack,
            CheckBox chkPortableMode,
            CheckBox chkSetUseV4,
            CheckBox chkSetEnableStat,
            CheckBox chkSetUpdateUseProxy,
            CheckBox chkSetCheckWhenAppStart)
        {
            this.setting = Service.Setting.Instance;
            this.servers = Service.Servers.Instance;

            // Do not put these lines of code into InitElement.
            this.cboxLanguage = cboxLanguage;
            this.cboxPageSize = cboxPageSize;
            this.chkServAutoTrack = chkServAutoTrack;
            this.chkPortableMode = chkPortableMode;
            this.chkSetUseV4 = chkSetUseV4;
            this.chkSetEnableStat = chkSetEnableStat;
            this.chkSetCheckWhenAppStart = chkSetCheckWhenAppStart;
            this.chkSetUpdateUseProxy = chkSetUpdateUseProxy;

            InitElement();
        }

        private void InitElement()
        {
            chkSetUpdateUseProxy.Checked = setting.isUpdateUseProxy;
            chkSetCheckWhenAppStart.Checked = setting.isCheckUpdateWhenAppStart;

            chkSetEnableStat.Checked = setting.isEnableStatistics;
            chkSetUseV4.Checked = setting.isUseV4;
            chkPortableMode.Checked = setting.isPortable;
            cboxLanguage.SelectedIndex = (int)setting.culture;
            cboxPageSize.Text = setting.serverPanelPageSize.ToString();
            var tracker = setting.GetServerTrackerSetting();
            chkServAutoTrack.Checked = tracker.isTrackerOn;
        }

        #region public method
        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            var pageSize = Lib.Utils.Str2Int(cboxPageSize.Text);
            if (pageSize != setting.serverPanelPageSize)
            {
                setting.serverPanelPageSize = pageSize;
                Service.Servers.Instance.RequireFormMainUpdate();
            }

            var index = cboxLanguage.SelectedIndex;
            if (IsIndexValide(index) && ((int)setting.culture != index))
            {
                setting.culture = (Model.Data.Enum.Cultures)index;
            }

            var keepTracking = chkServAutoTrack.Checked;
            var trackerSetting = setting.GetServerTrackerSetting();
            if (trackerSetting.isTrackerOn != keepTracking)
            {
                trackerSetting.isTrackerOn = keepTracking;
                setting.SaveServerTrackerSetting(trackerSetting);
                setting.isServerTrackerOn = keepTracking;
                servers.OnAutoTrackingOptionChanged();
            }

            setting.isUpdateUseProxy = chkSetUpdateUseProxy.Checked;
            setting.isCheckUpdateWhenAppStart = chkSetCheckWhenAppStart.Checked;
            setting.isPortable = chkPortableMode.Checked;
            setting.isUseV4 = chkSetUseV4.Checked;

            // Must enable v4 mode first.
            setting.isEnableStatistics =
                setting.isUseV4 && chkSetEnableStat.Checked;

            setting.SaveUserSettingsNow();
            return true;
        }

        public override bool IsOptionsChanged()
        {
            if (setting.isUseV4 != chkSetUseV4.Checked
                || setting.isUpdateUseProxy != chkSetUpdateUseProxy.Checked
                || setting.isCheckUpdateWhenAppStart != chkSetCheckWhenAppStart.Checked
                || setting.isEnableStatistics != chkSetEnableStat.Checked
                || setting.isPortable != chkPortableMode.Checked
                || Lib.Utils.Str2Int(cboxPageSize.Text) != setting.serverPanelPageSize)
            {
                return true;
            }

            var index = cboxLanguage.SelectedIndex;
            if (IsIndexValide(index) && ((int)setting.culture != index))
            {
                return true;
            }

            var tracker = setting.GetServerTrackerSetting();
            if (tracker.isTrackerOn != chkServAutoTrack.Checked)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region private method
        bool IsIndexValide(int index)
        {
            if (index < 0 || index > 2)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
