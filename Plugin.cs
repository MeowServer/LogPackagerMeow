using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LogPackagerMeow
{
#if EXILED
    using Exiled.API.Features;
    using Exiled.API.Interfaces;

    public class ExiledPlugin : Plugin<ExiledPluginConfig>
    {
        public override string Name => "LogPackagerMeow";
        public override string Author => "MeowServer";
        public override Version Version => new Version(1,0,0);

        public override void OnEnabled()
        {
            Plugin.Instance = new Plugin();
            PluginConfig.Instance = this.Config;

            PluginAPI.Events.EventManager.RegisterEvents<Plugin>(Plugin.Instance);

            PluginAPI.Core.Log.Info("LogPackagerMeow have been successfully enabled");
        }

        public override void OnDisabled()
        {
            if(Plugin.Instance != null)
                PluginAPI.Events.EventManager.UnregisterEvents<Plugin>(Plugin.Instance);

            Plugin.Instance = null;
            PluginConfig.Instance = null;

            PluginAPI.Core.Log.Info("LogPackagerMeow have been successfully disabled");
        }    
    }

    public class ExiledPluginConfig: PluginConfig, IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
    }
#endif

    public class PluginConfig
    {
        public static PluginConfig Instance;

        public int LocalAdminLogMaxCount { get; set; } = 500;
        public int ServerLogMaxCount { get; set; } = 500;
    }

    public class Plugin
    {
        public static Plugin Instance { get; internal set; }

        [PluginAPI.Core.Attributes.PluginConfig]
        public PluginConfig Config;

        public readonly string LocalAdminLogPath = Path.Combine(PluginAPI.Helpers.Paths.AppData, "SCP Secret Laboratory", "LocalAdminLogs", PluginAPI.Core.Server.Port.ToString());
        public readonly string ServerLogPath = Path.Combine(PluginAPI.Helpers.Paths.AppData, "SCP Secret Laboratory", "ServerLogs", PluginAPI.Core.Server.Port.ToString());

        [PluginAPI.Core.Attributes.PluginEntryPoint("LogPackagerMeow", "1.0.0", "A packager used to pack log files", "MeowServer")]
        public void LoadPlugin()
        {
            Instance = this;
            PluginConfig.Instance = Config;
            PluginAPI.Events.EventManager.RegisterEvents<Plugin>(this);
            PluginAPI.Core.Log.Info("LogPackagerMeow have been successfully enabled");
        }

        [PluginAPI.Core.Attributes.PluginEvent(PluginAPI.Enums.ServerEventType.RoundRestart)]
        public void OnRoundRestart(PluginAPI.Events.RoundRestartEvent ev)
        {
            var localAdminLogCount = Directory.GetFiles(LocalAdminLogPath).Count(x => !x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase));
            if (localAdminLogCount >= PluginConfig.Instance.LocalAdminLogMaxCount)
            {
                PackLog(LocalAdminLogPath);
            }

            var serverLogCount = Directory.GetFiles(ServerLogPath).Count(x => !x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase));
            if (serverLogCount >= PluginConfig.Instance.ServerLogMaxCount)
            {
                PackLog(ServerLogPath);
            }
        }

        public void PackLog(string path)
        {
            Task.Run(() =>
            {
                try
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    PluginAPI.Core.Log.Info("Packaging server logs");

                    var compressTo = Path.Combine(path, $"Log Package {DateTime.Now:yyyy-MM-dd HH.mm}.zip");
                    CompressFolder(path, compressTo);

                    timer.Stop();
                    PluginAPI.Core.Log.Info("Finish packaging server logs, total elapsed time: " + timer.ElapsedMilliseconds + "ms");
                }
                catch (Exception ex)
                {
                    PluginAPI.Core.Log.Error("Error while packaging server logs: " + ex.Message);
                }
            });
        }

        public void CompressFolder(string logFolderPath, string compressTo, bool skipNewFiles = true)
        {
            List<string> compressedLogFiles = new List<string>();

            using (FileStream zipToOpen = new FileStream(compressTo, FileMode.CreateNew))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                foreach (var filePath in Directory.GetFiles(logFolderPath))
                {
                    if (filePath.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (skipNewFiles && DateTime.Now - File.GetLastWriteTime(filePath) < TimeSpan.FromSeconds(30))
                        continue;

                    ZipArchiveEntry readmeEntry = archive.CreateEntry($"{Path.GetFileName(filePath)}.txt");
                    using (StreamWriter writer = new StreamWriter(readmeEntry.Open()))
                    {
                        writer.Write(File.ReadAllText(filePath));
                    }

                    compressedLogFiles.Add(filePath);
                }
            }

            foreach (var file in compressedLogFiles)
            {
                File.Delete(file);
            }
        }
    }
}