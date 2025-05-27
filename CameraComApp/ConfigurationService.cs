using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CameraComApp
{
    public class ConfigurationService
    {
        private readonly string _iniFilePath;
        private readonly Dictionary<string, string> _settings;

        public ConfigurationService(string iniFilePath)
        {
            _iniFilePath = iniFilePath;
            _settings = new Dictionary<string, string>();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_iniFilePath))
                {
                    foreach (string line in File.ReadAllLines(_iniFilePath))
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("["))
                            continue;

                        string[] parts = trimmedLine.Split('=');
                        if (parts.Length >= 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            _settings[key] = value;
                        }
                    }
                }
                else
                {
                    // Create default INI file if it doesn't exist
                    File.WriteAllText(_iniFilePath,
                        "; CameraComApp Configuration\n" +
                        "UsbScan =0\n"+
                        "COMPort=COM21\n" +
                        "type = COM\n" +
                        "IPAddress=127.0.0.1\n" +
                        "Port=2222\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading INI file: {ex.Message}");
            }
        }

        public string GetSetting(string key, string defaultValue)
        {
            string value;
            return _settings.TryGetValue(key, out  value) ? value : defaultValue;
        }

        public void SaveSetting(string key, string value)
        {
            _settings[key] = value;
            try
            {
                List<string> lines = new List<string> { "; CameraComApp Configuration" };
                foreach (KeyValuePair<string, string> setting in _settings)
                {
                    lines.Add($"{setting.Key}={setting.Value}");
                }
                File.WriteAllLines(_iniFilePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving INI file: {ex.Message}");
            }
        }
    }
}