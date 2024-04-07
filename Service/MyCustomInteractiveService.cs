using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoGenTestTask.Extensions;
using AutoGen.DotnetInteractive;
using System.Runtime.ExceptionServices;

namespace AutoGenTestTask.Service
{

    /// <summary>
    /// Это костыль. У AutoGen ошибка в библиотеке и надо убрать вызов команды "dotnet", "tool", "run",
    /// </summary>
    public class MyCustomInteractiveService : InteractiveService
    {
        public MyCustomInteractiveService(string installingDirectory) : base(installingDirectory)
        {
        }

        public async Task<bool> StartAsync(string workingDirectory, CancellationToken ct = default)
        {
            // После выполнения StartAsync, меняем kernel используя рефлексию
            // Получаем тип InteractiveService
            Type serviceType = typeof(InteractiveService);

            // Используем рефлексию для доступа к приватному полю kernel
            FieldInfo kernelField = serviceType.GetField("kernel", BindingFlags.NonPublic | BindingFlags.Instance);
            if (kernelField != null)
            {
                // Создаём новый экземпляр Kernel или получаем его каким-то образом
                Kernel newKernel = await CreateKernelAsync(workingDirectory, ct);

                // Устанавливаем новое значение для поля kernel
                kernelField.SetValue(this, newKernel);
            }
            else
            {
                Console.WriteLine("Не удалось найти поле 'kernel' для изменения.");
            }
            return true;
        }

        private async Task<Kernel> CreateKernelAsync(string workingDirectory, CancellationToken ct = default)
        {
            try
            {
                var url = KernelHost.CreateHostUriForCurrentProcessId();
                var compositeKernel = new CompositeKernel("cbcomposite");
                var cmd = new string[]
                {
                    //"dotnet",
                    //"tool",
                    //"run",
                    "dotnet-interactive",
                    $"[cb-{Process.GetCurrentProcess().Id}]",
                    "stdio",
                    //"--default-kernel",
                    //"csharp",
                    "--working-dir",
                    $@"""{workingDirectory}""",
                };
                var connector = new StdIoKernelConnector(
                    cmd,
                    "root-proxy",
                    url,
                    new DirectoryInfo(workingDirectory));

                // Start the dotnet-interactive tool and get a proxy for the root composite kernel therein.
                using var rootProxyKernel = await connector.CreateRootProxyKernelAsync().ConfigureAwait(false);

                // Get proxies for each subkernel present inside the dotnet-interactive tool.
                var requestKernelInfoCommand = new RequestKernelInfo(rootProxyKernel.KernelInfo.RemoteUri);
                var result =
                    await rootProxyKernel.SendAsync(
                        requestKernelInfoCommand,
                        ct).ConfigureAwait(false);

                var subKernels = result.Events.OfType<KernelInfoProduced>();

                foreach (var kernelInfoProduced in result.Events.OfType<KernelInfoProduced>())
                {
                    var kernelInfo = kernelInfoProduced.KernelInfo;
                    if (kernelInfo is not null && !kernelInfo.IsProxy && !kernelInfo.IsComposite)
                    {
                        var proxyKernel = await connector.CreateProxyKernelAsync(kernelInfo).ConfigureAwait(false);
                        proxyKernel.SetUpValueSharingIfSupported();
                        compositeKernel.Add(proxyKernel);
                    }
                }

                compositeKernel.Add(rootProxyKernel);

                SubscribeToPrivateMethodWithReflection(compositeKernel);

                return compositeKernel;
            }
            catch (CommandLineInvocationException ex) when (ex.Message.Contains("Cannot find a tool in the manifest file that has a command named 'dotnet-interactive'"))
            {
                var success = CallRestoreDotnetInteractive();

                if (success)
                {
                    return await CreateKernelAsync(workingDirectory, ct);
                }

                throw;
            }
        }

        private bool CallRestoreDotnetInteractive()
        {

            // Получаем MethodInfo приватного метода RestoreDotnetInteractive
            MethodInfo restoreDotnetInteractiveMethod = typeof(InteractiveService).GetMethod("RestoreDotnetInteractive", BindingFlags.NonPublic | BindingFlags.Instance);

            if (restoreDotnetInteractiveMethod != null)
            {
                // Вызываем метод с помощью рефлексии
                // Так как метод возвращает значение bool, мы можем его получить
                return (bool)restoreDotnetInteractiveMethod.Invoke(this, null);
            }

            return false;
        }

        private void SubscribeToPrivateMethodWithReflection(CompositeKernel compositeKernel)
        {
            // Reflect the private method
            MethodInfo onKernelDiagnosticEventReceivedMethod = typeof(InteractiveService).GetMethod("OnKernelDiagnosticEventReceived", BindingFlags.NonPublic | BindingFlags.Instance);

            if (onKernelDiagnosticEventReceivedMethod != null)
            {
                // Create an observer that can invoke the reflected method
                var observer = new MethodInvokerObserver(this, onKernelDiagnosticEventReceivedMethod);
                // Subscribe the observer to kernel events
                compositeKernel.KernelEvents.Subscribe(observer);
            }
            else
            {
                Console.WriteLine("Method 'OnKernelDiagnosticEventReceived' not found.");
            }
        }
    }
}
