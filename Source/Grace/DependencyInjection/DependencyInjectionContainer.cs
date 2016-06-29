﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Grace.Data.Immutable;
using Grace.DependencyInjection.Impl;
using Grace.Diagnostics;
using JetBrains.Annotations;

namespace Grace.DependencyInjection
{
    /// <summary>
    /// This is the standard IDependencyInjectionContainer implementation
    /// </summary>
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    [DebuggerTypeProxy(typeof(DependencyInjectionContainerDiagnostic))]
    public class DependencyInjectionContainer : IDependencyInjectionContainer, IMissingExportHandler, IEnumerable<IExportStrategy>
    {
        private readonly InjectionKernelManager _injectionKernelManager;
        protected bool _disposed;
        protected ImmutableArray<IMissingExportStrategyProvider> _missingExportStrategyProviders =
            ImmutableArray<IMissingExportStrategyProvider>.Empty;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DependencyInjectionContainer(IKernelConfiguration configuration = null)
        {
            if(configuration == null)
            {
                configuration = new KernelConfiguration();
            }

            _injectionKernelManager = new InjectionKernelManager(this, configuration.Comparer);

            AutoRegisterUnknown = true;
            ThrowExceptions = true;

            RootScope = new InjectionKernel(_injectionKernelManager, null, "RootScope", configuration);
        }

        /// <summary>
        /// If a concrete type is requested and it is not registered, an export strategy will be created.
        /// Note: It will be scanned for attributes
        /// </summary>
        public bool AutoRegisterUnknown { get; set; }

        /// <summary>
        /// If true exception will be thrown if a type can't be located, otherwise it will be caught and errors logged
        /// True by default
        /// </summary>
        public bool ThrowExceptions { get; set; }

        /// <summary>
        /// The root scope for the container
        /// </summary>
        [NotNull]
        public IInjectionScope RootScope { get; private set; }
        
        /// <summary>
        /// Name of scope
        /// </summary>
        public string ScopeName
        {
            get { return RootScope.ScopeName; }
        }

        /// <summary>
        /// Unique Scope Id
        /// </summary>
        public Guid ScopeId
        {
            get { return RootScope.ScopeId; }
        }

        /// <summary>
        /// Adds a secondary resolver to the container.
        /// </summary>
        /// <param name="newLocator">new secondary locator</param>
        public void AddSecondaryLocator(ISecondaryExportLocator newLocator)
        {
            RootScope.AddSecondaryLocator(newLocator);
        }

        /// <summary>
        /// List of Export Locators
        /// </summary>
        public IEnumerable<ISecondaryExportLocator> SecondaryExportLocators
        {
            get { return RootScope.SecondaryExportLocators; }
        }

        /// <summary>
        /// Add a strategy 
        /// </summary>
        /// <param name="inspector">strategy inspector</param>
        public void AddStrategyInspector(IExportStrategyInspector inspector)
        {
            RootScope.AddStrategyInspector(inspector);
        }

        /// <summary>
        /// Adds a configuration module
        /// </summary>
        /// <param name="module"></param>
        public void Add(IConfigurationModule module)
        {
            Configure(module);
        }

        /// <summary>
        /// Adds registration block
        /// </summary>
        /// <param name="registrationAction"></param>
        public void Add(ExportRegistrationDelegate registrationAction)
        {
            Configure(registrationAction);
        }

        /// <summary>
        /// This method can be used to configure the root scope of the container
        /// </summary>
        /// <param name="registrationDelegate">configuration delegate</param>
        public void Configure(ExportRegistrationDelegate registrationDelegate)
        {
            RootScope.Configure(registrationDelegate);
        }

        /// <summary>
        /// This method can be used to configure a particular scope in the container
        /// </summary>
        /// <param name="configurationModule"></param>
        /// <param name="scopeName"></param>
        public void Configure(string scopeName, IConfigurationModule configurationModule)
        {
            _injectionKernelManager.Configure(scopeName, configurationModule.Configure);
        }

        /// <summary>
        /// Missing export strategy providers can provide a set of exports that can be used to resolve a satisfy an import
        /// </summary>
        /// <param name="exportStrategyProvider">export strategy provider</param>
        public void AddMissingExportStrategyProvider(IMissingExportStrategyProvider exportStrategyProvider)
        {
            RootScope.AddMissingExportStrategyProvider(exportStrategyProvider);
        }
        
