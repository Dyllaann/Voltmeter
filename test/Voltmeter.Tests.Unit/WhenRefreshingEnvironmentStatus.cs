﻿using System;
using FluentAssertions;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;
using Voltmeter.Ports.Providers;
using Voltmeter.Ports.Storage;
using Voltmeter.UseCases;
using Xunit;

namespace Voltmeter.Tests.Unit
{
    public class WhenRefreshingEnvironmentStatus
    {
        private const string EnvironmentName = "some environment";
        private readonly RefreshEnvironmentStatusUseCase _useCase;
        private readonly Mock<IEnvironmentStatusProvider> _providerMock;
        private readonly Mock<IEnvironmentStatusStore> _retrieverMock;

        public WhenRefreshingEnvironmentStatus()
        {
            _providerMock = new Mock<IEnvironmentStatusProvider>();
            _retrieverMock = new Mock<IEnvironmentStatusStore>();

            var logger = new LoggerConfiguration()
                .WriteTo.InMemory()
                .CreateLogger();

            _useCase = new RefreshEnvironmentStatusUseCase(_providerMock.Object, _retrieverMock.Object, logger);
        }

        [Fact]
        public void GivenEnvironmentNameIsEmpty_ArgumentNullExceptionIsThrown()
        {
            Action action = () => _useCase.Refresh(null);

            action
                .Should()
                .Throw<ArgumentNullException>();
        }

        [Fact]
        public void GivenProviderReturnsEmptyResultSet_RetrieverIsNotUpdated()
        {
            _providerMock
                .Setup(p => p.ProvideFor(It.Is<Environment>(e => e.Name == EnvironmentName)))
                .Returns(new ApplicationStatus[0]);

            WhenRefreshing();

            _retrieverMock
                .Verify(
                    r => r.Update(It.Is<Environment>(e => e.Name == EnvironmentName), It.IsAny<ApplicationStatus[]>()),
                    Times.Never);
        }

        [Fact]
        public void GivenProviderRThrowsException_RetrieverIsNotUpdated()
        {
            _providerMock
                .Setup(p => p.ProvideFor(It.Is<Environment>(e => e.Name == EnvironmentName)))
                .Throws(new Exception("BANG!"));
                
            WhenRefreshing();

            _retrieverMock
                .Verify(
                    r => r.Update(It.Is<Environment>(e => e.Name == EnvironmentName), It.IsAny<ApplicationStatus[]>()),
                    Times.Never);
        }

        [Fact]
        public void GivenProviderRThrowsException_ErrorIsLogged()
        {
            _providerMock
                .Setup(p => p.ProvideFor(It.Is<Environment>(e => e.Name == EnvironmentName)))
                .Throws(new Exception("BANG!"));

            WhenRefreshing();

            InMemorySink
                .Instance
                .Should()
                .HaveMessage("Could not get status of {Environment}")
                .Appearing().Once()
                .WithLevel(LogEventLevel.Error)
                .WithProperty("Environment")
                .WithValue(new Environment().ToString());
        }

        [Fact]
        public void GivenProviderReturnsResults_RetrieverIsUpdated()
        {
            _providerMock
                .Setup(p => p.ProvideFor(It.Is<Environment>(e => e.Name == EnvironmentName)))
                .Returns(new [] { new ApplicationStatus() });

            WhenRefreshing();

            _retrieverMock
                .Verify(
                    r => r.Update(It.Is<Environment>(e => e.Name == EnvironmentName), It.IsAny<ApplicationStatus[]>()),
                    Times.Once);
        }

        private void WhenRefreshing()
        {
            _useCase.Refresh(new Environment { Name = EnvironmentName });
        }
    }
}