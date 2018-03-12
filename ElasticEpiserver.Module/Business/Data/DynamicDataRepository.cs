using System;
using System.Collections.Generic;
using System.Linq;
using ElasticEpiserver.Module.Business.Data.Entities;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data
{
    public interface IDynamicDataRepository<T> where T : DynamicDataBase
    {
        IList<T> ReadAll();
        T ReadById(Guid id);
        void Create(T model);
        bool CreateIfNotExisting(T model, Func<T, bool> predicateForExisting);
        void Update(T model);
        void Delete(T model);
        T SingleOrDefault(Func<T, bool> predicate);
    }

    public class DynamicDataRepository<T> : IDynamicDataRepository<T> where T : DynamicDataBase
    {
        public IList<T> ReadAll()
        {
            return GetStore().Items<T>().ToList();
        }

        public T ReadById(Guid id)
        {
            return SingleOrDefault(i => i.Id.ExternalId == id);
        }

        public void Create(T model)
        {
            Update(model);
        }

        public bool CreateIfNotExisting(T model, Func<T, bool> predicateForExisting)
        {
            var isExisting = SingleOrDefault(predicateForExisting) != null;

            if (!isExisting)
            {
                Create(model);
                return true;
            }

            return false;
        }

        public void Update(T model)
        {
            GetStore().Save(model);
        }

        public void Delete(T model)
        {
            GetStore().Delete(model.Id);
        }

        public T SingleOrDefault(Func<T, bool> predicate)
        {
            return ReadAll().SingleOrDefault(predicate);
        }

        private static DynamicDataStore GetStore()
        {
            return DynamicDataStoreFactory.Instance.CreateStore(typeof(T));
        }
    }
}