        /// <summary>
        /// Add an object for disposal 
        /// </summary>
        /// <param name="disposable"></param>
        /// <param name="cleanupDelegate">logic that will be run directly before the object is disposed</param>
        public void AddDisposable(IDisposable disposable, BeforeDisposalCleanupDelegate cleanupDelegate = null)
        {
            RootScope.AddDisposable(disposable, cleanupDelegate);
        }

        /// <summary>
        /// Remove an object from the disposal scope
        /// </summary>
        /// <param name="disposable"></param>
        public void RemoveDisposable(IDisposable disposable)
        {
            RootScope.RemoveDisposable(disposable);
        }

        /// <summary>
        /// Creates a child scope from this scope
        /// </summary>
        /// <param name="scopeName">name of the scope you want to create</param>
        /// <param name="registrationDelegate"></param>
        /// <param name="disposalScopeProvider"></param>
        /// <returns></returns>
        public IInjectionScope CreateChildScope(ExportRegistrationDelegate registrationDelegate = null,
            string scopeName = null,
            IDisposalScopeProvider disposalScopeProvider = null)
        {
            return RootScope.CreateChildScope(registrationDelegate, scopeName, disposalScopeProvider);
        }

        /// <summary>
        /// Creates a child scope from this scope using a configuration module
        /// </summary>
        /// <param name="scopeName">name of the scope you want to create</param>
        /// <param name="configurationModule"></param>
        /// <param name="disposalScopeProvider"></param>
        /// <returns></returns>
        public IInjectionScope CreateChildScope(IConfigurationModule configurationModule,
            string scopeName = null,
            IDisposalScopeProvider disposalScopeProvider = null)
        {
            return RootScope.CreateChildScope(configurationModule, scopeName, disposalScopeProvider);
        }

        /// <summary>
        /// Create an injection context
        /// </summary>
        /// <returns></returns>
        public IInjectionContext CreateContext(IDisposalScope disposalScope = null)
        {
            return RootScope.CreateContext(disposalScope);
        }

        /// <summary>
        /// This method can be used to configure the root scope of the container
        /// </summary>
        /// <param name="configurationModule"></param>
        public void Configure(IConfigurationModule configurationModule)
        {
            RootScope.Configure(configurationModule);
        }

        /// <summary>
        /// This method can be used to configure a particular scope in the container
        /// </summary>
        /// <param name="scopeName">the name of the scope that is being configured</param>
        /// <param name="registrationDelegate">configuration delegate</param>
        public void Configure(string scopeName, ExportRegistrationDelegate registrationDelegate)
        {
            _injectionKernelManager.Configure(scopeName, registrationDelegate);
        }

        /// <summary>
        /// By handling this event you can provide a value when no export was found
        /// </summary>
        public event EventHandler<ResolveUnknownExportArgs> ResolveUnknownExports;

        /// <summary>
        /// Locate an export by type
        /// </summary>
        /// <param name="injectionContext">injection context for the locate</param>
        /// <param name="consider">filter to be used when locating</param>
        /// <param name="withKey"></param>
        /// <typeparam name="T">type to locate</typeparam>
        /// <returns>export T if found, other wise default(T)</returns>
        public T Locate<T>(IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null)
        {
            return RootScope.Locate<T>(injectionContext, consider, withKey);
        }

        /// <summary>
        /// Locate an object by type
        /// </summary>
        /// <param name="objectType">type to locate</param>
        /// <param name="injectionContext">injection context to use while locating</param>
        /// <param name="consider">filter to use while locating export</param>
        /// <param name="withKey"></param>
        /// <returns>export object if found, other wise null</returns>
        public object Locate(Type objectType, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null)
        {
            return RootScope.Locate(objectType, injectionContext, consider, withKey);
        }

        /// <summary>
        /// Locate an export by name
        /// </summary>
        /// <param name="exportName">name of export to locate</param>
        /// <param name="injectionContext">injection context to use while locating</param>
        /// <param name="consider">filter to use while locating</param>
        /// <param name="withKey"></param>
        /// <returns>export object if found, other wise null</returns>
        public object Locate(string exportName, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null)
        {
            return RootScope.Locate(exportName, injectionContext, consider, withKey);
        }

