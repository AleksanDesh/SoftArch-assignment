using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// This is an example implementation of the Service Locator pattern, it registers/unregisters
/// services with a dictionary with the types of the services as keys. The pattern is very
/// straightforward: the service locator is a proxy between the service user and the services so
/// that the service users don't directly call the services, so that your application and the
/// implementation of the services are decoupled.
/// This pattern is globally accecible, and, like the name indicates, is suitable
/// to implement global services in your game, e.g. micro-transaction shop, achievement system,
/// save load system, etc.
/// This pattern is not suitable for modules like HUD manager, enemy manager, etc because they are
/// not global modules.
/// </summary>
public class ServiceLocator 
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void RegisterService<T>(T service)
    {
        if (!services.ContainsKey(typeof(T))){
            services[typeof(T)] = service;
        }
        else
        {
            throw new System.Exception("Service type: " + typeof(T).ToString() +
                " was already registered, unregister it first.");
        }
    }

    public static void UnregisterService<T>(T service)
    {
        if (services.ContainsKey(typeof(T)))
        {
            services.Remove(typeof(T));
        }
    }

    public static T GetService<T>()
    {
        try
        {
            return (T)services[typeof(T)];
        }
        catch
        {
            throw new System.Exception("Service type: " + typeof(T).ToString() +
                " not found, register it first");
        }
    }
}
