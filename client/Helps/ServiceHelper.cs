using System;
using Microsoft.Maui;

namespace client.Helps;

public static class ServiceHelper
{
    private static IServiceProvider? _services;
    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("ServiceHelper not initialized.");

    internal static void Initialize(IServiceProvider services) => _services = services;
}
