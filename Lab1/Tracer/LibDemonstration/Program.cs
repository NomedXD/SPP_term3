using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ProgramLab1.SomeClasses;
using Tracer;


namespace ProgramLab1
{
    class Program
    {
        // _tracer0 - измеритель для главного потока(вызывающего), который работает в Main
        static private Tracer.Tracer _tracer0;
        // _tracer1,_tracer2  - измерители для двух дочерних потоков
        static private Tracer.Tracer _tracer1;
        static private Tracer.Tracer _tracer2;

        static void Main()
        {
            // Назначение главному измерителю айди главного потока
            _tracer0 = new Tracer.Tracer(Thread.CurrentThread.ManagedThreadId);
            // Назначение дочерним измерителям айди их потоков(прямо в конструкторе)
            Thread thread1 = new Thread(Thread1);
            Thread thread2 = new Thread(Thread2);
            // Выполнение дочерних потоков
            thread1.Start();
            thread2.Start();
            // Измерения класса Foo в главном потоке
            Foo foo = new Foo(_tracer0);
            foo.MyMethod();
            // Главный поток ожидает два других
            thread1.Join();
            thread2.Join();
            // Результаты Tracer's(TraceResultStruct всех потков) объединяются в общую map 
            _tracer2.GetTraceResult();
            _tracer1.GetTraceResult();
            _tracer0.GetTraceResult();
            // Результаты из общей map записываются в файлы json и XML
            _tracer1.GetThreadsResult("..//..//..//Result.json", "..//..//..//Result.xml");
        }
        // Поток 1
        static public void Thread1()
        {
            _tracer1 = new Tracer.Tracer(Thread.CurrentThread.ManagedThreadId);
            Foo foo = new Foo(_tracer1);
            foo.MyMethod();
            foo.MySecondMethod();
        }
        // Поток 2
        static public void Thread2()
        {
            _tracer2 = new Tracer.Tracer(Thread.CurrentThread.ManagedThreadId);
            Foo foo = new Foo(_tracer2);
            Bar bar = new Bar(_tracer2);
            foo.MyMethod();
            bar.InnerMethod();
        }

    }
}
