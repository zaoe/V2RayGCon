﻿using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.UserControls
{
    public partial class ServerUI :
        UserControl,
        Model.BaseClass.IFormMainFlyPanelComponent,
        VgcApis.Models.Interfaces.IDropableControl
    {
        Service.Servers servers;
        Service.ShareLinkMgr slinkMgr;
        VgcApis.Models.Interfaces.ICoreServCtrl coreServCtrl;

        int[] formHeight;
        Bitmap[] foldingButtonIcons;
        string keyword = null;

        public ServerUI(
            VgcApis.Models.Interfaces.ICoreServCtrl serverItem)
        {
            servers = Service.Servers.Instance;
            slinkMgr = Service.ShareLinkMgr.Instance;

            this.coreServCtrl = serverItem;
            InitializeComponent();

            this.foldingButtonIcons = new Bitmap[] {
                Properties.Resources.StepBackArrow_16x,
                Properties.Resources.StepOverArrow_16x,
            };

            this.formHeight = new int[] {
                this.Height,  // collapseLevel= 0
                this.cboxInbound.Top,
            };
        }

        private void ServerListItem_Load(object sender, EventArgs e)
        {
            SetStatusThen(string.Empty);
            RefreshUI(this, EventArgs.Empty);
            this.coreServCtrl.OnPropertyChanged += RefreshUI;
            rtboxServerTitle.BackColor = this.BackColor;
        }

        #region interface VgcApis.Models.IDropableControl
        public string GetTitle() =>
            coreServCtrl.GetCoreStates().GetTitle();

        public string GetUid() =>
            coreServCtrl.GetCoreStates().GetUid();
        #endregion

        #region private method
        private void HighLightTitleWithKeywords()
        {
            VgcApis.Libs.UI.RunInUiThread(rtboxServerTitle, () =>
            {
                var box = rtboxServerTitle;
                var title = box.Text.ToLower();

                if (string.IsNullOrEmpty(keyword)
                    || !VgcApis.Libs.Utils.PartialMatchCi(title, keyword))
                {
                    return;
                }

                int idxTitle = 0, idxKeyword = 0;
                while (idxTitle < title.Length && idxKeyword < keyword.Length)
                {
                    if (title[idxTitle].CompareTo(keyword[idxKeyword]) == 0)
                    {
                        box.SelectionStart = idxTitle;
                        box.SelectionLength = 1;
                        box.SelectionBackColor = Color.Yellow;
                        idxKeyword++;
                    }
                    idxTitle++;
                }
                box.SelectionStart = 0;
                box.SelectionLength = 0;
                box.DeselectAll();
            });
        }

        void RestartServer()
        {
            var server = this.coreServCtrl;
            servers.StopAllServersThen(
                () => server.GetCoreCtrl().RestartCoreThen());
        }

        void RefreshUI(object sender, EventArgs arg)
        {
            VgcApis.Libs.UI.RunInUiThread(rtboxServerTitle, () =>
            {
                Lib.UI.UpdateControlOnDemand(
                    cboxInbound, coreServCtrl.GetCoreStates().GetInboundType());

                Lib.UI.UpdateControlOnDemand(
                    rtboxServerTitle, coreServCtrl.GetCoreStates().GetTitle());

                Lib.UI.UpdateControlOnDemand(
                    lbStatus, coreServCtrl.GetCoreStates().GetStatus());

                UpdateServerOptionTickStat();
                UpdateInboundAddrOndemand();
                UpdateMarkLable();
                UpdateSelectedTickStat();
                UpdateOnOffLabel(coreServCtrl.GetCoreCtrl().IsCoreRunning());
                UpdateFilterMarkBox();
                UpdateBorderFoldingStat();
                UpdateToolsTip();
                UpdateLastModifiedLable();
            });
        }

        void UpdateLastModifiedLable()
        {
            var utcTicks = coreServCtrl.GetCoreStates().GetLastModifiedUtcTicks();
            var date = new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();

            Lib.UI.UpdateControlOnDemand(lbLastModify, date.ToString(I18N.MMdd));
            var tooltip = I18N.LastModified + date.ToLongDateString() + date.ToLongTimeString();
            if (toolTip1.GetToolTip(lbLastModify) != tooltip)
            {
                toolTip1.SetToolTip(lbLastModify, tooltip);
            }
        }

        private void UpdateServerOptionTickStat()
        {
            Lib.UI.UpdateControlOnDemand(
                globalImportToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsInjectGlobalImport());

            Lib.UI.UpdateControlOnDemand(
                skipCNWebsiteToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsInjectSkipCnSite());

            Lib.UI.UpdateControlOnDemand(
                autorunToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsAutoRun());

            Lib.UI.UpdateControlOnDemand(
                untrackToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsUntrack());
        }

        void UpdateInboundAddrOndemand()
        {
            if (!VgcApis.Libs.Utils.TryParseIPAddr(
                tboxInboundAddr.Text, out string ip, out int port))
            {
                return;
            }

            var addr = coreServCtrl.GetCoreStates().GetInboundAddr();
            if (tboxInboundAddr.Text != addr)
            {
                tboxInboundAddr.Text = addr;
            }
        }

        private void UpdateToolsTip()
        {
            var title = rtboxServerTitle.Text;
            if (toolTip1.GetToolTip(rtboxServerTitle) != title)
            {
                toolTip1.SetToolTip(rtboxServerTitle, title);
            }
        }

        private void UpdateMarkLable()
        {
            var text = (coreServCtrl.GetCoreStates().IsAutoRun() ? "A" : "")
                + (coreServCtrl.GetCoreStates().IsInjectSkipCnSite() ? "C" : "")
                + (coreServCtrl.GetCoreStates().IsInjectGlobalImport() ? "I" : "")
                + (coreServCtrl.GetCoreStates().IsUntrack() ? "U" : "");

            if (lbIsAutorun.Text != text)
            {
                lbIsAutorun.Text = text;
            }
        }

        void UpdateBorderFoldingStat()
        {
            var level = Lib.Utils.Clamp(
                coreServCtrl.GetCoreStates().GetFoldingState(), 0, foldingButtonIcons.Length);

            if (btnIsCollapse.BackgroundImage != foldingButtonIcons[level])
            {
                btnIsCollapse.BackgroundImage = foldingButtonIcons[level];
            }

            var newHeight = this.formHeight[level];
            if (this.Height != newHeight)
            {
                this.Height = newHeight;
            }
        }

        void UpdateFilterMarkBox()
        {
            if (cboxMark.Text == coreServCtrl.GetCoreStates().GetMark())
            {
                return;
            }

            cboxMark.Text = coreServCtrl.GetCoreStates().GetMark();
        }

        void UpdateSelectedTickStat()
        {
            if (coreServCtrl.GetCoreStates().IsSelected() == chkSelected.Checked)
            {
                return;
            }

            chkSelected.Checked = coreServCtrl.GetCoreStates().IsSelected();
            HighlightSelectedServerItem(chkSelected.Checked);
        }

        void HighlightSelectedServerItem(bool selected)
        {
            var fontStyle = new Font(rtboxServerTitle.Font, selected ? FontStyle.Bold : FontStyle.Regular);
            var colorRed = selected ? Color.OrangeRed : Color.Black;
            rtboxServerTitle.Font = fontStyle;
            lbStatus.Font = fontStyle;
            lbStatus.ForeColor = colorRed;
        }

        private void UpdateOnOffLabel(bool isServerOn)
        {
            var text = isServerOn ? "ON" : "OFF";

            if (tboxInboundAddr.ReadOnly != isServerOn)
            {
                tboxInboundAddr.ReadOnly = isServerOn;
            }

            if (lbRunning.Text != text)
            {
                lbRunning.Text = text;
                lbRunning.ForeColor = isServerOn ? Color.DarkOrange : Color.Green;
            }
        }
        #endregion

        #region properties
        public bool isSelected
        {
            get
            {
                return coreServCtrl.GetCoreStates().IsSelected();
            }
            private set { }
        }
        #endregion

        #region public method
        public void SetKeywords(string keywords)
        {
            this.keyword = keywords?.Replace(@" ", "")?.ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                return;
            }

            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                // control may be desposed, the sun may explode while this function is running
                try
                {
                    HighLightTitleWithKeywords();
                }
                catch { }
            });
        }

        public string GetConfig() => coreServCtrl.GetConfiger().GetConfig();

        public void SetStatusThen(string status, Action next = null)
        {
            if (lbStatus.IsDisposed)
            {
                next?.Invoke();
                return;
            }

            try
            {
                VgcApis.Libs.UI.RunInUiThread(lbStatus, () =>
                {
                    Lib.UI.UpdateControlOnDemand(lbStatus, status);
                });
            }
            catch { }
            next?.Invoke();
        }

        public void SetSelected(bool selected)
        {
            coreServCtrl.GetCoreStates().SetIsSelected(selected);
        }

        public double GetIndex() => coreServCtrl.GetCoreStates().GetIndex();

        public void SetIndex(double index)
        {
            coreServCtrl.GetCoreStates().SetIndex(index);
        }

        public void Cleanup()
        {
            this.coreServCtrl.OnPropertyChanged -= RefreshUI;
        }
        #endregion

        #region UI event
        private void ServerListItem_MouseDown(object sender, MouseEventArgs e)
        {
            // this effect is ugly and useless
            // Cursor.Current = Lib.UI.CreateCursorIconFromUserControl(this);
            DoDragDrop((ServerUI)sender, DragDropEffects.Move);
        }

        private void btnAction_Click(object sender, System.EventArgs e)
        {
            Button btnSender = sender as Button;
            Point pos = new Point(btnSender.Left, btnSender.Top + btnSender.Height);
            ctxMenuStripMore.Show(this, pos);
            //menu.Show(this, pos);
        }

        private void cboxInbound_SelectedIndexChanged(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetInboundType(cboxInbound.SelectedIndex);
        }

        private void chkSelected_CheckedChanged(object sender, EventArgs e)
        {
            var selected = chkSelected.Checked;
            if (selected == coreServCtrl.GetCoreStates().IsSelected())
            {
                return;
            }
            coreServCtrl.GetCoreStates().SetIsSelected(selected);
            HighlightSelectedServerItem(chkSelected.Checked);
        }

        private void tboxInboundAddr_TextChanged(object sender, EventArgs e)
        {
            if (VgcApis.Libs.UI.TryParseAddressFromTextBox(
                tboxInboundAddr, out string ip, out int port))
            {
                coreServCtrl.GetCoreStates().SetInboundAddr(ip, port);
            }
        }

        private void lbSummary_Click(object sender, EventArgs e)
        {
            chkSelected.Checked = !chkSelected.Checked;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RestartServer();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var config = coreServCtrl.GetConfiger().GetConfig();
            new Views.WinForms.FormConfiger(config);
        }

        private void vmessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var vmessLink = slinkMgr.EncodeConfigToShareLink(
                GetConfig(),
                VgcApis.Models.Datas.Enum.LinkTypes.vmess);

            Lib.Utils.CopyToClipboardAndPrompt(vmessLink);
        }

        private void v2cfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var content = slinkMgr.EncodeConfigToShareLink(
                GetConfig(),
                VgcApis.Models.Datas.Enum.LinkTypes.v2cfg);

            Lib.Utils.CopyToClipboardAndPrompt(content);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Lib.UI.Confirm(I18N.ConfirmDeleteControl))
            {
                return;
            }
            Cleanup();
            servers.DeleteServerByConfig(GetConfig());
        }

        private void logOfThisServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetLogger().ShowFormLog();
        }

        private void cboxMark_TextChanged(object sender, EventArgs e)
        {
            this.coreServCtrl.GetCoreStates().SetMark(cboxMark.Text);
        }

        private void cboxMark_DropDown(object sender, EventArgs e)
        {
            var servers = Service.Servers.Instance;
            cboxMark.Items.Clear();
            foreach (var item in servers.GetMarkList())
            {
                cboxMark.Items.Add(item);
            }
            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxMark);
        }

        private void Label1_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void LbAddTimestamp_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void lbStatus_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void label4_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void lbRunning_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void btnMultiboxing_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().RestartCoreThen();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().StopCoreThen();
        }

        private void btnIsCollapse_Click(object sender, EventArgs e)
        {
            var level = (coreServCtrl.GetCoreStates().GetFoldingState() + 1) % 2;
            coreServCtrl.GetCoreStates().SetFoldingState(level);
        }

        private void lbIsAutorun_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void rtboxServerTitle_Click(object sender, EventArgs e)
        {
            chkSelected.Checked = !chkSelected.Checked;
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestartServer();
        }

        private void multiboxingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().RestartCoreThen();
        }

        private void stopToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().StopCoreThen();
        }

        private void untrackToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsUntrack();

        private void autorunToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsAutoRun();


        private void globalImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().ToggleIsInjectImport();
        }

        private void skipCNWebsiteToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsInjectSkipCnSite();

        private void runSpeedTestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            VgcApis.Libs.Utils.RunInBackground(() => coreServCtrl.GetCoreCtrl().RunSpeedTest());
        }

        private void moveToTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetIndex(0);
            servers.RequireFormMainReload();
        }

        private void moveToBottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetIndex(double.MaxValue);
            servers.RequireFormMainReload();
        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var finalConfig = coreServCtrl.GetConfiger().GetFinalConfig();
            new WinForms.FormConfiger(finalConfig.ToString(Formatting.Indented));
        }

        private void vToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var vee = slinkMgr.EncodeConfigToShareLink(
                GetConfig(),
                VgcApis.Models.Datas.Enum.LinkTypes.v);
            Lib.Utils.CopyToClipboardAndPrompt(vee);
        }


        #endregion


    }
}
