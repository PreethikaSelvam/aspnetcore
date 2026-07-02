// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Components.Tests.Rendering;

public class ComponentStateLoggingTests
{
    [Fact]
    public void RenderIntoBatch_LogsWhenComponentDisposed()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        _ = componentState.DisposeAsync();

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        var batchBuilder = new RenderBatchBuilder();
        RenderFragment renderFragment = builder => { };

        componentState.RenderIntoBatch(batchBuilder, renderFragment, out var exception);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 9),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected SkippingRenderOnDisposedComponent (EventId 9) to be logged when rendering a disposed component");
    }

    [Fact]
    public void NotifyCascadingValueChanged_LogsWhenComponentDisposed()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        _ = componentState.DisposeAsync();

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        var lifetime = new ParameterViewLifetime();
        componentState.NotifyCascadingValueChanged(lifetime);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 7),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected SkippingCascadingUpdateOnDisposedComponent (EventId 7) to be logged when updating cascading values on disposed component");
    }

    [Fact]
    public void ComponentState_LogsComponentIdentityCorrectly()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentId = 42;
        var componentState = new ComponentState(mockRenderer, componentId, component, null);

        var capturedMessages = new List<string>();
        mockLogger
            .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                if (formatter != null)
                {
                    var message = formatter.DynamicInvoke(state, exception) as string;
                    if (message is not null)
                    {
                        capturedMessages.Add(message);
                    }
                }
            });

        _ = componentState.DisposeAsync();
        var batchBuilder = new RenderBatchBuilder();
        RenderFragment renderFragment = builder => { };
        componentState.RenderIntoBatch(batchBuilder, renderFragment, out var ex);

        var relevantMessages = capturedMessages
            .Where(msg => msg.Contains(componentId.ToString(System.Globalization.CultureInfo.InvariantCulture)) || msg.Contains("Skipping"))
            .ToList();

        Assert.NotEmpty(relevantMessages);
    }

    private class TestComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, "<div>Test</div>");
        }
    }

    private class MockRenderer : Renderer
    {
        private readonly ILogger _logger;

        public MockRenderer(ILogger logger) : base(
            new ServiceCollection()
                .AddSingleton(logger)
                .AddLogging(b => b.AddProvider(new MockLoggerProvider(logger)))
                .BuildServiceProvider(),
            new MockLoggerFactory(logger))
        {
            _logger = logger;
        }

        public override Dispatcher Dispatcher => new TestDispatcher();

        protected override void HandleException(Exception exception)
        {
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return Task.CompletedTask;
        }
    }

    private class MockLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _logger;

        public MockLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }

    private class TestDispatcher : Dispatcher
    {
        public override bool CheckAccess() => true;

        public override Task InvokeAsync(Action workItem)
        {
            workItem();
            return Task.CompletedTask;
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            return workItem();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            return Task.FromResult(workItem());
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            return workItem();
        }
    }

    private class MockLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public MockLoggerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() { }
    }
}
