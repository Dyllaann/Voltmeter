﻿using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Voltmeter.Ports.Discovery;
using Voltmeter.Ports.Storage;

namespace Voltmeter.UseCases
{
    public class RefreshEnvironmentStatusUseCase
    {
        private readonly IEnvironmentStatusStore _store;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly RefreshServiceStatusUseCase _refreshServiceUseCase;
        private readonly ILogger _logger;

        public RefreshEnvironmentStatusUseCase(
            IEnvironmentStatusStore store,
            ILogger logger, 
            IServiceDiscovery serviceDiscovery, 
            RefreshServiceStatusUseCase refreshServiceUseCase)
        {
            _store = store;
            _logger = logger;
            _serviceDiscovery = serviceDiscovery;
            _refreshServiceUseCase = refreshServiceUseCase;
        }

        public void Refresh(Environment environment)
        {
            if (environment == null || string.IsNullOrWhiteSpace(environment.Name))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            try
            {
                var services = _serviceDiscovery.DiscoverServicesIn(environment);

                var results = new List<ApplicationStatus>();

                foreach (var service in services)
                {
                    _refreshServiceUseCase.Refresh(service);
                    results.Add(new ApplicationStatus());
                }

                if (results.Any())
                {
                    _store.Update(environment, results);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not get status of {Environment}", environment);
                // Swallow
            }
        }
    }
}