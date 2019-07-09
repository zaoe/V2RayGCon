using System;
using System.Net;
using System.Windows.Forms;

namespace V2RayGCon.Controller.OptionComponent
{
    class TabDefaults : OptionComponentController
    {
        Service.Setting setting;

        ComboBox cboxDefImportMode = null;
        CheckBox chkSetSpeedtestIsUse = null;

        TextBox tboxDefImportAddr = null,
            tboxSetSpeedtestUrl = null,
            tboxSetSpeedtestCycles = null,
            tboxSetSpeedtestExpectedSize = null;

        public TabDefaults(
            ComboBox cboxDefImportMode,
            TextBox tboxDefImportAddr,

            CheckBox chkSetSpeedtestIsUse,
            TextBox tboxSetSpeedtestUrl,
            TextBox tboxSetSpeedtestCycles,
            TextBox tboxSetSpeedtestExpectedSize)
        {
            this.setting = Service.Setting.Instance;

            // Do not put these lines of code into InitElement.
            this.cboxDefImportMode = cboxDefImportMode;
            this.tboxDefImportAddr = tboxDefImportAddr;

            this.chkSetSpeedtestIsUse = chkSetSpeedtestIsUse;
            this.tboxSetSpeedtestUrl = tboxSetSpeedtestUrl;
            this.tboxSetSpeedtestCycles = tboxSetSpeedtestCycles;
            this.tboxSetSpeedtestExpectedSize = tboxSetSpeedtestExpectedSize;

            InitElement();
        }

        private void InitElement()
        {
            // mode
            cboxDefImportMode.SelectedIndex = setting.CustomDefImportMode;
            tboxDefImportAddr.TextChanged += OnTboxImportAddrTextChanged;
            tboxDefImportAddr.Text = string.Format(
                @"{0}:{1}",
                setting.CustomDefImportIp,
                setting.CustomDefImportPort);

            // speedtest
            chkSetSpeedtestIsUse.Checked = setting.isUseCustomSpeedtestSettings;
            tboxSetSpeedtestCycles.Text = setting.CustomSpeedtestCycles.ToString();
            tboxSetSpeedtestUrl.Text = setting.CustomSpeedtestUrl;
            tboxSetSpeedtestExpectedSize.Text = setting.CustomSpeedtestExpectedSizeInKib.ToString();
        }

        #region public method
        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            // mode
            if (VgcApis.Libs.Utils.TryParseIPAddr(tboxDefImportAddr.Text, out string ip, out int port))
            {
                setting.CustomDefImportIp = ip;
                setting.CustomDefImportPort = port;
            }
            setting.CustomDefImportMode = cboxDefImportMode.SelectedIndex;

            // speedtest
            setting.isUseCustomSpeedtestSettings = chkSetSpeedtestIsUse.Checked;
            setting.CustomSpeedtestUrl = tboxSetSpeedtestUrl.Text;
            setting.CustomSpeedtestCycles = VgcApis.Libs.Utils.Str2Int(tboxSetSpeedtestCycles.Text);
            setting.CustomSpeedtestExpectedSizeInKib = VgcApis.Libs.Utils.Str2Int(tboxSetSpeedtestExpectedSize.Text);

            setting.SaveUserSettingsNow();
            return true;
        }

        public override bool IsOptionsChanged()
        {
            var success = VgcApis.Libs.Utils.TryParseIPAddr(tboxDefImportAddr.Text, out string ip, out int port);
            if (!success
                || setting.CustomDefImportIp != ip
                || setting.CustomDefImportPort != port
                || setting.CustomDefImportMode != cboxDefImportMode.SelectedIndex

                || setting.isUseCustomSpeedtestSettings != chkSetSpeedtestIsUse.Checked
                || setting.CustomSpeedtestUrl != tboxSetSpeedtestUrl.Text
                || setting.CustomSpeedtestExpectedSizeInKib != VgcApis.Libs.Utils.Str2Int(tboxSetSpeedtestExpectedSize.Text)
                || setting.CustomSpeedtestCycles != VgcApis.Libs.Utils.Str2Int(tboxSetSpeedtestCycles.Text))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region private method
        void OnTboxImportAddrTextChanged(object sender, EventArgs e) =>
            VgcApis.Libs.UI.TryParseAddressFromTextBox(
                tboxDefImportAddr, out string ip, out int port);
        #endregion
    }
}
