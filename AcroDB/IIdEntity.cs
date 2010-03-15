using System;

namespace AcroDB
{
    public interface IIdEntity
    {
        Guid GetEntityId();
        void SetEntityId(Guid id);
    }
}
