using Microsoft.DotNet.Interactive.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenTestTask.Service
{
    class MethodInvokerObserver : IObserver<KernelEvent>
    {
        private readonly object _target;
        private readonly MethodInfo _methodInfo;

        public MethodInvokerObserver(object target, MethodInfo methodInfo)
        {
            _target = target;
            _methodInfo = methodInfo;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KernelEvent value)
        {
            // Invoke the method using reflection
            _methodInfo.Invoke(_target, new object[] { value });
        }
    }
}
