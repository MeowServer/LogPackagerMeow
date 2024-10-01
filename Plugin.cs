using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LogPackagerMeow
{
    public class Plugin
    {
        public static Plugin Instance { get; private set; }

        public readonly string LocalAdminLogPath = Path.Combine(PluginAPI.Helpers.Paths.AppData, "SCP Secret Laboratory", "LocalAdminLogs", PluginAPI.Core.Server.Port.ToString());
        public readonly string ServerLogPath = Path.Combine(PluginAPI.Helpers.Paths.AppData, "SCP Secret Laboratory", "ServerLogs", PluginAPI.Core.Server.Port.ToString());

        [PluginAPI.Core.Attributes.PluginEntryPoint("LogPackagerMeow", "1.0.0", "A packager used to pack log files", "MeowServer")]
        public void LoadPlugin()
        {
            Instance = this;

            BindEvent();

            PluginAPI.Core.Log.Info("LogPackagerMeow have been successfully enabled");
        }

        //IPlugin
        public void BindEvent()
        {
            PluginAPI.Events.EventManager.RegisterEvents<Plugin>(this);
        }

        [PluginAPI.Core.Attributes.PluginEvent(PluginAPI.Enums.ServerEventType.RoundRestart)]
        public void OnRoundRestart(PluginAPI.Events.RoundRestartEvent ev)
        {
            var localAdminLogCount = Directory.GetFiles(LocalAdminLogPath).Count(x => !x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase));
            if (localAdminLogCount >= 500)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var timer = System.Diagnostics.Stopwatch.StartNew();
                        PluginAPI.Core.Log.Info("Packaging local admin logs");

                        var compressTo = Path.Combine(LocalAdminLogPath, $"Log Package {DateTime.Now:yyyy-MM-dd HH.mm}.zip");
                        CompressLog(LocalAdminLogPath, compressTo);

                        timer.Stop();
                        PluginAPI.Core.Log.Info("Finish packaging local admin logs, total elapsed time: " + timer.ElapsedMilliseconds + "ms");
                    }
                    catch(Exception ex)
                    {
                        PluginAPI.Core.Log.Error("Error while packaging local admin logs: " + ex.Message);
                    }
                });
            }

            var serverLogCount = Directory.GetFiles(ServerLogPath).Count(x => !x.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase));
            if (serverLogCount >= 500)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var timer = System.Diagnostics.Stopwatch.StartNew();
                        PluginAPI.Core.Log.Info("Packaging server logs");

                        var compressTo = Path.Combine(ServerLogPath, $"Log Package {DateTime.Now:yyyy-MM-dd HH.mm}.zip");
                        CompressLog(ServerLogPath, compressTo);

                        timer.Stop();
                        PluginAPI.Core.Log.Info("Finish packaging server logs, total elapsed time: " + timer.ElapsedMilliseconds + "ms");
                    }
                    catch(Exception ex)
                    {
                        PluginAPI.Core.Log.Error("Error while packaging server logs: " + ex.Message);
                    }
                });
            }
        }

        public void CompressLog(string logFolderPath, string compressTo, bool skipNewLogs = true)
        {
            List<string> compressedLogFiles = new List<string>();

            using (FileStream zipToOpen = new FileStream(compressTo, FileMode.CreateNew))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                foreach (var filePath in Directory.GetFiles(logFolderPath))
                {
                    if (filePath.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (skipNewLogs && DateTime.Now - File.GetLastWriteTime(filePath) < TimeSpan.FromSeconds(30))
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