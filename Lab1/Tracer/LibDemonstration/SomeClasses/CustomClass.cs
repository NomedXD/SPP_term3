using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracer;

namespace ProgramLab1.SomeClasses
{
    // Не использую
    class CustomClass
    {
        private ITracer _tracer;

        internal CustomClass(ITracer tracer)
        {
            _tracer = tracer;
        }

        public void MethodA()
        {
            _tracer.StartTrace();
            int a = 5;
            int b = 7;
            int res = a+ b;
            MethodB();
            MethodC();
            _tracer.StopTrace();
        }

        private void MethodB()
        {
            _tracer.StartTrace();
            int a = 5;
            int b = 7;
            int res = a - b;
            _tracer.StopTrace();
        }

        private void MethodC()
        {
            _tracer.StartTrace();
            int a = 5;
            int b = 7;
            int res = a * b;
            MethodD();
            _tracer.StopTrace();
        }

        private void MethodD()
        {
            _tracer.StartTrace();
            int a = 5;
            int b = 7;
            int res = a + b - 2;
            _tracer.StopTrace();
        }
    }
}