        /// <summary>
        /// Locate all export of type T
        /// </summary>
        /// <param name="injectionContext">injection context to use while locating</param>
        /// <param name="consider">filter to use while locating</param>
        /// <param name="withKey"></param>
        /// <param name="comparer"></param>
        /// <typeparam name="T">type to locate</typeparam>
        /// <returns>List of T, this will return an empty list if not exports are found</returns>
        public List<T> LocateAll<T>(IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null, IComparer<T> comparer = null)
        {
            return RootScope.LocateAll(injectionContext, consider, withKey, comparer);
        }

        /// <summary>
        /// Locate All exports by the name provided
        /// </summary>
        /// <param name="name">export name to locate</param>
        /// <param name="injectionContext">injection context to use while locating</param>
        /// <param name="consider">filter to use while locating</param>
        /// <param name="withKey"></param>
        /// <param name="comparer"></param>
        /// <returns>List of objects, this will return an empty list if no exports are found</returns>
        public List<object> LocateAll(string name, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null, IComparer<object> comparer = null)
        {
            return RootScope.LocateAll(name, injectionContext, consider, withKey, comparer);
        }

        /// <summary>
        /// Locate all exports by type
        /// </summary>
        /// <param name="exportType">type to locate</param>
        /// <param name="injectionContext">injection context</param>
        /// <param name="consider">filter to use while locating</param>
        /// <param name="withKey"></param>
        /// <param name="comparer"></param>
        /// <returns>list of object, this will return an empty list if no exports are found</returns>
        public List<object> LocateAll(Type exportType, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null, IComparer<object> comparer = null)
        {
            return RootScope.LocateAll(exportType, injectionContext, consider, withKey, comparer);
        }
        
        /// <summary>
        /// Returns a list of all known strategies.
        /// </summary>
        /// <param name="exportFilter"></param>
        /// <returns>returns all known strategies</returns>
        public IEnumerable<IExportStrategy> GetAllStrategies(ExportStrategyFilter exportFilter = null)
        {
            return RootScope.GetAllStrategies(exportFilter);
        }

        /// <summary>
        /// Finds the best matching strategy exported by the name provided
        /// </summary>
        /// <param name="name"></param>
        /// <param name="injectionContext"></param>
        /// <param name="exportFilter"></param>
        /// <param name="withKey"></param>
        /// <returns></returns>
        public IExportStrategy GetStrategy(string name, IInjectionContext injectionContext = null, ExportStrategyFilter exportFilter = null, object withKey = null)
        {
            return RootScope.GetStrategy(name, injectionContext, exportFilter, withKey);
        }

        /// <summary>
        /// Finds the best matching strategy exported by the name provided
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="injectionContext"></param>
        /// <param name="exportFilter"></param>
        /// <param name="withKey"></param>
        /// <returns></returns>
        public IExportStrategy GetStrategy(Type exportType, IInjectionContext injectionContext = null, ExportStrategyFilter exportFilter = null, object withKey = null)
        {
            return RootScope.GetStrategy(exportType, injectionContext, exportFilter, withKey);
        }

        /// <summary>
        /// Get the list of exported strategies sorted by best option.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="injectionContext"></param>
        /// <param name="exportFilter"></param>
        /// <returns></returns>
        public IEnumerable<IExportStrategy> GetStrategies(string name,
            IInjectionContext injectionContext = null,
            ExportStrategyFilter exportFilter = null)
        {
            return RootScope.GetStrategies(name, injectionContext, exportFilter);
        }

        /// <summary>
        /// Get the list of exported strategies sorted by best option.
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="injectionContext"></param>
        /// <param name="exportFilter"></param>
        /// <returns></returns>
        public IEnumerable<IExportStrategy> GetStrategies(Type exportType,
            IInjectionContext injectionContext = null,
            ExportStrategyFilter exportFilter = null)
        {
            return RootScope.GetStrategies(exportType, injectionContext, exportFilter);
        }

        /// <summary>
        /// Get the export strategy collection
        /// </summary>
        /// <param name="exportType"></param>
        /// <param name="createIfDoesntExist"></param>
        /// <returns>can be null if nothing is registered by that name</returns>
        public IExportStrategyCollection GetStrategyCollection(Type exportType, bool createIfDoesntExist = true)
        {
            return RootScope.GetStrategyCollection(exportType, createIfDoesntExist);
        }

        /// <summary>
        /// Get the export collection by name
        /// </summary>
        /// <param name="exportName">export name</param>
        /// <param name="createIfDoesntExist"></param>
        /// <returns></returns>
        public IExportStrategyCollection GetStrategyCollection(string exportName, bool createIfDoesntExist = true)
        {
            return RootScope.GetStrategyCollection(exportName, createIfDoesntExist);
        }

