using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Helper
{
    /// <summary>
    /// Stores an object of type T alongside a Date
    /// </summary>
    struct Dated<T>
    {
        public DateTime Date { get; set; }
        public T Value { get; set; }

        public Dated(DateTime date, T value){
            Date = date;
            Value = value;
        }
    }
}
