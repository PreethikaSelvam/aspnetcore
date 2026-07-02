// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Components.RenderTree;

public class RendererLogStateTransitionsTest
{
    [Fact]
    public void SkippingCascadingUpdateOnDisposedComponent_LogsWhenEnabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 42,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.SkippingCascadingUpdateOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 7),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void SkippingCascadingUpdateOnDisposedComponent_DoesNotLogWhenDisabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 42,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(false);

        Renderer.Log.SkippingCascadingUpdateOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void StoppedSingleDeliveryCascadingParameters_LogsWhenEnabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 5,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.StoppedSingleDeliveryCascadingParameters(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 8),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void StoppedSingleDeliveryCascadingParameters_DoesNotLogWhenDisabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 5,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(false);

        Renderer.Log.StoppedSingleDeliveryCascadingParameters(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void SkippingRenderOnDisposedComponent_LogsWhenEnabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 100,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.SkippingRenderOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 9),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void SkippingRenderOnDisposedComponent_DoesNotLogWhenDisabled()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 100,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(false);

        Renderer.Log.SkippingRenderOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void LogMethods_UseCorrectEventIds()
    {
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 1,
            component: component,
            parentComponentState: null);

        mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var capturedEventIds = new List<EventId>();

        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                capturedEventIds.Add(eventId);
            });

        Renderer.Log.SkippingCascadingUpdateOnDisposedComponent(mockLogger.Object, componentState);
        Renderer.Log.StoppedSingleDeliveryCascadingParameters(mockLogger.Object, componentState);
        Renderer.Log.SkippingRenderOnDisposedComponent(mockLogger.Object, componentState);

        Assert.Collection(capturedEventIds,
            eventId => Assert.Equal(7, eventId.Id),
            eventId => Assert.Equal(8, eventId.Id),
            eventId => Assert.Equal(9, eventId.Id));
    }

    [Fact]
    public void SkippingCascadingUpdateOnDisposedComponent_UsesEventId7()
    {
        // Explicitly verify EventId 7
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 42,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.SkippingCascadingUpdateOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 7),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void StoppedSingleDeliveryCascadingParameters_UsesEventId8()
    {
        // Explicitly verify EventId 8
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 5,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.StoppedSingleDeliveryCascadingParameters(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 8),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void SkippingRenderOnDisposedComponent_UsesEventId9()
    {
        // Explicitly verify EventId 9
        var mockLogger = new Mock<ILogger>();
        var component = new SimpleComponent();
        var componentState = new ComponentState(
            new MockRenderer(),
            componentId: 100,
            component: component,
            parentComponentState: null);

        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        Renderer.Log.SkippingRenderOnDisposedComponent(mockLogger.Object, componentState);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 9),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private class SimpleComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private class MockRenderer : Renderer
    {
        public MockRenderer() : base(
            new ServiceCollection()
                .AddLogging(builder => builder.AddDebug())
                .BuildServiceProvider(),
            new ServiceCollection()
                .AddLogging(builder => builder.AddDebug())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerFactory>())
        {
        }

        public override Dispatcher Dispatcher => throw new NotImplementedException();

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }
    }
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogging(
        this IServiceCollection services,
        Action<LoggingBuilder> configure)
    {
        services.AddSingleton<ILoggerFactory>(sp =>
        {
            var factory = new LoggerFactory();
            configure(new LoggingBuilder(services, factory));
            return factory;
        });
        return services;
    }
}

internal class LoggingBuilder : ILoggingBuilder
{
    private readonly IServiceCollection _services;
    private readonly ILoggerFactory _factory;

    public LoggingBuilder(IServiceCollection services, ILoggerFactory factory)
    {
        _services = services;
        _factory = factory;
    }

    public IServiceCollection Services => _services;

    public ILoggingBuilder AddDebug() => this;
}
