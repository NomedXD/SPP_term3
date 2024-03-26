using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracer;

namespace ProgramLab1.SomeClasses
{
    public class Bar
    {
        private ITracer _tracer;

        internal Bar(ITracer tracer)
        {
            _tracer = tracer;
        }

        public void InnerMethod()
        {
            _tracer.StartTrace();
            int a = 5;
            int b = 7;
            int res = a + b;
            Thread.Sleep(100);
            _tracer.StopTrace();
        }
    }
}
