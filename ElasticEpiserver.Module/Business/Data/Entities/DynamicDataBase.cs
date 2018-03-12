using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace ElasticEpiserver.Module.Business.Data.Entities
{
    public abstract class DynamicDataBase : IDynamicData
    {
        public Identity Id { get; set; }

        protected DynamicDataBase()
        {
            Id = Identity.NewIdentity(Guid.NewGuid());
        }
    }
}