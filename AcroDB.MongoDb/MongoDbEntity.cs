using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcroDB.MongoDb
{
    public class MongoDbEntity : BaseEntity
    {
        public AcroDataContext AcroContext { get; set; }
    }
}
