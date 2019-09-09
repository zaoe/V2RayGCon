﻿using System;
using System.IO;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.FormMainComponent
{
    class MenuItemsBasic : FormMainComponentController
    {
        Service.Servers servers;
        Service.ShareLinkMgr slinkMgr;
        Service.Updater updater;
        Service.PluginsServer pluginServ;

        ToolStripMenuItem pluginToolStrip;
        Form formMain;

        public MenuItemsBasic(
            Form formMain,
            ToolStripMenuItem pluginToolStrip,

            ToolStripMenuItem miSimVmessServer,
            ToolStripMenuItem miImportLinkFromClipboard,
            ToolStripMenuItem miExportAllServer,
            ToolStripMenuItem miImportFromFile,
            ToolStripMenuItem miAbout,
            ToolStripMenuItem miHelp,
            ToolStripMenuItem miFormConfigEditor,
            ToolStripMenuItem miFormQRCode,
            ToolStripMenuItem miFormLog,
            ToolStripMenuItem miFormOptions,
            ToolStripMenuItem miDownloadV2rayCore,
            ToolStripMenuItem miRemoveV2rayCore,
            ToolStripMenuItem miCheckVgcUpdate)
        {
            servers = Service.Servers.Instance;
            slinkMgr = Service.ShareLinkMgr.Instance;
            updater = Service.Updater.Instance;
            pluginServ = Service.PluginsServer.Instance;

            this.formMain = formMain;

            InitMenuPlugin(pluginToolStrip);

            InitMenuFile(miSimVmessServer, miImportLinkFromClipboard, miExportAllServer, miImportFromFile);
            InitMenuWindows(miFormConfigEditor, miFormQRCode, miFormLog, miFormOptions);
            InitMenuAbout(miAbout, miHelp, miDownloadV2rayCore, miRemoveV2rayCore, miCheckVgcUpdate);
        }

        #region public method
        public void ImportServersFromTextFile()
        {
            string v2cfgLinks = VgcApis.Libs.UI.ReadFileContentFromDialog(
                VgcApis.Models.Consts.Files.TxtExt);

            if (v2cfgLinks == null)
            {
                return;
            }

            slinkMgr.ImportLinkWithV2cfgLinks(v2cfgLinks);
        }

        public void ExportAllServersToTextFile()
        {
            if (this.servers.IsEmpty())
            {
                MessageBox.Show(I18N.ServerListIsEmpty);
                return;
            }

            var serverList = servers.GetAllServersOrderByIndex();
            string s = string.Empty;

            foreach (var server in serverList)
            {
                var vlink = Lib.Utils.AddLinkPrefix(
                    Lib.Utils.Base64Encode(server.GetConfiger().GetConfig()),
                    VgcApis.Models.Datas.Enum.LinkTypes.v2cfg);

                s += vlink + System.Environment.NewLine + System.Environment.NewLine;
            }

            VgcApis.Libs.UI.SaveToFile(
                VgcApis.Models.Consts.Files.TxtExt,
                s);
        }

        public override bool RefreshUI() { return false; }
        public override void Cleanup()
        {
            pluginServ.OnRequireMenuUpdate -= OnRequireMenuUpdateHandler;
        }
        #endregion

        #region private method
        void UpdatePluginMenu()
        {
            var plugins = pluginServ.GetAllEnabledPlugins();
            pluginToolStrip.DropDownItems.Clear();

            if (plugins.Count <= 0)
            {
                pluginToolStrip.Visible = false;
                return;
            }

            foreach (var plugin in plugins)
            {
                var mi = new ToolStripMenuItem(
                    plugin.Name, plugin.Icon, (s, a) => plugin.Show());
                pluginToolStrip.DropDownItems.Add(mi);
                mi.ToolTipText = plugin.Description;
            }
            pluginToolStrip.Visible = true;
        }

        void OnRequireMenuUpdateHandler(object sender, EventArgs evs)
        {
            VgcApis.Libs.UI.RunInUiThread(formMain, UpdatePluginMenu);
        }

        void InitMenuPlugin(ToolStripMenuItem pluginToolStrip)
        {
            this.pluginToolStrip = pluginToolStrip;
            OnRequireMenuUpdateHandler(this, EventArgs.Empty);
            pluginServ.OnRequireMenuUpdate += OnRequireMenuUpdateHandler;
        }

        private void InitMenuAbout(
            ToolStripMenuItem aboutVGC,
            ToolStripMenuItem help,
            ToolStripMenuItem downloadV2rayCore,
            ToolStripMenuItem removeV2rayCore,
            ToolStripMenuItem miCheckVgcUpdate)
        {
            // menu about
            downloadV2rayCore.Click += (s, a) => Views.WinForms.FormDownloadCore.GetForm();

            removeV2rayCore.Click += (s, a) => RemoveV2RayCore();

            aboutVGC.Click += (s, a) =>
                Lib.UI.VisitUrl(I18N.VistPorjectPage, Properties.Resources.ProjectLink);

            help.Click += (s, a) =>
                Lib.UI.VisitUrl(I18N.VistWikiPage, Properties.Resources.WikiLink);

            miCheckVgcUpdate.Click += (s, a) => updater.CheckForUpdate(true);
        }

        private void InitMenuFile(ToolStripMenuItem simVmessServer, ToolStripMenuItem importLinkFromClipboard, ToolStripMenuItem exportAllServer, ToolStripMenuItem importFromFile)
        {
            // menu file
            simVmessServer.Click +=
                (s, a) => Views.WinForms.FormSimAddVmessClient.GetForm();

            importLinkFromClipboard.Click += (s, a) =>
            {
                string text = Lib.Utils.GetClipboardText();
                slinkMgr.ImportLinkWithV2cfgLinks(text);
            };

            exportAllServer.Click += (s, a) => ExportAllServersToTextFile();

            importFromFile.Click += (s, a) => ImportServersFromTextFile();
        }

        private static void InitMenuWindows(ToolStripMenuItem miFormConfigEditor, ToolStripMenuItem miFormQRCode, ToolStripMenuItem miFormLog, ToolStripMenuItem miFormOptions)
        {
            // menu window
            miFormConfigEditor.Click += (s, a) => new Views.WinForms.FormConfiger();

            miFormQRCode.Click += (s, a) => Views.WinForms.FormQRCode.ShowForm();

            miFormLog.Click += (s, a) => Views.WinForms.FormLog.ShowForm();

            miFormOptions.Click += (s, a) => Views.WinForms.FormOption.ShowForm();
        }

        private void RemoveV2RayCore()
        {
            if (!Lib.UI.Confirm(I18N.ConfirmRemoveV2RayCore))
            {
                return;
            }

            if (!Directory.Exists(Lib.Utils.GetSysAppDataFolder()))
            {
                MessageBox.Show(I18N.Done);
                return;
            }

            servers.StopAllServersThen(() =>
            {
                try
                {
                    Lib.Utils.DeleteAppDataFolder();
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show(I18N.FileInUse);
                    return;
                }
                MessageBox.Show(I18N.Done);
            });
        }
        #endregion
    }
}
