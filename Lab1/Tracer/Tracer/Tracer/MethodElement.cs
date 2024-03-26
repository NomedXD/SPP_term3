using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Tracer
{
    // Базовый элемент, т е измеряемый метод
    public class MethodElement
    {
        // Содержит в себе структуру с информацией
        private MethodStruct _methodStruct;
        // Часы для измерения времени работы метода
        private Stopwatch _stopwatch;

        [JsonProperty(PropertyName = "method properties")]
        public MethodStruct GetMethodStruct { get { return _methodStruct; } }

        [JsonIgnore]
        [XmlElement("methodProperties")]
        public MethodStruct MethodStructField
        {
            get { return _methodStruct; }
            set { _methodStruct = value; }
        }

        [JsonIgnore]
        [XmlIgnore]
        public MethodElement parentMethod;

        // Получение информации о вызывающем методе и имени класса
        private (string,string) GetCallingMethodNameAndClassName()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();
            int skipFrames = 2;

            // Получаем вызывающий метод, пропуская методы внутри библиотеки Tracer
            for (int i = skipFrames; i < stackFrames.Length; i++)
            {
                MethodBase method = stackFrames[i].GetMethod();
                // То есть ищем первый метод, который не часть Tracer
                if (method.DeclaringType != typeof(Tracer))
                {
                    return (method.Name,method.DeclaringType.Name);
                }
            }
            // Если вызывающий метод не найден
            return (string.Empty,string.Empty); 
        }

        // Начало отсчета
        public void StartStopwatch()
        {
            _stopwatch = Stopwatch.StartNew();
        }
        // Конец отсчета. Полное время выполнения записывается в _methodStruct
        public void StopStopwatch()
        {
            TimeSpan elapsedTime = _stopwatch.Elapsed;
            _methodStruct.Time = elapsedTime.TotalMilliseconds;
        }

        public MethodElement()
        {
            _methodStruct.innerMethodStructList= new List<MethodElement>();
            StackTrace stackTrace = new StackTrace();
            (string, string) res = GetCallingMethodNameAndClassName();

            _methodStruct.Name = res.Item1;
            _methodStruct.ClassName = res.Item2;
            _methodStruct.MethodDepth = stackTrace.FrameCount - 4;
        }
    }
}
