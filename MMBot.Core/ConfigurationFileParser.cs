using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MMBot
{
    public class ConfigurationFileParser
    {
        readonly Dictionary<string, Dictionary<string, string>> _ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
        private readonly string _file;

        /// <summary>
        /// Initialize an INI file
        /// Load it if it exists
        /// </summary>
        /// <param name="file">Full path where the INI file has to be read from or written to</param>
        public ConfigurationFileParser(string file)
        {
            _file = file;

            if (!File.Exists(file))
                return;

            Load();
        }

        /// <summary>
        /// Load the INI file content
        /// </summary>
        public void Load()
        {
            var txt = File.ReadAllText(_file);

            var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            _ini[""] = currentSection;

            var lines = txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("#"))
                .Select((t, i) => new
                {
                    idx = i,
                    text = t.Trim()
                });


            foreach (var l in lines)
            {
                var line = l.text;

                if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                {
                    currentSection.Add(";" + l.idx, line);
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    _ini[line.Substring(1, line.Length - 2)] = currentSection;
                    continue;
                }

                var idx = line.IndexOf("=", StringComparison.Ordinal);
                if (idx == -1)
                    currentSection[line] = "";
                else
                {
                    var value = line.Substring(idx + 1).TrimStart();

                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    currentSection[line.Substring(0, idx).Trim()] = value;
                }
            }
        }

        /// <summary>
        /// Get a parameter value at the root level
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <returns></returns>
        public string GetValue(string key)
        {
            return GetValue(key, "", "");
        }

        /// <summary>
        /// Get a parameter value in the section
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="section">section</param>
        /// <returns></returns>
        public string GetValue(string key, string section)
        {
            return GetValue(key, section, "");
        }

        /// <summary>
        /// Returns a parameter value in the section, with a default value if not found
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="section">section</param>
        /// <param name="default">default value</param>
        /// <returns></returns>
        public string GetValue(string key, string section, string @default)
        {
            if (!_ini.ContainsKey(section))
                return @default;

            if (!_ini[section].ContainsKey(key))
                return @default;

            return _ini[section][key];
        }

        /// <summary>
        /// Save the INI file
        /// </summary>
        public void Save()
        {
            var sb = new StringBuilder();
            foreach (var section in _ini)
            {
                if (section.Key != "")
                {
                    sb.AppendFormat("[{0}]", section.Key);
                    sb.AppendLine();
                }

                foreach (var keyValue in section.Value)
                {
                    if (keyValue.Key.StartsWith(";"))
                    {
                        sb.Append(keyValue.Value);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendFormat("{0}={1}", keyValue.Key, keyValue.Value);
                        sb.AppendLine();
                    }
                }

                if (!endWithCRLF(sb))
                    sb.AppendLine();
            }

            File.WriteAllText(_file, sb.ToString());
        }

        bool endWithCRLF(StringBuilder sb)
        {
            if (sb.Length < 4)
            {
                return sb[sb.Length - 2] == '\r' &&
                       sb[sb.Length - 1] == '\n';
            }

            return sb[sb.Length - 4] == '\r' &&
                   sb[sb.Length - 3] == '\n' &&
                   sb[sb.Length - 2] == '\r' &&
                   sb[sb.Length - 1] == '\n';
        }

        /// <summary>
        /// Write a parameter value at the root level
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="value">parameter value</param>
        public void WriteValue(string key, string value)
        {
            WriteValue(key, "", value);
        }

        /// <summary>
        /// Write a parameter value in a section
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="section">section</param>
        /// <param name="value">parameter value</param>
        public void WriteValue(string key, string section, string value)
        {
            Dictionary<string, string> currentSection;
            if (!_ini.ContainsKey(section))
            {
                currentSection = new Dictionary<string, string>();
                _ini.Add(section, currentSection);
            }
            else
                currentSection = _ini[section];

            currentSection[key] = value;
        }

        /// <summary>
        /// Get all the keys names in a section
        /// </summary>
        /// <param name="section">section</param>
        /// <returns></returns>
        public string[] GetKeys(string section)
        {
            if (!_ini.ContainsKey(section))
                return new string[0];

            return _ini[section].Keys.ToArray();
        }

        /// <summary>
        /// Get all the section names of the INI file
        /// </summary>
        /// <returns></returns>
        public string[] GetSections()
        {
            return _ini.Keys.Where(t => t != "").ToArray();
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return (from section in GetSections()
                          from key in GetKeys(section)
                          let value = GetValue(key, section, null)
                          select new
                          {
                              Key = string.Format("{0}_{1}", section, key),
                              Value = value
                          }).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}