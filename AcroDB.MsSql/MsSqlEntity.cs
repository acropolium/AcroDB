using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcroDB.MsSql
{
    public class MsSqlEntity : BaseEntity
    {
        public AcroDataContext AcroContext { get; set; }
    }
}
