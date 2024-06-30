using Microsoft.UI.Xaml.Markup;
using Starward.Pages.GameLauncher;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Starward;


/// <summary>
/// <see href="https://github.com/gabor-budai/WinUI-DI-FrameNavigate""/>
/// </summary>
file class XamlMetadataProvider : IXamlMetadataProvider
{
    private readonly App _app;
    private readonly IXamlMetadataProvider _appProvider;
    private readonly Type _xamlUserType;
    private readonly Type _activatorDelegateType;
    private readonly Action<object, object> _setActivator;
    private readonly Func<object, object> _getUnderlyingType;


    public XamlMetadataProvider(App app)
    {
        _app = app;
        var appType = app.GetType();
        var ns = appType.Namespace!;
        var xamlTypeInfoNamespace = $"{ns}.{ns.Replace('.', '_')}_XamlTypeInfo";

        _xamlUserType = Type.GetType($"{xamlTypeInfoNamespace}.XamlUserType")!;
        _activatorDelegateType = Type.GetType($"{xamlTypeInfoNamespace}.Activator")!;
        Debug.Assert(!(_xamlUserType is null || _activatorDelegateType is null), "Something went wrong");
        _appProvider = (IXamlMetadataProvider)CreateGetter(appType, "_AppProvider")(app);
        _getUnderlyingType = CreateGetter(_xamlUserType, "UnderlyingType");
        _setActivator = CreateSetter(_xamlUserType, "Activator");
        Debug.Assert(!(_appProvider is null || _getUnderlyingType is null || _setActivator is null), "Something went wrong");
    }


    public IXamlType GetXamlType(Type type)
    {
        if (type == typeof(GenshinCloudLauncherPage))
        {
            var xamlType = _appProvider.GetXamlType(typeof(GameLauncherPage));
            var func = () => new GenshinCloudLauncherPage();
            _setActivator(xamlType, Delegate.CreateDelegate(_activatorDelegateType, func.Target, func.Method));
            return xamlType;
        }
        else
        {
            var xamlType = _appProvider.GetXamlType(type);
            return xamlType;
        }
    }

    public IXamlType GetXamlType(string fullName)
    {
        IXamlType xamlType = _appProvider.GetXamlType(fullName);
        return xamlType;
    }

    public XmlnsDefinition[] GetXmlnsDefinitions() => _appProvider.GetXmlnsDefinitions();

    private static Func<object, object> CreateGetter(Type type, string propertyName)
    {
        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var convertedInstance = Expression.Convert(instanceParameter, type);
        var property = Expression.Property(convertedInstance, propertyName);
        var convert = Expression.Convert(property, typeof(object));

        var lambda = Expression.Lambda<Func<object, object>>(convert, instanceParameter);
        return lambda.Compile();
    }

    private static Action<object, object> CreateSetter(Type type, string propertyName)
    {
        var propertyInfo = type.GetProperty(propertyName)!;
        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var valueParameter = Expression.Parameter(typeof(object), "value");

        var convertedInstance = Expression.Convert(instanceParameter, type);
        var convertedValue = Expression.Convert(valueParameter, propertyInfo.PropertyType);
        var property = Expression.Property(convertedInstance, propertyName);
        var assign = Expression.Assign(property, convertedValue);

        var lambda = Expression.Lambda<Action<object, object>>(assign, instanceParameter, valueParameter);
        return lambda.Compile();
    }
}

partial class App : IXamlMetadataProvider
{
    private IXamlMetadataProvider? __appUserProvider;
    private IXamlMetadataProvider _AppUserProvider => __appUserProvider ??= new XamlMetadataProvider(this);
    IXamlType IXamlMetadataProvider.GetXamlType(Type type) => _AppUserProvider.GetXamlType(type);
    IXamlType IXamlMetadataProvider.GetXamlType(string fullName) => _AppUserProvider.GetXamlType(fullName);
    // Not used to redirect types, but it's required, because the generator will fail if it's not overridden.
    XmlnsDefinition[] IXamlMetadataProvider.GetXmlnsDefinitions() => _AppUserProvider.GetXmlnsDefinitions();
}


