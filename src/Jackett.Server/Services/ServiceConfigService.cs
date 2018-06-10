﻿using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Server.Services
{
    public class ServiceConfigService : IServiceConfigService
    {
        private const string NAME = "Jackett";
        private const string DESCRIPTION = "API Support for your favorite torrent trackers";
        private const string SERVICEEXE = "JackettService.exe";

        private IConfigurationService configService;
        private IProcessService processService;
        private Logger logger;

        public ServiceConfigService(IConfigurationService c, IProcessService p, Logger l)
        {
            configService = c;
            processService = p;
            logger = l;
        }

        public bool ServiceExists()
        {
            return GetService(NAME) != null;
        }

        public bool ServiceRunning()
        {
            var service = GetService(NAME);
            if (service == null)
                return false;
            return service.Status == ServiceControllerStatus.Running;
        }

        public void Start()
        {

            var service = GetService(NAME);
            service.Start();
        }

        public void Stop()
        {
            var service = GetService(NAME);
            service.Stop();
        }

        public ServiceController GetService(string serviceName)
        {
            return ServiceController.GetServices().FirstOrDefault(c => String.Equals(c.ServiceName, serviceName, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Install()
        {
            if (ServiceExists())
            {
                logger.Warn("The service is already installed!");
            }
            else
            {
                var exePath = Path.Combine(configService.ApplicationFolder(), SERVICEEXE);
                if (!File.Exists(exePath) && Debugger.IsAttached)
                {
                    exePath = Path.Combine(configService.ApplicationFolder(), "..\\..\\..\\Jackett.Service\\bin\\Debug", SERVICEEXE);
                }

                string arg = $"create {NAME} start= auto binpath= \"{exePath}\" DisplayName= {NAME}";

                processService.StartProcessAndLog("sc.exe", arg, true);

                processService.StartProcessAndLog("sc.exe", $"description {NAME} \"{DESCRIPTION}\"", true);
            }
        }

        public void Uninstall()
        {
            RemoveService();

            processService.StartProcessAndLog("sc.exe", $"delete {NAME}", true);

            logger.Info("The service was uninstalled.");
        }

        public void RemoveService()
        {
            var service = GetService(NAME);
            if(service == null)
            {
                logger.Warn("The service is already uninstalled");
                return;
            }
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));

                service.Refresh();
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    logger.Info("Service stopped.");
                }
                else
                {
                    logger.Error("Failed to stop the service");
                }
            }
            else
            {
                logger.Warn("The service was already stopped");
            }
        }
    }
}
