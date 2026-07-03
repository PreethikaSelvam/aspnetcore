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
    public void SetDirectParameters_LogsSupplyingCombinedParametersAtTrace()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Trace))
            .Returns(true);

        componentState.SetDirectParameters(ParameterView.Empty);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Trace,
                It.Is<EventId>(e => e.Id == 10),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce,
            "Expected SupplyingCombinedParameters (EventId 10) to be logged at Trace level when parameters are supplied");
    }

    [Fact]
    public void SetDirectParameters_WithSingleDeliveryParam_LogsStoppedSingleDeliveryAndSupplying()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);

        var supplier = new SingleDeliverySupplierComponent(isFixed: true);
        var supplierState = new ComponentState(mockRenderer, 1, supplier, null);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(mockRenderer, 2, consumer, supplierState);

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        consumerState.SetDirectParameters(ParameterView.Empty);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 8),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected StoppedSingleDeliveryCascadingParameters (EventId 8) to be logged when a single-delivery cascading parameter is consumed");

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Trace,
                It.Is<EventId>(e => e.Id == 10),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce,
            "Expected SupplyingCombinedParameters (EventId 10) to be logged after single-delivery teardown");
    }

    [Fact]
    public async Task FullLifecycle_LogsAllFourStateTransitionsInSequence()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);

        var supplier = new SingleDeliverySupplierComponent(isFixed: true);
        var supplierState = new ComponentState(mockRenderer, 1, supplier, null);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(mockRenderer, 2, consumer, supplierState);

        var batchBuilder = new RenderBatchBuilder();
        var capturedEventIds = new List<int>();

        mockLogger.Reset();
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
                capturedEventIds.Add(eventId.Id);
            });

        consumerState.SetDirectParameters(ParameterView.Empty);
        await consumerState.DisposeAsync();
        consumerState.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
        consumerState.RenderIntoBatch(batchBuilder, builder => { }, out _);

        Assert.Contains(7, capturedEventIds);
        Assert.Contains(8, capturedEventIds);
        Assert.Contains(9, capturedEventIds);
        Assert.Contains(10, capturedEventIds);

        var firstSkipIndex = Math.Min(capturedEventIds.IndexOf(7), capturedEventIds.IndexOf(9));
        var lastSupplyIndex = Math.Max(
            capturedEventIds.LastIndexOf(8),
            capturedEventIds.LastIndexOf(10));
        Assert.True(firstSkipIndex > lastSupplyIndex,
            $"Disposal-skip events (7, 9) should occur after supply events (8, 10). Actual order: [{string.Join(", ", capturedEventIds)}]");
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

    private sealed class TestSingleDeliveryAttribute : CascadingParameterAttributeBase
    {
        internal override bool SingleDelivery => true;
    }

    private class SingleDeliverySupplierComponent(bool isFixed) : ComponentBase, ICascadingValueSupplier
    {
        public bool IsFixed => isFixed;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => parameterInfo.Attribute is TestSingleDeliveryAttribute;

        public object GetCurrentValue(object key, in CascadingParameterInfo parameterInfo)
            => null;

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
        }

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
        }
    }

    private class SingleDeliveryConsumerComponent : IComponent
    {
        public RenderHandle RenderHandle { get; private set; }

        [TestSingleDelivery]
        public string CascadingValue { get; set; }

        public void Attach(RenderHandle renderHandle) => RenderHandle = renderHandle;
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
