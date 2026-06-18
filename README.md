# BOMExplosion

**BOMExplosion** is a high-performance C# library and UI toolkit designed for *Captain of Industry*. It provides an advanced, real-time Material Requirements Planning (MRP) calculation engine that allows players to perform complex "Bill of Materials" (BOM) explosions on their factory production chains.

### Why "BOMExplosion"?

The name might sound destructive, but in the world of industrial engineering and supply chain management, **"BOM Explosion"** is a formal, industry-standard term.

A **Bill of Materials (BOM)** is essentially the "recipe" for a product. When you have a complex item—like a "Lab Equipment"—it is composed of electronics, steel, and rubber. Those electronics, in turn, are made of copper and silicon.

To "explode" the BOM means to mathematically deconstruct a final product through every sub-assembly until you reach the raw base materials (ore, water, power). This mod "explodes" the game's recipes in real-time, instantly telling the player exactly how much raw material they need to feed their factory to meet their production goals.

### Mod Project Overview

This project refactors and optimizes the production calculation logic to provide:

* **Recursive BOM Calculation:** Dynamically walks the factory tree to calculate exact input requirements.
* **Fluent UI Toolkit:** Uses an optimized, chainable UI architecture to build complex production target rows on the fly.
* **Data Persistence:** A custom JSON-based serialization engine (using memory-optimized `StringBuilder` parsing) to save and load complex factory presets without causing game stutter.
* **Dependency Injection:** Cleanly integrated into the game's architecture using modular service patterns.

### Software Architecture Overview
The Production Calculator leverages a classic multi-tiered software architecture to ensure a robust, maintainable separation of concerns. At the foundational data access tier, the system handles seamless **mod-to-game data interactions**, safely querying the host engine's internal database to extract raw simulation variables, crop math, and recipe constraints without triggering native exceptions. This data feeds into the centralized business logic layer, where a deterministic solver processes complex production chains and scales calculations entirely independently of the UI. The presentation tier sits cleanly on top of this logic, delivering **user-friendly, game-independent interface windows** that dynamically render interactive production lines, visual nodes, and building icons. Finally, to ensure persistence across sessions, the architecture is supported by a **custom JSON save/load system**, allowing users to reliably store, retrieve, and share their optimized factory configurations outside of the host game's standard save state.

### Technical Highlights

* **Performance-First:** Designed to minimize Garbage Collection (GC) pressure, ensuring the mod remains invisible to the game's main simulation tick.
* **Modular Architecture:** Separates the calculation math (`Core.Calculation`) from the visualization layer (`UI`), allowing for easy expansion.
* **Extensible:** Built with future-proofing in mind, making it simple to inject new metrics like power consumption, labor force, and maintenance costs into existing chain calculations.

## Latest Updates (Jun-26)

### New Features & Mechanics

* **Interactive Production Chains:** Enhanced the core solver UI, allowing users to dynamically build and expand production chains simply by clicking directly on product icons.
* **Complete Farm & Greenhouse Integration:** Engineered a custom `GenerateVirtualFarmRecipes` system that perfectly translates the game's abstract agricultural math into standard, solver-friendly recipes.
* **Dynamic Fertility Math:** The calculator now accurately calculates **Fertilizer I** requirements based on the hidden `ConsumedFertilityPerDay` variable for every unique crop (calculating at a perfect 2% restoration rate).
* **Tiered Yield Multipliers:** Crops now accurately reflect the +50% production bonus for Tier II Irrigated Farms and the +100% bonus for Tier III Greenhouses.
* **Tier 1 Farm Filtering:** The solver intelligently ignores basic rain-fed farms (`!HasIrrigationAndFertilizerSupport`), keeping calculations strictly focused on controllable, piped setups.

### UI & Presentation Upgrades

* **Universal Building Support:** Upgraded the `RecipeBuildingTotals` structure to handle both `MachineProto` and `FarmProto` objects simultaneously.
* **Dynamic Icon Rendering:** Patched the left panel and main UI rows to automatically fetch and draw the correct building icons for Farms and Greenhouses, fixing the "invisible icon" bug.
* **Contextual UI Text:** Tooltips and labels now intelligently switch terminology, displaying the localized string for "farms" instead of "machines" when viewing agricultural recipes.

### Under the Hood (Engine & Stability)

* **Bypassed Unity Native Crashes:** Scrapped `Newtonsoft.Json` serialization in favor of a custom, crash-proof C# Reflection "Sniper" script to safely extract deeply nested game variables without triggering the engine's memory protection.
* **Mastered Deterministic Math:** Resolved severe integer truncation bugs and `Fix32` calculation core overflows by properly aligning `Percent`, `PartialQuantity`, and `Quantity` structs to the crop's `DaysToGrow` scale.
* **Future-Proofed Database Lookups:** Swapped raw string lookups for the engine's official static ID constants (`IdsCore.Products.CleanWater` and `Ids.Products.FertilizerChemical`), guaranteeing the mod won't break if the developers rename assets in future updates.
