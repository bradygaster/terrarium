# TerrariumCreature

A creature for the .NET Terrarium ecosystem simulation game.

## Building Your Creature

1. **Customize the attributes** in `TerrariumCreature.cs`:
   - For animals, distribute 100 points across characteristics
   - Adjust size, skin, and colors
   - Set carnivore vs herbivore

2. **Implement behavior** in the event handlers:
   - `OnIdle`: Main logic (movement, eating, attacking)
   - `OnLoad`: State verification each turn
   - `OnAttacked`: Defense reactions
   - `OnAttackedAnimal`: Post-attack behavior

3. **Build and test**:
   ```bash
   dotnet build
   ```

4. **Deploy to Terrarium**:
   - Copy the built DLL to the Terrarium creatures directory
   - Or use the Terrarium client to load your creature

## Strategy Tips

- **Herbivores**: High eyesight + camouflage to find plants and avoid predators
- **Carnivores**: Balance speed, attack, and eyesight to hunt effectively
- **Plants**: Maximize seed spread distance to colonize quickly

## Resources

- [Terrarium Documentation](https://github.com/terrarium-game/terrarium/tree/main/docs)
- [Sample Creatures](https://github.com/terrarium-game/terrarium/tree/main/src/Terrarium.Samples)
- [OrganismBase API](https://github.com/terrarium-game/terrarium/tree/main/src/Terrarium.OrganismBase)

## License

Your creature code is yours. The Terrarium.OrganismBase library is MIT licensed.
