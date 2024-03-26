using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tracer.ResultOutput;
using Tracer.Serialization;

namespace Tracer
{
    /*Tracer должен собирать следующую информацию об измеряемом методе:
    имя метода;
    имя класса с измеряемым методом;
    время выполнения метода.
    */
    public class Tracer : ITracer
    {
        // Результаты выполнения для каждого отдельного tracer
        private TraceResultStruct _traceResultStruct;
        // Текущий и предыдущий методы
        private MethodElement _method;
        private MethodElement _prevMethod;
        // Текущая глубина вложенности метода
        private int _currentMethodDepth;
        // Статическая обзая map для складывания всех результатов
        private static ConcurrentDictionary<int, TraceResultStruct> _traceMap;

        // Проходим по всем методам, для которых измерения попали в _traceResultStruct.Methods
        // И суммируем к общему времени
        private void CountTotalTime()
        {
            _traceResultStruct.Time = 0;
            foreach (var method in _traceResultStruct.Methods)
            {
                _traceResultStruct.Time += method.GetMethodStruct.Time;
            }
        }

        private string GetResultInJSON(TraceResultStruct result)
        {
            JsonTraceResultSerializer serializer = new JsonTraceResultSerializer();
            return serializer.Serialize(result);
        }

        private string GetResultInXML(TraceResultStruct result)
        {
            XMLTraceResultSerializer serializer = new XMLTraceResultSerializer();
            return serializer.Serialize(result);
        }

        public void StartTrace()
        {
            // Текущий метод становится предыдущим
            _prevMethod = _method;
            _method = new MethodElement();
            _method.parentMethod = _prevMethod;

            int prevMethodDepth = _prevMethod?.GetMethodStruct.MethodDepth ?? -1;
            _currentMethodDepth = _method.GetMethodStruct.MethodDepth;

            switch (prevMethodDepth)
            {
                //Если prevMethod null, то добавляем в Methods Tracer текущий метод
                case var depth when depth == -1:
                    _traceResultStruct.Methods.Add(_method);
                    break;
                //Если глубина prevMethod совпадает с текущей глубиной - 1, значит текущий метод нужно добавить в внутренние методы
                case var depth when depth == (_currentMethodDepth - 1):                                       
                    _prevMethod.GetMethodStruct.innerMethodStructList.Add(_method);
                    break;
                default:
                    _traceResultStruct.Methods.Add(_method);
                    break;
            }

            _method.StartStopwatch();
        }

        public void StopTrace()
        {
            _method.StopStopwatch();
            if (_prevMethod == _method)
            {
                if (_prevMethod.parentMethod != null)
                {
                    _method = _prevMethod.parentMethod;
                }
            }
            else
            {
                if (_method.GetMethodStruct.MethodDepth == 0)
                {
                    _prevMethod = _method;
                }
                else
                {
                    _method = _prevMethod;
                }
            }

        }
        // Добавление результатов в общую map, каждый поток вызывает по отдельности
        public TraceResultStruct GetTraceResult()
        {
            CountTotalTime();
            _traceMap.AddOrUpdate(_traceResultStruct.Id, _traceResultStruct, (key, existingValue) => _traceResultStruct);
            return _traceResultStruct;
        }

        // Вывод в файл и консоль, вызывается лишь главным потоком после всей работы
        public void GetThreadsResult(string filePath1, string filePath2)
        {
            string json = string.Empty;
            string xml = string.Empty;
            foreach(var thread in _traceMap)
            {
                json += GetResultInJSON(thread.Value) + ", ";
                xml += GetResultInXML(thread.Value) + '\n';
            }
            WriteConsoleResult("[\n" + json + "\n]", xml);
            WriteFileResult(filePath1, filePath2, "[\n" + json + "\n]", xml);
        }


        public void WriteConsoleResult(string resJSON,string resXML)
        {
            IResultWritable writer = new ConsoleResultWriter();
            writer.WriteResult(resJSON);
            writer.WriteResult(resXML);
        }

        public void WriteFileResult(string filePath1, string filePath2, string resJSON, string resXML)
        {
            IResultWritable writer = new FileResultWriter(filePath1);
            writer.WriteResult(resJSON);
            writer = new FileResultWriter(filePath2);
            writer.WriteResult(resXML);
        }


        public Tracer(int threadID)
        {
            _traceResultStruct.Id = threadID;
            _traceResultStruct.Methods = new List<MethodElement>();
            //Все потоки имеют один и тот же потокобезопасный словарь(static), соответственно объект нужно инициализировать только один
            //раз при первом создании Tracer
            if (_traceMap == null)
            {
                _traceMap = new ConcurrentDictionary<int, TraceResultStruct>();
            }
        }
    }
}
