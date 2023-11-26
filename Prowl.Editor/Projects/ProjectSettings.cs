﻿using Prowl.Runtime.Utils;

namespace Prowl.Editor
{

    public interface IProjectSetting
    {
        // Define the properties and methods for your project settings
    }

    public class ProjectSettings
    {
        private Dictionary<Type, IProjectSetting> settings = new ();

        internal IEnumerable<Type> GetRegisteredSettingTypes() => settings.Keys;

        public T GetSetting<T>() where T : IProjectSetting, new()
        {
            return (T)GetSetting(typeof(T));
        }

        public IProjectSetting GetSetting(Type settingType)
        {
            if (settings.TryGetValue(settingType, out var setting))
                return setting;
            else
            {
                var newSetting = Activator.CreateInstance(settingType) as IProjectSetting;
                settings[settingType] = newSetting;
                return newSetting;
            }
        }

        public void Save(string? path = null)
        {
            path ??= Path.Combine(Project.ProjectDirectory, "ProjectSettings.setting");

            // Convert Type keys to string representations
            var convertedSettings = settings.ToDictionary(entry => entry.Key.FullName, entry => entry.Value );

            // Serialize settings to JSON
            string json = JsonUtility.Serialize(convertedSettings);

            // Write JSON to file
            File.WriteAllText(path, json);
        }

        public static ProjectSettings Load()
        {
            string filePath = Path.Combine(Project.ProjectDirectory, "ProjectSettings.setting");

            if (File.Exists(filePath))
            {
                // Read JSON from file
                string json = File.ReadAllText(filePath);

                // Deserialize JSON to settings
                var loadedSettings = JsonUtility.Deserialize<Dictionary<string, IProjectSetting>>(json);

                // Remove any settings whos type cannot be inferred with Type.GetType
                var convertedSettings = loadedSettings.Where(x => Type.GetType(x.Key) != null).ToDictionary(x => Type.GetType(x.Key), x => x.Value);

                // Create a new ProjectSettings instance and set the loaded settings
                return new() { settings = convertedSettings };
            }
            else
            {
                // If the file doesn't exist, return a new instance of ProjectSettings
                var newSettings = new ProjectSettings();
                newSettings.Save();
                return newSettings;
            }
        }
    }
}
