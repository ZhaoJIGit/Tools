using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMGPro.Models
{
    public class PupupWindowEventArgs<T> : EventArgs
    {
        public T Data { get; }

        public PupupWindowEventArgs(T data)
        {
            Data = data;
        }
    }
}
