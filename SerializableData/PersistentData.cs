using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;


namespace LionStudios
{
    public interface IPersistentData
    {
        void SaveLocal();
        string GetCollection();
        string GetInstanceId();
        bool CheckEqualGeneric(IPersistentData persistentData);
        void SetInstanceId(string instanceId);
    }

    [Serializable]
    public abstract class PersistentData<T> : IPersistentData where T : PersistentData<T>, new()
    {
        public const string _DefaultInstanceId = "Singleton";

        //[HideInInspector]
        public string instanceId = _DefaultInstanceId;

        public void SetInstanceId(string instanceId)
        {
            this.instanceId = instanceId;
        }

        public virtual void SaveLocal()
        {
            if (Database.Paused)
            {
                Database.EnqueueEdit(SaveLocal);
                return;
            }

            //Debug.Log(new RText(this + " :: SaveLocal", Color.yellow));

            string filePath = GetFilePath(instanceId);

            try
            {
                // Binary doesn't seem to work on iOS.  Not sure about android...
                // For now just serialize as json.
                SaveJson(filePath);
                //#endif
                Database.AddReference(this as T);
                //Debug.Log("Saved " + Path.GetFileNameWithoutExtension(filePath) + " to filepath... " + filePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Unable to save persistent data!\nException: " + e);
            }
        }

        protected void SaveJson(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            string json = JsonUtility.ToJson(this);
            //Debug.Log("Save Path: " + filePath + "\n" + json);
            File.WriteAllText(filePath, json);
        }

        protected void SaveBinary(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (FileStream file = File.Create(filePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(file, this);
            }
        }

        public static bool Exists(string instanceId = _DefaultInstanceId)
        {
            //Debug.Log("Checking File exists = " + GetFilePath(instanceId));
            return File.Exists(GetFilePath(instanceId));
        }

        public static T Load(string instanceId = _DefaultInstanceId)
        {
            return LoadFile(GetFilePath(instanceId));
        }

        public static IEnumerable<T> LoadAll()
        {
            string classPath = GetClassPath();
            //Debug.Log("Database path :" + classPath);

            if (Directory.Exists(classPath))
            {
                string[] files = Directory.GetFiles(GetClassPath());

                foreach (string file in files)
                    yield return LoadFile(file);
            }
        }

        protected static T LoadFile(string filePath)
        {
            string instId = Path.GetFileNameWithoutExtension(filePath);
            // Have we already created this asset?
            T data = Database.GetInstance<T>(instId);

            if (data == null)
            {
                if (File.Exists(filePath))
                    data = LoadJSON(filePath);

                if (data == null)
                {
                    // Attempt to load default
                    string resourcePath = Database.GetFilePathResources<T>(instId);
                    //Debug.Log(resourcePath);
                    DefaultDataContainer<T> defaultData = Resources.Load(resourcePath) as DefaultDataContainer<T>;
                    //Debug.Log(new RText("Default Data = " + defaultData, Color.cyan));
                    if (defaultData != null)
                        data = UnityEngine.Object.Instantiate(defaultData).data;

                    // If still null, create a new instance
                    if (data == null)
                    {
                        //Debug.Log("Creating new data");
                        data = new T();
                    }
                    //else
                    //    Debug.Log("using default data");

                    data.SaveLocal();
                }

                Database.AddReference(data);
            }
            //else
            //{
                //Debug.Log("Using existing instance");
            //}

            
            return data;
        }

        protected static T LoadJSON(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                T obj = JsonUtility.FromJson<T>(json);
                //Debug.Log("LoadJSON - Obj = " + obj);
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to read JSON at filePath: " + filePath);
                Debug.LogError(e);
            }
            return default;
        }

        static T LoadBinary(string filePath)
        {
            try
            {
                using (FileStream file = File.Open(filePath, FileMode.Open))
                {
                    if (file.Length > 0)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        return (T)bf.Deserialize(file);
                    }
                }
            }
#pragma warning disable 0168
            catch (FileNotFoundException e)
#pragma warning restore 0168
            {
                // Do nothing, this is valid.
            }
            catch (Exception e)
            {
                Debug.LogError("PERSISTENT PLAYER STATE DATA IS CORRUPTED! RE-INITIALIZING DATA\nException: " + e);
            }

            return default;
        }

        protected static string GetFilePath(string instanceId)
        {
            return Database.GetFilePath<T>(instanceId);
        }

        protected static string GetClassPath()
        {
            return Database.GetClassPath<T>();
        }

        #region Delete

        public static void DeleteAllInstances()
        {
            string classPath = GetClassPath();
            if (Database.Paused)
            {
                if (Directory.Exists(classPath))
                {
                    string[] files = Directory.GetFiles(classPath);
                    foreach (string file in files)
                    {
                        string instId = Path.GetFileNameWithoutExtension(file);
                        T inst = GetInstance(Path.GetFileNameWithoutExtension(file));
                        if (inst == null)
                        {
                            Debug.LogWarning("Inst '" + instId + "' is null at path: " + file);
                            continue;
                        }
                        inst.Delete(file);
                    }
                }
            }
            else
            {
                if (Directory.Exists(classPath))
                {
                    Database.RemoveCollectionReference<T>();
                    Directory.Delete(classPath, true);
                }
            }

            //Debug.Log("Deleting All Persistent Data... ");
        }

        public string GetCollection()
        {
            return Database.GetDatabaseCollection<T>();
        }

        public string GetInstanceId()
        {
            return instanceId;
        }

        static T GetInstance(string instId)
        {
            return Database.GetInstance<T>(instId);
        }

        public static void DeleteInst(string instId)
        {
            T inst = GetInstance(instId);
            if (inst == null)
            {
                Debug.LogError("Cannot delete Instance :: Inst is null for id '" + instId + "'");
                return;
            }

            inst.Delete();
        }

        public virtual void Delete(string filePath)
        {
            if (Database.Paused)
            {
                Database.EnqueueEdit(Delete);
                return;
            }

            //Debug.Log(new RText(this + " :: Delete", Color.magenta));
            if (File.Exists(filePath))
            {
                Database.RemoveReference(this as T);
                File.Delete(filePath);
            }
        }

        public virtual void Delete()
        {
            if (Database.Paused)
            {
                Database.EnqueueEdit(Delete);
                return;
            }

            //Debug.Log(new RText(this + " :: Delete", Color.magenta));

            string filePath = GetFilePath(instanceId);
            if (File.Exists(filePath))
            {
                Database.RemoveReference(this as T);
                File.Delete(filePath);
            }
        }
        #endregion

        public virtual bool CheckEqualGeneric(IPersistentData inst)
        {
            return inst is T instance && CheckEqual(instance);
        }

        public virtual bool CheckEqual(T inst)
        {
            return instanceId == inst.instanceId;
        }

        public override string ToString()
        {
            return base.ToString() + " :: instanceId = " + instanceId;
        }

#if UNITY_EDITOR
        public static void CreateDefault<U>(string instanceId = _DefaultInstanceId) where U : DefaultDataContainer<T>
        {
            U defaultData = ScriptableObject.CreateInstance<U>();
            string filePath = "";// DefaultDataCreator.GetProjectResourcePath(instanceId);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            UnityEditor.AssetDatabase.CreateAsset(defaultData, filePath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }
}
