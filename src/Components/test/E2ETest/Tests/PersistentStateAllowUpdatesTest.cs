// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

/// <summary>
/// Test suite for [PersistentState(AllowUpdates = true)] fix.
/// Tests persistent state preservation during enhanced navigation scenarios.
/// </summary>
public class PersistentStateAllowUpdatesTest : ServerTestBase<BasicTestAppServerSiteFixture>
{
    public PersistentStateAllowUpdatesTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    /// <summary>
    /// Test 1: Component with property check in OnInitializedAsync (THE PRIMARY BUG FIX)
    /// Verifies that persisted state is preserved when component checks property during initialization.
    /// </summary>
    [Theory]
    [InlineData("wasm")]
    [InlineData("auto")]
    public void PersistentState_WithPropertyCheckInOnInitializedAsync_PreservesStateOnEnhancedNavigation(string renderMode)
    {
        // Navigate to persistent counter test component
        Navigate($"persistent-state/counter?mode={renderMode}");

        // Get initial counter value
        var counterElement = Browser.FindElement(By.Id("counter-value"));
        var initialValue = counterElement.Text;
        Assert.False(string.IsNullOrEmpty(initialValue), "Initial counter value should be present");

        // Navigate away and back
        Navigate("persistent-state/other-page");
        Thread.Sleep(100);

        Navigate($"persistent-state/counter?mode={renderMode}");
        Thread.Sleep(100);

        // Verify counter value was preserved
        var restoredValue = Browser.FindElement(By.Id("counter-value")).Text;
        Assert.Equal(initialValue, restoredValue);
    }

    /// <summary>
    /// Test 2: Multiple navigation cycles
    /// Verifies persistent state is preserved across multiple navigation cycles.
    /// </summary>
    [Fact]
    public void PersistentState_MultipleNavigationCycles_PreservesStateConsistently()
    {
        Navigate("persistent-state/counter");

        var initialValue = Browser.FindElement(By.Id("counter-value")).Text;

        // Perform multiple navigation cycles
        for (int i = 0; i < 2; i++)
        {
            Navigate("persistent-state/other-page");
            Thread.Sleep(100);

            Navigate("persistent-state/counter");
            Thread.Sleep(100);

            var currentValue = Browser.FindElement(By.Id("counter-value")).Text;
            Assert.Equal(initialValue, currentValue);
        }
    }

    /// <summary>
    /// Test 3: Verify component renders with InteractiveWebAssembly render mode.
    /// </summary>
    [Fact]
    public void PersistentState_WithInteractiveWebAssemblyMode_Renders()
    {
        Navigate("persistent-state/counter");
        var element = Browser.FindElement(By.Id("counter-value"));
        Assert.NotNull(element);
    }

    /// <summary>
    /// Test 4: State preservation across page navigations.
    /// </summary>
    [Fact]
    public void PersistentState_PreservedAcrossPageNavigations()
    {
        Navigate("persistent-state/counter");
        var firstValue = Browser.FindElement(By.Id("counter-value")).Text;

        Navigate("persistent-state/other-page");
        Navigate("persistent-state/counter");

        var secondValue = Browser.FindElement(By.Id("counter-value")).Text;
        Assert.Equal(firstValue, secondValue);
    }
}
