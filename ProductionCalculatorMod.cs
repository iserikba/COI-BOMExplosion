using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Unity.UiToolkit.Library;
using ProductionCalculator.Core.Persistence;
using ProductionCalculator.Core.Services;
using System;
using System.ComponentModel;
using System.Runtime.Remoting.Contexts;
using static Mafi.Base.Ids.Fleet;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace ProductionCalculator
{
    /// <summary>
    /// The main entry point for the mod. 
    /// This class handles the registration of dependencies and system initialization.
    /// 
    /// The DependencyResolver(The DI Container): You might have noticed how in your ProductionCalculatorWindow constructor, 
    /// it asks for ProductionCalculatorService and SavedCalculationRepository automatically.That "magic" is handled by the 
    /// RegisterDependencies method.You are telling the game: "Whenever any class asks for these two services, instantiate them 
    /// and give them the references." It makes your code very clean because you don't have to manage the lifetime of your services manually.
    ///    IsUiOnly => true: This is a performance flag for the game engine.Because your mod is just a calculator, the engine doesn't 
    ///    need to synchronize this data across multiplayer or run it through the game's simulation tick, which keeps the game smooth.
    ///   RegisterPrototypes vs RegisterDependencies:
    ///   RegisterPrototypes is for content (adding new machines, buildings, items).
    /// RegisterDependencies is for logic/infrastructure(services, file storage, data managers).
    /// </summary>
    public sealed class ProductionCalculatorMod : IMod, IDisposable
    {
        public ModManifest Manifest { get; private set; }
        public ModJsonConfig JsonConfig { get; private set; }
        public Option<IConfig> ModConfig { get; set; }

        // Tells the game engine that this mod only touches UI and doesn't 
        // modify the core game simulation/world-state.
        public bool IsUiOnly => true;

        public ProductionCalculatorMod(ModManifest manifest)
        {
            this.Manifest = manifest;
            this.JsonConfig = new ModJsonConfig(this);

            Log.Info("BOMExplosion: Mod successfully loaded.");
        }

        // Standard interface method: Use this if you are adding new items/recipes 
        // to the game's ProtosDb. Since this is a calculator, we leave it empty.
        void IMod.RegisterPrototypes(ProtoRegistrator registrator) { }

        // This is where the magic happens. We register our custom services into 
        // the game's Dependency Injection (DI) container.
        void IMod.RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
            // Configure the folder where we save our JSON files
            SavedCalculationRepository.ConfigureStorage(this.Manifest);

            // Register the Calculator Service and Repo so other windows can access them.
            depBuilder.RegisterDependency<ProductionCalculatorService>().AsSelf();
            depBuilder.RegisterDependency<SavedCalculationRepository>().AsSelf();
        }

        void IMod.EarlyInit(DependencyResolver resolver) { }

        void IMod.Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            Log.Info("BOMExplosion: Mod initialization complete.");
        }

        void IMod.MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) { }

        public void Dispose() { }
    }
}