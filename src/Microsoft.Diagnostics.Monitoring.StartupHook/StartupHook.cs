﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using System;
using System.IO;

internal sealed class StartupHook
{
    private static CurrentAppDomainExceptionProcessor? s_exceptionProcessor;
    private static AspNetHostingStartupLoader? s_hostingStartupLoader;

    public static void Initialize()
    {
        try
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            string? hostingStartupPath = Environment.GetEnvironmentVariable(StartupHookIdentifiers.EnvironmentVariables.HostingStartupPath);
            // TODO: Log if specified hosting startup assembly doesn't exist
            if (File.Exists(hostingStartupPath))
            {
                s_hostingStartupLoader = new AspNetHostingStartupLoader(hostingStartupPath);
            }

            s_exceptionProcessor = new CurrentAppDomainExceptionProcessor();
            s_exceptionProcessor.Start();

            try
            {
                // Check that the profiler is loaded before establishing the dispatcher, which has a dependency on the existance of the profiler
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion)))
                {
                    SharedInternals.MessageDispatcher = new MonitorMessageDispatcher(new ProfilerMessageSource());
                    ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging);
                }
            }
            catch
            {
            }

            ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.StartupHook);
        }
        catch
        {
            // TODO: Log failure
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        try
        {
            s_exceptionProcessor?.Dispose();
            s_hostingStartupLoader?.Dispose();
            SharedInternals.MessageDispatcher?.Dispose();
        }
        catch
        {

        }
    }
}
