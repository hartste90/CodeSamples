using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace LionStudios
{
    public abstract class SerializableContainer : ScriptableObject
    {
        public const string DefaultPlayerDataPath = "DefaultPlayerData";

        public static List<SerializableContainer> containers = new List<SerializableContainer>();

        public static T CreateContainer<T>() where T : SerializableContainer
        {
            T container = CreateInstance<T>();
            containers.Add(container);
            //Debug.Log("Created Container '" + container.GetType() + "' :: Container Count = " + containers.Count);
            return container;
        }

        public void MakeContainer()
        {
            if (containers.Contains(this) == false)
                containers.Add(this);
        }

        private void OnDestroy()
        {
            //Debug.Log("Destroying Container '" + GetType() + "'");
            containers.Remove(this);
        }

        public static string GetDataResourcePath<T>()
        {
            return DefaultPlayerDataPath + "/" + typeof(T).Name;
        }

        public virtual void PrepareDataForSave() { }
        public abstract void Save();
        



#if UNITY_EDITOR
        public const string DefaultPlayerDataResourcePath = "Resources/" + DefaultPlayerDataPath;

        public void SaveDefaultData(string saveDataName = DefaultPlayerDataResourcePath)
        {
            //PrepareData();

            //Fleuriste.ExpansionData data = this as Fleuriste.ExpansionData;
            //if (data != null)
            //{
            //    foreach (Fleuriste.Expansion exp in data)
            //    {
            //        Debug.Log(exp.name + " :: " + exp.tiles.Count);
            //    }
            //}

            string savePath = "Assets/" + ProjectSettings.PlayerDataSavesPath + "/" + saveDataName;
            string fileName = savePath + "/" + GetType().Name + ".asset";
            string backupPath = savePath + "_BackUp";
            string backupFileName = backupPath + "/" + GetType().Name + ".asset";


            if (Directory.Exists(savePath) == false)
                Directory.CreateDirectory(savePath);

            if (File.Exists(fileName))
            {
                if (Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                File.Copy(fileName, backupFileName, true);
            }

            SerializableContainer clone = Instantiate(this);
            AssetDatabase.CreateAsset(clone, fileName);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        public static void SaveAllDefaultData(string saveDataName = DefaultPlayerDataResourcePath)
        {
            foreach (SerializableContainer container in containers)
                container.SaveDefaultData();
        }

        public static void SetDefault(string path)
        {
            string toPath = "Assets/" + ProjectSettings.PlayerDataSavesPath + "/" + DefaultPlayerDataResourcePath;
            Debug.Log("Copy: " + path + "\nTo: " + toPath);
            FileSystemTools.CopyDir(path, toPath, true);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        public static void Load(string path)
        {
            Database.DeleteAllData();
            foreach (string containerPath in Directory.GetFiles(path))
            {
                string extension = Path.GetExtension(containerPath);
                if (extension == ".meta")
                    continue;

                //Debug.Log(containerPath);
                SerializableContainer container = AssetDatabase.LoadAssetAtPath<SerializableContainer>(containerPath);
                container.Save();
            }
        }
#endif
    }
}
