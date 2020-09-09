using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nakama;
using System.Linq;


namespace LionStudios
{
    public interface IPersistentDataRemote : IPersistentData
    {
        WriteStorageObject GetWriteStorageObject();
        void EnqueueSaveRemote();
        void SaveRemote();
        Task<IApiStorageObjectAcks> SaveRemoteAsync();
        void EnqueueDeleteRemote();
        void DeleteRemote();
        Task DeleteRemoteAsync();
    }

    [Serializable]
    public class PersistentDataRemote<T> : PersistentData<T>, IPersistentDataRemote where T : PersistentDataRemote<T>, new()
    {

        #region Load
        public async Task<T> ReloadRemoteAsync()
        {
            return await LoadRemoteAsync(null, instanceId);
        }

        public static async Task<T> LoadRemoteAsyncSystem()
        {
            return await LoadRemoteAsync(null, null, null, false);
        }

        public static async Task<T> LoadRemoteAsync(ISession session = null, string key = _DefaultInstanceId, string userId = null, bool userRead = true)
        {
            if (session == null)
            {
                await NakamaController.AuthFlow();
                session = NakamaController.Session;
            }

            if (string.IsNullOrEmpty(key))
                key = _DefaultInstanceId;

            StorageObjectId storageObjectId = ObjectPool<StorageObjectId>.GetObject();

            storageObjectId.Collection = Database.GetDatabaseCollection<T>();
            storageObjectId.Key = key;
            if (userRead)
                storageObjectId.UserId = userId ?? session.UserId;
            else
                storageObjectId.UserId = null;

            try
            {
                var result = await NakamaController.ReadStorageObjectsAsync(session, storageObjectId);

                storageObjectId.UserId = null;
                ObjectPool<StorageObjectId>.ReturnObject(storageObjectId);

                if (result != null && result.Objects != null && result.Objects.Any())
                {
                    var obj = result.Objects.First();
                    //Debug.LogFormat("{0} :: {1} :: {2}", obj.Collection, obj.Key, obj.Value);
                    return JsonUtility.FromJson<T>(obj.Value);
                }
            }
            catch(Exception e)
            {
                if (NakamaController.InternetConnection == false)
                    NakamaController.Connection = NakamaConnection.Offline;

                throw e;
            }

            return default;
        }

        //public static StorageObjectId GetStorageObjectId

        public static async Task<T> LoadRemoteSystemStorage(ServerConfig config, string key = _DefaultInstanceId)
        {
            StorageObjectId storageObjectId = ObjectPool<StorageObjectId>.GetObject();
            storageObjectId.Collection = Database.GetDatabaseCollection<T>();
            storageObjectId.Key = key;
            storageObjectId.UserId = null;
            T obj = await NakamaSystemStorage.ReadObject<T>(config, storageObjectId);
            ObjectPool<StorageObjectId>.ReturnObject(storageObjectId);
            return obj;
        }

        public static async Task<T[]> LoadRemoteSystemStorage(ServerConfig config, List<string> keys)
        {
            string collection = Database.GetDatabaseCollection<T>();
            StorageObjectId[] storageObjectIds = new StorageObjectId[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                StorageObjectId storageObjectId = ObjectPool<StorageObjectId>.GetObject();
                storageObjectId.Collection = collection;
                storageObjectId.Key = keys[i];
                storageObjectIds[i] = storageObjectId;
            }
            T[] objs = await NakamaSystemStorage.ReadObjects<T>(config, storageObjectIds);
            ObjectPool<StorageObjectId>.ReturnObjects(storageObjectIds);
            return objs;
        }

        public static async Task<IEnumerable<T>> LoadAllRemoteAsync()
        {
            try
            {
                return await NakamaController.RetrieveCollection<T>();
            }
            catch(Exception e)
            {
                if (NakamaController.InternetConnection == false)
                    NakamaController.Connection = NakamaConnection.Offline;
                throw e;
            }
        }

        public static async Task LoadAllRemoteAsync(ICollection<T> list)
        {
            try
            { 
                await NakamaController.RetrieveCollection(list);
            }
            catch(Exception e)
            {
                if (NakamaController.InternetConnection == false)
                    NakamaController.Connection = NakamaConnection.Offline;
                throw e;
            }
        }
        #endregion

        #region Save
        void EnqueueWriteObject()
        {
            UpdateWriteStorageObject();
            NakamaController.EnqueueWriteObject(_WriteStorageObject);
        }

        public virtual void EnqueueSaveRemote()
        {
            if (Database.Paused)
            {
                Database.EnqueueEdit(EnqueueSaveRemote);
                return;
            }

            //Debug.Log(new RText(this + " :: EnqueueSaveRemote", Color.cyan));
            //Debug.Log("EnqueueSaveRemote");

            SaveLocal();
            EnqueueWriteObject();
        }

        public static void EnqueueSaveCollection(IEnumerable<T> collection)
        {
            foreach (T obj in collection)
                obj.EnqueueSaveRemote();
        }

        public virtual void SaveRemote()
        {
            EnqueueSaveRemote();

            if (Database.Paused)
                return;

            try
            {
                NakamaController.ProcessWriteQueue();
            }
            catch(Exception e)
            {
                Debug.Log(e);
            }
        }

        public virtual async Task<IApiStorageObjectAcks> SaveRemoteAsync()
        {
            EnqueueSaveRemote();

            return await NakamaController.ProcessWriteQueue();
        }

        public static async Task<IApiStorageObjectAcks> SaveCollectionRemoteAsync(List<T> collection)
        {
            EnqueueSaveCollection(collection);

            return await NakamaController.ProcessWriteQueue();
        }
        #endregion


        #region Delete

        public void EnqueueDeleteRemote()
        {
            if (Database.Paused)
            {
                Database.EnqueueEdit(EnqueueDeleteRemote);
                return;
            }

//            NakamaController.DequeueWriteObject(_WriteStorageObject);
            Delete();

            StorageObjectId storageObj = ObjectPool<StorageObjectId>.GetObject();
            storageObj.Collection = Database.GetDatabaseCollection<T>();
            storageObj.Key = instanceId;
            storageObj.UserId = null;
            NakamaController.EnqueueDeleteObject(storageObj);
        }

        public void DeleteRemote()
        {
            EnqueueDeleteRemote();
            NakamaController.ProcessDeleteQueue();
        }

        public async Task DeleteRemoteAsync()
        {
            EnqueueDeleteRemote();
            await NakamaController.ProcessDeleteQueue();
        }

        public static void EnqueueDeleteRemoteCollection(List<T> collection)
        {
            foreach (T obj in collection)
                obj.EnqueueDeleteRemote();
        }

        public static async Task DeleteRemoteCollectionAsync(List<T> collection)
        {
            EnqueueDeleteRemoteCollection(collection);
            await NakamaController.ProcessDeleteQueue();
        }
        #endregion

        void UpdateWriteStorageObject()
        {
            _WriteStorageObject.collection = GetCollection();
            _WriteStorageObject.key = instanceId;
            _WriteStorageObject.value = JsonUtility.ToJson(this);
        }

        public WriteStorageObject GetWriteStorageObject()
        {
            UpdateWriteStorageObject();
            return _WriteStorageObject;
        }

        WriteStorageObject _WriteStorageObject = new WriteStorageObject();
    }
}
