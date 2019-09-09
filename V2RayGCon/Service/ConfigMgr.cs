﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    sealed public class ConfigMgr :
         Model.BaseClass.SingletonService<ConfigMgr>,
        VgcApis.Models.IServices.IConfigMgrService
    {
        Setting setting;
        Cache cache;
        Servers servers;

        ConfigMgr() { }

        #region public methods
        public long RunCustomSpeedTest(string rawConfig, string testUrl, int testTimeout) =>
            RunSpeedTestWorker(rawConfig, "Custom speed-test", testUrl, testTimeout, false, false, false, null);

        public long RunSpeedTest(string rawConfig)
        {
            var url = GetDefaultSpeedtestUrl();
            return RunSpeedTestWorker(rawConfig, "Default speed-test", "", GetDefaultTimeout(), false, false, false, null);
        }

        public long RunDefaultSpeedTest(
            string rawConfig,
            string title,
            EventHandler<VgcApis.Models.Datas.StrEvent> logDeliever)
        {
            var url = GetDefaultSpeedtestUrl();
            return RunSpeedTestWorker(rawConfig, title, url, GetDefaultTimeout(), true, true, false, logDeliever);
        }

        public string InjectImportTpls(
            string config,
            bool isIncludeSpeedTest,
            bool isIncludeActivate)
        {
            JObject import = Lib.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                isIncludeSpeedTest,
                isIncludeActivate,
                false);

            Lib.Utils.MergeJson(ref import, JObject.Parse(config));
            return import.ToString();
        }

        public JObject DecodeConfig(
            string rawConfig,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var coreConfig = rawConfig;
            JObject decodedConfig = null;

            try
            {
                string injectedConfig = coreConfig;
                if (isInjectActivateTpl || isInjectSpeedTestTpl)
                {
                    injectedConfig = InjectImportTpls(
                        rawConfig,
                        isInjectSpeedTestTpl,
                        isInjectActivateTpl);
                }

                decodedConfig = ParseImport(injectedConfig);
                cache.core[coreConfig] = decodedConfig.ToString(Formatting.None);
            }
            catch { }

            if (decodedConfig == null)
            {
                setting.SendLog(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        decodedConfig = JObject.Parse(cache.core[coreConfig]);
                    }
                    catch (KeyNotFoundException) { }
                    setting.SendLog(I18N.UsingDecodeCache);
                }
            }

            return decodedConfig;
        }

        public bool ModifyInboundByCustomSetting(
            ref JObject config,
            int inbType,
            string ip,
            int port)
        {
            switch (inbType)
            {
                case (int)Model.Data.Enum.ProxyTypes.HTTP:
                case (int)Model.Data.Enum.ProxyTypes.SOCKS:
                    break;

                case (int)Model.Data.Enum.ProxyTypes.Config:
                default:
                    return true;
            }

            var protocol = Lib.Utils.InboundTypeNumberToName(inbType);
            var part = protocol + "In";
            try
            {
                JObject o = CreateInboundSetting(
                    inbType, ip, port, protocol, part);

                ReplaceInboundSetting(ref config, o);
#if DEBUG
                var debug = config.ToString(Formatting.Indented);
#endif
                return true;
            }
            catch
            {
                setting.SendLog(I18N.CoreCantSetLocalAddr);
            }
            return false;
        }

        /// <summary>
        /// ref means config will change after the function is executed.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public void InjectSkipCnSiteSettingsIntoConfig(
            ref JObject config,
            bool useV4)
        {
            var c = JObject.Parse(@"{}");

            var dict = new Dictionary<string, string> {
                { "dns","dnsCFnGoogle" },
                { "routing",GetRoutingTplName(config, useV4) },
            };

            foreach (var item in dict)
            {
                var tpl = Lib.Utils.CreateJObject(item.Key);
                var value = cache.tpl.LoadExample(item.Value);
                tpl[item.Key] = value;

                if (!Lib.Utils.Contains(config, tpl))
                {
                    c[item.Key] = value;
                }
            }

            // put dns/routing settings in front of user settings
            Lib.Utils.CombineConfigWithRoutingInFront(ref config, c);

            // put outbounds after user settings
            var hasOutbounds = Lib.Utils.GetKey(config, "outbounds") != null;
            var hasOutDtr = Lib.Utils.GetKey(config, "outboundDetour") != null;

            var outboundTag = "outboundDetour";
            if (!hasOutDtr && (hasOutbounds || useV4))
            {
                outboundTag = "outbounds";
            }

            var o = Lib.Utils.CreateJObject(
                outboundTag,
                cache.tpl.LoadExample("outDtrFreedom"));

            if (!Lib.Utils.Contains(config, o))
            {
                Lib.Utils.CombineConfigWithRoutingInFront(ref o, config);
                config = o;
            }
        }

        /*
         * exceptions  
         * test<FormatException> base64 decode fail
         * test<System.Net.WebException> url not exist
         * test<Newtonsoft.Json.JsonReaderException> json decode fail
         */
        public JObject ParseImport(string configString)
        {
            var maxDepth = VgcApis.Models.Consts.Import.ParseImportDepth;

            var result = Lib.Utils.ParseImportRecursively(
                GetHtmlContentFromCache,
                JObject.Parse(configString),
                maxDepth);

            try
            {
                Lib.Utils.RemoveKeyFromJObject(result, "v2raygcon.import");
            }
            catch (KeyNotFoundException)
            {
                // do nothing;
            }

            return result;
        }

        /// <summary>
        /// update running servers list
        /// </summary>
        /// <param name="includeCurServer"></param>
        public Model.Data.ServerTracker GenCurTrackerSetting(
            IEnumerable<Controller.CoreServerCtrl> servers,
            string curServerConfig,
            bool isStart)
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            var tracked = trackerSetting.serverList;

            var running = servers
                .Where(s => s.GetCoreCtrl().IsCoreRunning()
                    && !s.GetCoreStates().IsUntrack())
                .Select(s => s.GetConfiger().GetConfig())
                .ToList();

            tracked.RemoveAll(c => !running.Any(r => r == c));  // remove stopped
            running.RemoveAll(r => tracked.Any(t => t == r));
            tracked.AddRange(running);
            tracked.Remove(curServerConfig);

            if (isStart)
            {
                trackerSetting.curServer = curServerConfig;
            }
            else
            {
                trackerSetting.curServer = null;
            }

            trackerSetting.serverList = tracked;
            return trackerSetting;
        }

        public List<Controller.CoreServerCtrl> GenServersBootList(
            IEnumerable<Controller.CoreServerCtrl> serverList)
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            if (!trackerSetting.isTrackerOn)
            {
                return serverList.Where(s => s.GetCoreStates().IsAutoRun()).ToList();
            }

            setting.isServerTrackerOn = true;
            var trackList = trackerSetting.serverList;

            var bootList = serverList
                .Where(s => s.GetCoreStates().IsAutoRun()
                || trackList.Contains(s.GetConfiger().GetConfig()))
                .ToList();

            if (string.IsNullOrEmpty(trackerSetting.curServer))
            {
                return bootList;
            }

            bootList.RemoveAll(s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            var lastServer = serverList.FirstOrDefault(
                    s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            if (lastServer != null && !lastServer.GetCoreStates().IsUntrack())
            {
                bootList.Insert(0, lastServer);
            }
            return bootList;
        }

        public JObject GenV4ServersPackage(
            List<VgcApis.Models.Interfaces.ICoreServCtrl> servList,
            string packageName)
        {
            var package = cache.tpl.LoadPackage("pkgV4Tpl");
            var outbounds = package["outbounds"] as JArray;
            var description = new List<string>();

            for (var i = 0; i < servList.Count; i++)
            {
                var s = servList[i];
                var parts = Lib.Utils.ExtractOutboundsFromConfig(
                    s.GetConfiger().GetConfig());
                var c = 0;
                foreach (JObject p in parts)
                {
                    p["tag"] = $"agentout{i}s{c++}";
                    outbounds.Add(p);
                }
                var name = s.GetCoreStates().GetName();
                if (c == 0)
                {
                    setting.SendLog(I18N.PackageFail + ": " + name);
                }
                else
                {
                    description.Add($"{i}.[{name}]");
                    setting.SendLog(I18N.PackageSuccess + ": " + name);
                }
            }

            package["v2raygcon"]["alias"] = string.IsNullOrEmpty(packageName) ? "PackageV4" : packageName;
            package["v2raygcon"]["description"] =
                $"[Total: {description.Count()}] " +
                string.Join(" ", description);

            try
            {
                var finalConfig = GetGlobalImportConfigForPacking();
                Lib.Utils.CombineConfigWithRoutingInTheEnd(ref finalConfig, package);
                return finalConfig;
            }
            catch
            {
                setting.SendLog(I18N.InjectPackagingImportsFail);
                return package;
            }
        }

        public void Run(
            Setting setting,
            Cache cache,
            Servers servers)
        {
            this.setting = setting;
            this.cache = cache;
            this.servers = servers;
        }

        #endregion

        #region private methods
        int GetDefaultTimeout()
        {
            var customTimeout = setting.CustomSpeedtestTimeout;
            if (customTimeout > 0)
            {
                return customTimeout;
            }
            return VgcApis.Models.Consts.Intervals.SpeedTestTimeout;
        }

        string GetDefaultSpeedtestUrl() =>
          setting.isUseCustomSpeedtestSettings ?
          setting.CustomSpeedtestUrl :
          VgcApis.Models.Consts.Webs.GoogleDotCom;

        JObject GetGlobalImportConfigForPacking()
        {
            var imports = Lib.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                false, false, true);
            return ParseImport(imports.ToString());
        }

        long RunSpeedTestWorker(
            string rawConfig,
            string title,
            string testUrl,
            int testTimeout,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl,
            EventHandler<VgcApis.Models.Datas.StrEvent> logDeliever)
        {
            var port = VgcApis.Libs.Utils.GetFreeTcpPort();
            var speedTestConfig = CreateSpeedTestConfig(
                rawConfig, port, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);

            if (string.IsNullOrEmpty(speedTestConfig))
            {
                logDeliever?.Invoke(this, new VgcApis.Models.Datas.StrEvent(I18N.DecodeImportFail));
                return long.MaxValue;
            }

            var speedTester = new Lib.V2Ray.Core(setting)
            {
                title = title
            };

            if (logDeliever != null)
            {
                speedTester.OnLog += logDeliever;
            }

            speedTester.WaitForToken();
            try
            {
                speedTester.RestartCore(speedTestConfig);
            }
            finally
            {
                speedTester.ReleaseToken();
            }

            var expectedSizeInKib = 0;
            if (setting.isUseCustomSpeedtestSettings)
            {
                expectedSizeInKib = setting.CustomSpeedtestExpectedSizeInKib;
            }
            long testResult = Lib.Utils.VisitWebPageSpeedTest(testUrl, port, expectedSizeInKib, testTimeout);

            speedTester.WaitForTokenHurry();
            try
            {
                speedTester.StopCore();
            }
            finally
            {
                speedTester.ReleaseToken();
            }

            if (logDeliever != null)
            {
                speedTester.OnLog -= logDeliever;
            }
            return testResult;
        }

        List<string> GetHtmlContentFromCache(IEnumerable<string> urls)
        {
            if (urls == null || urls.Count() <= 0)
            {
                return new List<string>();
            }
            return Lib.Utils.ExecuteInParallel(urls, (url) => cache.html[url]);
        }

        JObject CreateInboundSetting(
           int inboundType,
           string ip,
           int port,
           string protocol,
           string part)
        {
            var o = JObject.Parse(@"{}");
            o["tag"] = "agentin";
            o["protocol"] = protocol;
            o["listen"] = ip;
            o["port"] = port;
            o["settings"] = cache.tpl.LoadTemplate(part);

            if (inboundType == (int)Model.Data.Enum.ProxyTypes.SOCKS)
            {
                o["settings"]["ip"] = ip;
            }

            return o;
        }

        string CreateSpeedTestConfig(
            string rawConfig,
            int port,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var empty = string.Empty;
            if (port <= 0)
            {
                return empty;
            }

            var config = DecodeConfig(
                rawConfig, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);

            if (config == null)
            {
                return empty;
            }

            // inject log config
            var nodeLog = Lib.Utils.GetKey(config, "log");
            if (nodeLog != null && nodeLog is JObject)
            {
                nodeLog["loglevel"] = "warning";
            }
            else
            {
                config["log"] = JToken.Parse("{'loglevel': 'warning'}");
            }

            if (!ModifyInboundByCustomSetting(
                ref config,
                (int)Model.Data.Enum.ProxyTypes.HTTP,
                VgcApis.Models.Consts.Webs.LoopBackIP,
                port))
            {
                return empty;
            }

            // debug
            var configString = config.ToString(Formatting.None);

            return configString;
        }

        string GetRoutingTplName(JObject config, bool useV4)
        {
            var routingRules = Lib.Utils.GetKey(config, "routing.rules");
            var routingSettingsRules = Lib.Utils.GetKey(config, "routing.settings.rules");
            var hasRoutingV4 = routingRules == null ? false : (routingRules is JArray);
            var hasRoutingV3 = routingSettingsRules == null ? false : (routingSettingsRules is JArray);

            var isUseRoutingV4 = !hasRoutingV3 && (useV4 || hasRoutingV4);
            return isUseRoutingV4 ? "routeCnipV4" : "routeCNIP";
        }

        void ReplaceInboundSetting(ref JObject config, JObject o)
        {
            // Bug. Stream setting will mess things up.
            // Lib.Utils.MergeJson(ref config, o);

            var hasInbound = Lib.Utils.GetKey(config, "inbound") != null;
            var hasInbounds = Lib.Utils.GetKey(config, "inbounds.0") != null;
            var isUseV4 = !hasInbound && (hasInbounds || setting.isUseV4);

            if (isUseV4)
            {
                if (!hasInbounds)
                {
                    config["inbounds"] = JArray.Parse(@"[{}]");
                }
                config["inbounds"][0] = o;
            }
            else
            {
                config["inbound"] = o;
            }
        }


        #endregion
    }
}
