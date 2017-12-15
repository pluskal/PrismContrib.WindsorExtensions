﻿using System;
using System.Globalization;
using Microsoft.Practices.ServiceLocation;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using CommonServiceLocator;

using CommonServiceLocator.WindsorAdapter;
using CommonServiceLocator.WindsorAdapter.Unofficial;
using WindsorServiceLocator = CommonServiceLocator.WindsorAdapter.Unofficial.WindsorServiceLocator;

namespace PrismContrib.WindsorExtensions
{
    using Prism;
    using Prism.Events;
    using Prism.Logging;
    using Prism.Modularity;
    using Prism.Regions;
    using Prism.Regions.Behaviors;

    /// <summary>
    /// Base class that provides a basic bootstrapping sequence that
    /// registers most of the Composite Application Library assets
    /// in a <see cref="IWindsorContainer"/>.
    /// </summary>
    /// <remarks>
    /// This class must be overriden to provide application specific configuration.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public abstract class WindsorBootstrapper : Bootstrapper
    {
        private bool useDefaultConfiguration = true;

        /// <summary>
        /// Gets the default <see cref="IWindsorContainer"/> for the application.
        /// </summary>
        /// <value>The default <see cref="IWindsorContainer"/> instance.</value>
        [CLSCompliant(false)]
        public IWindsorContainer Container { get; protected set; }


        /// <summary>
        /// Run the bootstrapper process.
        /// </summary>
        /// <param name="runWithDefaultConfiguration">If <see langword="true"/>, registers default Composite Application Library services in the container. This is the default behavior.</param>
        public override void Run(bool runWithDefaultConfiguration)
        {
            this.useDefaultConfiguration = runWithDefaultConfiguration;

            this.Logger = this.CreateLogger();
            if (this.Logger == null)
            {
                throw new InvalidOperationException(Resources.NullLoggerFacadeException);
            }

            this.Logger.Log(Resources.LoggerCreatedSuccessfully, Category.Debug, Priority.Low);

            this.Logger.Log(Resources.CreatingModuleCatalog, Category.Debug, Priority.Low);
            this.ModuleCatalog = this.CreateModuleCatalog();
            if (this.ModuleCatalog == null)
            {
                throw new InvalidOperationException(Resources.NullModuleCatalogException);
            }

            this.Logger.Log(Resources.ConfiguringModuleCatalog, Category.Debug, Priority.Low);
            this.ConfigureModuleCatalog();

            this.Logger.Log(Resources.CreatingWindsorContainer, Category.Debug, Priority.Low);
            this.Container = this.CreateContainer();
            if (this.Container == null)
            {
                throw new InvalidOperationException(Resources.NullUnityContainerException);
            }

            this.Logger.Log(Resources.ConfiguringUnityContainer, Category.Debug, Priority.Low);
            this.ConfigureContainer();

            this.Logger.Log(Resources.ConfiguringServiceLocatorSingleton, Category.Debug, Priority.Low);
            this.ConfigureServiceLocator();

            this.Logger.Log(Resources.ConfigureViewModelLocator, Category.Debug, Priority.Low);
            this.ConfigureViewModelLocator();

            this.Logger.Log(Resources.ConfiguringRegionAdapters, Category.Debug, Priority.Low);
            this.ConfigureRegionAdapterMappings();

            this.Logger.Log(Resources.ConfiguringDefaultRegionBehaviors, Category.Debug, Priority.Low);
            this.ConfigureDefaultRegionBehaviors();

            this.Logger.Log(Resources.RegisteringFrameworkExceptionTypes, Category.Debug, Priority.Low);
            this.RegisterFrameworkExceptionTypes();

            this.Logger.Log(Resources.CreatingShell, Category.Debug, Priority.Low);
            this.Shell = this.CreateShell();
            if (this.Shell != null)
            {
                this.Logger.Log(Resources.SettingTheRegionManager, Category.Debug, Priority.Low);
                RegionManager.SetRegionManager(this.Shell, this.Container.Resolve<IRegionManager>());

                this.Logger.Log(Resources.UpdatingRegions, Category.Debug, Priority.Low);
                RegionManager.UpdateRegions();

                this.Logger.Log(Resources.InitializingShell, Category.Debug, Priority.Low);
                this.InitializeShell();
            }

            this.Logger.Log(Resources.InitializingModules, Category.Debug, Priority.Low);
            this.InitializeModules();

            this.Logger.Log(Resources.BootstrapperSequenceCompleted, Category.Debug, Priority.Low);
        }

        /// <summary>
        /// Configures the LocatorProvider for the <see cref="ServiceLocator" />.
        /// </summary>
        protected override void ConfigureServiceLocator()
        {
            ServiceLocator.SetLocatorProvider(() => this.Container.Resolve<IServiceLocator>());
        }

        /// <summary>
        /// Registers in the <see cref="IWindsorContainer"/> the <see cref="Type"/> of the Exceptions
        /// that are not considered root exceptions by the <see cref="ExceptionExtensions"/>.
        /// </summary>
        protected override void RegisterFrameworkExceptionTypes()
        {
            base.RegisterFrameworkExceptionTypes();

            ExceptionExtensions.RegisterFrameworkExceptionType(
                typeof(Castle.Windsor.Configuration.Interpreters.ConfigurationProcessingException));

            ExceptionExtensions.RegisterFrameworkExceptionType(
                typeof(ComponentNotFoundException));
        }

        /// <summary>
        /// Configures the <see cref="IWindsorContainer"/>. May be overwritten in a derived class to add specific
        /// type mappings required by the application.
        /// </summary>
        protected virtual void ConfigureContainer()
        {
            this.Container.Register(Component.For<ILoggerFacade>().Instance(this.Logger));

            this.Container.Register(Component.For<IModuleCatalog>().Instance(this.ModuleCatalog));

            if (this.useDefaultConfiguration)
            {
                this.Container.Register(Component.For<IWindsorContainer>().Instance(this.Container));
                this.RegisterTypeIfMissing(typeof(IServiceLocator), typeof(WindsorServiceLocator), true);
                this.RegisterTypeIfMissing(typeof(IModuleInitializer), typeof(ModuleInitializer), true);
                this.RegisterTypeIfMissing(typeof(IModuleManager), typeof(ModuleManager), true);
                this.RegisterTypeIfMissing(typeof(RegionAdapterMappings), typeof(RegionAdapterMappings), true);
                this.RegisterTypeIfMissing(typeof(IRegionManager), typeof(RegionManager), true);
                this.RegisterTypeIfMissing(typeof(IEventAggregator), typeof(EventAggregator), true);
                this.RegisterTypeIfMissing(typeof(IRegionViewRegistry), typeof(RegionViewRegistry), true);
                this.RegisterTypeIfMissing(typeof(IRegionBehaviorFactory), typeof(RegionBehaviorFactory), true);
                this.RegisterTypeIfMissing(typeof(IRegionNavigationJournalEntry), typeof(RegionNavigationJournalEntry), false);
                this.RegisterTypeIfMissing(typeof(IRegionNavigationJournal), typeof(RegionNavigationJournal), false);
                this.RegisterTypeIfMissing(typeof(IRegionNavigationService), typeof(RegionNavigationService), false);
                this.RegisterTypeIfMissing(typeof(IRegionNavigationContentLoader), typeof(RegionNavigationContentLoader), true);
                this.RegisterTypeIfMissing(typeof(DelayedRegionCreationBehavior), typeof(DelayedRegionCreationBehavior), false);

                // register region adapters
                this.Container.Register(Classes.FromAssemblyContaining<IRegionAdapter>().BasedOn<IRegionAdapter>().LifestyleTransient());

                // register region behaviors
                this.Container.Register(Classes.FromAssemblyContaining<IRegionBehavior>().BasedOn<IRegionBehavior>().LifestyleTransient());
            }
        }

        /// <summary>
        /// Initializes the modules. May be overwritten in a derived class to use a custom Modules Catalog
        /// </summary>
        protected override void InitializeModules()
        {
            IModuleManager manager;

            try
            {
                manager = this.Container.Resolve<IModuleManager>();
            }
            catch (ComponentNotFoundException ex)
            {
                if (ex.Message.Contains("IModuleCatalog"))
                {
                    throw new InvalidOperationException(Resources.NullModuleCatalogException);
                }

                throw;
            }

            manager.Run();
        }

        /// <summary>
        /// Creates the <see cref="IWindsorContainer"/> that will be used as the default container.
        /// </summary>
        /// <returns>A new instance of <see cref="IWindsorContainer"/>.</returns>
        [CLSCompliant(false)]
        protected virtual IWindsorContainer CreateContainer()
        {
            return new WindsorContainer();
        }

        /// <summary>
        /// Registers a type in the container only if that type was not already registered.
        /// </summary>
        /// <param name="fromType">The interface type to register.</param>
        /// <param name="toType">The type implementing the interface.</param>
        /// <param name="registerAsSingleton">Registers the type as a singleton.</param>
        protected void RegisterTypeIfMissing(Type fromType, Type toType, bool registerAsSingleton)
        {
            if (fromType == null)
            {
                throw new ArgumentNullException("fromType");
            }
            if (toType == null)
            {
                throw new ArgumentNullException("toType");
            }
            if (this.Container.Kernel.HasComponent(fromType))
            {
                this.Logger.Log(
                    String.Format(CultureInfo.CurrentCulture,
                                  Resources.TypeMappingAlreadyRegistered,
                                  fromType.Name), Category.Debug, Priority.Low);
            }
            else
            {
                if (registerAsSingleton)
                {
                    this.Container.Register(Component.For(fromType)
                        .ImplementedBy(toType)
                        .LifeStyle.Singleton);
                }
                else
                {
                    this.Container.Register(Component.For(fromType)
                        .ImplementedBy(toType)
                        .LifeStyle.Transient);
                }
            }
        }
    }
}
