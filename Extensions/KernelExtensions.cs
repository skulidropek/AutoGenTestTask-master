using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenTestTask.Extensions
{
    public static class KernelExtensions
    {
        public static void SetUpValueSharingIfSupported(this ProxyKernel proxyKernel)
        {
            var supportedCommands = proxyKernel.KernelInfo.SupportedKernelCommands;
            if (supportedCommands.Any(d => d.Name == nameof(RequestValue)) &&
                supportedCommands.Any(d => d.Name == nameof(SendValue)))
            {
                proxyKernel.UseValueSharing();
            }
        }
    }
}
