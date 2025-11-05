# Kingdoms - Recreation Project

## Overview
Recreation of the KINGDOMS game (Steam) - A procedural medieval RPG with advanced AI systems.

## Project Architecture

### Folder Structure
- **Managers/**: Core game managers (Singleton pattern)
- **Player/**: Player controller, inventory, stats
- **NPC/**: NPC behaviors and components
- **AI/**: Decision systems, needs, pathfinding
- **World/**: Procedural generation, terrain, biomes
- **Building/**: Construction system, settlements
- **Data/**: ScriptableObjects for game data
- **UI/**: User interface components
- **Utils/**: Helper classes and extensions

## Development Roadmap

### Phase 1 - MVP (Minimum Viable Product)
**Goal**: Basic playable prototype

- [ ] World Generation
  - [ ] Basic terrain generation
  - [ ] Simple biomes (plains, forests)
  
- [ ] Player System
  - [ ] First-person movement
  - [ ] Basic interactions
  
- [ ] NPC System
  - [ ] Spawn NPCs
  - [ ] Basic AI (wander, idle)
  
- [ ] Day/Night Cycle
  - [ ] Time system
  - [ ] Lighting changes

### Phase 2 - AI & Simulation
**Goal**: Living world with basic NPC needs

- [ ] NPC Needs System
  - [ ] Hunger, thirst, sleep
  - [ ] Decision-making based on needs
  
- [ ] Pathfinding
  - [ ] NavMesh integration
  - [ ] Smart movement
  
- [ ] Basic Economy
  - [ ] Resources
  - [ ] Trading

### Phase 3 - Building & Settlements
**Goal**: Construction and settlement systems

- [ ] Building System
  - [ ] Place buildings
  - [ ] Building types (house, tavern, shop)
  
- [ ] Settlement Management
  - [ ] NPCs build settlements
  - [ ] Settlement growth

### Phase 4 - Advanced Features
- [ ] Combat system
- [ ] Crafting
- [ ] Political system
- [ ] Kingdom management

## Technical Guidelines

### Coding Standards
1. English for all code (variables, functions, classes, comments)
2. Follow SOLID principles
3. Use managers for each major system
4. Keep it simple - implement only what's requested
5. Prioritize readability over optimization

### Architecture Patterns
- **Singleton**: For managers (GameManager, etc.)
- **Component**: For modular behaviors
- **ScriptableObject**: For data and configurations
- **Observer**: For event systems

## Current Status
**Phase**: Setup
**Last Update**: Initial project setup complete

## Next Steps
1. Create GameManager
2. Setup basic scene
3. Implement player controller
4. Start world generation prototype
