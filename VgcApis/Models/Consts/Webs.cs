﻿namespace VgcApis.Models.Consts
{
    public static class Webs
    {
        public static int CheckForUpdateDelay = 15 * 1000;

        public static string ReleaseDownloadUrlTpl =
            @"https://github.com/UudrSgMEZ/V2RayGCon/releases/download/{0}/V2RayGCon.zip";

        public static string LoopBackIP = System.Net.IPAddress.Loopback.ToString();
        public static int DefaultProxyPort = 8080;

        public const string FakeRequestUrl = @"http://localhost:3000/pac/?&t=abc1234";
        public const string GoogleDotCom = @"https://www.google.com";

        public const string BingDotCom = @"https://www.bing.com";

        // https://www.bing.com/search?q=vmess&first=21
        public const string SearchUrlPrefix = BingDotCom + @"/search?q=";
        public const string SearchPagePrefix = @"&first=";


    }
}
