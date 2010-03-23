using System;
using System.Linq;
using AcroDB.AutoMigration;

namespace AcroDB.Attributes
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class AcroDbMigrationAttribute : Attribute
    {
        public MigratorResult WhenToFire
        {
            get; private set;
        }

        public Type Event
        {
            get; private set;
        }

        private IMigrationTrack _obj;
        public void Call(MigratorResult result, Type @interface)
        {
            if (_obj == null)
                _obj = (IMigrationTrack)Activator.CreateInstance(Event);
            _obj.MigrationChange(result, @interface);
        }

        public AcroDbMigrationAttribute(MigratorResult whenToFire, Type migrationTrackType)
        {
            WhenToFire = whenToFire;
            if (!migrationTrackType.GetInterfaces().Any(f => f == typeof(IMigrationTrack)))
                throw new ArgumentException("migrationTrackType");
            Event = migrationTrackType;
        }
    }
}