        /// <summary>
        /// Adds a new strategy to the container
        /// </summary>
        /// <param name="addStrategy"></param>
        public void AddStrategy(IExportStrategy addStrategy)
        {
            RootScope.AddStrategy(addStrategy);
        }

        /// <summary>
        /// Allows the caller to remove a strategy from the container
        /// </summary>
        /// <param name="knownStrategy">strategy to remove</param>
        public void RemoveStrategy(IExportStrategy knownStrategy)
        {
            RootScope.RemoveStrategy(knownStrategy);
        }

        /// <summary>
        /// Inject dependencies into a constructed object
        /// </summary>
        /// <param name="injectedObject">object to be injected</param>
        /// <param name="injectionContext">injection context</param>
        public void Inject(object injectedObject, IInjectionContext injectionContext = null)
        {
            RootScope.Inject(injectedObject, injectionContext);
        }

        /// <summary>
        /// List of Injection Inspectors for the scope
        /// </summary>
        public IEnumerable<IExportStrategyInspector> Inspectors
        {
            get { return RootScope.Inspectors; }
        }

        /// <summary>
        /// Dispose of the container
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Locate missing exports
        /// </summary>
        /// <param name="context">injection context</param>
        /// <param name="exportName">export name</param>
        /// <param name="exportType">export type</param>
        /// <param name="consider">export filter</param>
        /// <param name="locateKey"></param>
        /// <returns>export object</returns>
        public object LocateMissingExport(IInjectionContext context, string exportName, Type exportType, ExportStrategyFilter consider, object locateKey)
        {
            object returnValue = null;

            foreach (IMissingExportStrategyProvider missingExportStrategyProvider in _missingExportStrategyProviders)
            {
                var exports = missingExportStrategyProvider.ProvideExports(context,
                    exportName,
                    exportType,
                    consider,
                    locateKey);

                foreach (IExportStrategy exportStrategy in exports)
                {
                    RootScope.AddStrategy(exportStrategy);

                }

                IExportStrategy strategy = exportType != null ?
                                           RootScope.GetStrategy(exportType, context, consider, locateKey) :
                                           RootScope.GetStrategy(exportName, context, consider, locateKey);

                if (strategy != null)
                {
                    returnValue = strategy.Activate(RootScope, context, consider, locateKey);
                }
            }

            if (returnValue == null)
            {
                EventHandler<ResolveUnknownExportArgs> missingExportEvent = ResolveUnknownExports;

                if (missingExportEvent != null)
                {
                    ResolveUnknownExportArgs eventArgs = new ResolveUnknownExportArgs(context,
                        exportName,
                        exportType,
                        locateKey);

                    missingExportEvent(this, eventArgs);

                    return eventArgs.ExportedValue;
                }
            }

            return null;
        }

        /// <summary>
        /// This method compares 2 export strategies by class name
        /// </summary>
        /// <param name="x">x compare object</param>
        /// <param name="y">y compare object</param>
        /// <returns>compare value</returns>
        public static int CompareExportStrategiesByName(IExportStrategy x, IExportStrategy y)
        {
            return string.Compare(x.ActivationType.Name, y.ActivationType.Name, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// dispose implementation
        /// </summary>
        /// <param name="dispose"></param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            if (RootScope != null)
            {
                RootScope.Dispose();
            }
        }

        // ReSharper disable once UnusedMember.Local
        private string DebugDisplayString
        {
            get { return "Exports: " + GetAllStrategies().Count(); }
        }

        public IEnumerable<IInjectionValueProviderInspector> InjectionInspectors
        {
            get
            {
                return RootScope.InjectionInspectors;
            }
        }

        public IEnumerator<IExportStrategy> GetEnumerator()
        {
            return new List<IExportStrategy>(GetAllStrategies()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryLocate<T>(out T value, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null)
        {
            return RootScope.TryLocate(out value, injectionContext, consider, withKey);
        }

        public bool TryLocate(Type type, out object value, IInjectionContext injectionContext = null, ExportStrategyFilter consider = null, object withKey = null)
        {
            return RootScope.TryLocate(type, out value, injectionContext, consider, withKey);
        }

        public void AddInjectionValueProviderInspector([NotNull] IInjectionValueProviderInspector inspector)
        {
            RootScope.AddInjectionValueProviderInspector(inspector);
        }
    }
}