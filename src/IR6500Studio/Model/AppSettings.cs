// IR6500 Studio
// Copyright (c) 2026 Marius Sumutiu. All rights reserved.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using IR6500Studio.Protocol;

namespace IR6500Studio.Model {
    public sealed class AppSettings {
        // Connection
        public string PortName { get; set; } = "";
        public int BaudRate { get; set; } = 9600;
        public int Address { get; set; } = 1;
        public int ResponseTimeoutMs { get; set; } = 500;
        public int Retries { get; set; } = 2;
        public int PollIntervalMs { get; set; } = 500;
        public bool DtrEnable { get; set; } = true;
        public bool RtsEnable { get; set; } = true;
        public bool UseSimulator { get; set; } = false;
        public double SimulatorSpeed { get; set; } = 1;

        // Profiles
        public double MaxTemperature { get; set; } = 230;
        public UnusedStepMode UnusedSteps { get; set; } = UnusedStepMode.Zeros;
        public bool VerifyAfterWrite { get; set; } = true;
        public double PreviewStartTemperature { get; set; } = 25;
        public string LastProfilePath { get; set; } = "";
        public int LastSlot { get; set; } = 0;

        // Logging
        public bool AutoRecordRuns { get; set; } = true;
        public int RecordAfterRunSeconds { get; set; } = 60;
        public string LogFolder { get; set; } = "";

        // Window
        public int WindowX { get; set; } = int.MinValue;
        public int WindowY { get; set; } = int.MinValue;
        public int WindowWidth { get; set; } = 1360;
        public int WindowHeight { get; set; } = 860;
        public bool WindowMaximized { get; set; }

        [XmlIgnore]
        public string EffectiveLogFolder =>
            string.IsNullOrWhiteSpace(LogFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IR6500 Studio", "Logs")
                : LogFolder;

        public static string SettingsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IR6500 Studio");

        public static string ProfilesFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IR6500 Studio", "Profiles");

        private static string SettingsPath => Path.Combine(SettingsFolder, "settings.xml");

        private static readonly XmlSerializer Serializer = new(typeof(AppSettings));

        public static AppSettings Load() {
            try {
                if (File.Exists(SettingsPath))
                    using (var r = XmlReader.Create(SettingsPath))
                        return ((AppSettings)Serializer.Deserialize(r)).Sanitize();
            } catch { /* corrupt settings: fall back to defaults */ }
            return new AppSettings();
        }

        public void Save() {
            try {
                Directory.CreateDirectory(SettingsFolder);
                using var w = XmlWriter.Create(SettingsPath, new XmlWriterSettings { Indent = true });
                Serializer.Serialize(w, this);
            } catch { /* settings are best-effort */ }
        }

        private AppSettings Sanitize() {
            if (Address < 0 || Address > 99) Address = 1;
            if (BaudRate <= 0) BaudRate = 9600;
            ResponseTimeoutMs = Math.Max(100, Math.Min(5000, ResponseTimeoutMs));
            Retries = Math.Max(0, Math.Min(5, Retries));
            PollIntervalMs = Math.Max(200, Math.Min(10000, PollIntervalMs));
            if (MaxTemperature <= 0 || MaxTemperature > 500) MaxTemperature = 230;
            if (SimulatorSpeed < 1 || SimulatorSpeed > 50) SimulatorSpeed = 1;
            if (LastSlot < 0 || LastSlot > 9) LastSlot = 0;
            RecordAfterRunSeconds = Math.Max(0, Math.Min(3600, RecordAfterRunSeconds));
            return this;
        }
    }

    /// <summary>Writes telemetry to a CSV file (invariant culture, Excel-friendly).</summary>
    public sealed class CsvRecorder : IDisposable {
        private readonly StreamWriter _w;
        private readonly DateTime _start;

        public CsvRecorder(string folder, string label) {
            Directory.CreateDirectory(folder);
            _start = DateTime.Now;
            string safe = string.IsNullOrWhiteSpace(label) ? "session" : MakeSafe(label);
            FilePath = Path.Combine(folder, $"{_start:yyyy-MM-dd_HHmmss}_{safe}.csv");
            _w = new StreamWriter(FilePath, false, new UTF8Encoding(true));
            _w.WriteLine("timestamp,elapsed_s,pv_c,sp_c,output_pct,state,segment");
            _w.Flush();
        }

        public string FilePath { get; }
        public int Rows { get; private set; }

        public void Write(Telemetry t) {
            var c = CultureInfo.InvariantCulture;
            _w.WriteLine(string.Join(",",
                t.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", c),
                (t.Time - _start).TotalSeconds.ToString("0.0", c),
                t.Pv?.ToString("0.0", c) ?? "",
                t.Sp?.ToString("0.0", c) ?? "",
                t.Op?.ToString("0", c) ?? "",
                t.State.ToString(),
                t.Segment?.ToString(c) ?? ""));
            Rows++;
            if (Rows % 10 == 0) _w.Flush();
        }

        private static string MakeSafe(string s) {
            var sb = new StringBuilder();
            foreach (char ch in s)
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), ch) >= 0 || ch == ' ' ? '_' : ch);
            string r = sb.ToString();
            return r.Length > 40 ? r.Substring(0, 40) : r;
        }

        public void Dispose() {
            try { _w.Flush(); _w.Dispose(); } catch { }
        }
    }
}
