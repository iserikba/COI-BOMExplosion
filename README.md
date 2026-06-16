# BOMExplosion

**BOMExplosion** is a high-performance C# library and UI toolkit designed for *Captain of Industry*. It provides an advanced, real-time Material Requirements Planning (MRP) calculation engine that allows players to perform complex "Bill of Materials" (BOM) explosions on their factory production chains.

### Why "BOMExplosion"?

The name might sound destructive, but in the world of industrial engineering and supply chain management, **"BOM Explosion"** is a formal, industry-standard term.

A **Bill of Materials (BOM)** is essentially the "recipe" for a product. When you have a complex item—like a "Lab Equipment"—it is composed of electronics, steel, and rubber. Those electronics, in turn, are made of copper and silicon.

To "explode" the BOM means to mathematically deconstruct a final product through every sub-assembly until you reach the raw base materials (ore, water, power). This mod "explodes" the game's recipes in real-time, instantly telling the player exactly how much raw material they need to feed their factory to meet their production goals.

### Project Overview

This project refactors and optimizes the production calculation logic to provide:

* **Recursive BOM Calculation:** Dynamically walks the factory tree to calculate exact input requirements.
* **Fluent UI Toolkit:** Uses an optimized, chainable UI architecture to build complex production target rows on the fly.
* **Data Persistence:** A custom JSON-based serialization engine (using memory-optimized `StringBuilder` parsing) to save and load complex factory presets without causing game stutter.
* **Dependency Injection:** Cleanly integrated into the game's architecture using modular service patterns.

### Technical Highlights

* **Performance-First:** Designed to minimize Garbage Collection (GC) pressure, ensuring the mod remains invisible to the game's main simulation tick.
* **Modular Architecture:** Separates the calculation math (`Core.Calculation`) from the visualization layer (`UI`), allowing for easy expansion.
* **Extensible:** Built with future-proofing in mind, making it simple to inject new metrics like power consumption, labor force, and maintenance costs into existing chain calculations.
