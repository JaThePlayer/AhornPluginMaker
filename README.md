# Ahorn Plugin Maker
A program to turn .cs files into [Ahorn](https://github.com/CelestialCartographers/Ahorn) plugins.

Just run the program, drag a .cs file into it and watch the magic happen. 

Once your plugin is generated, you can drop it into your Ahorn/entities (or triggers) folder. 

Now, before the plugin works, you'll need to open it and change the `sprite` variable to the path to a sprite of your entity. This step is not needed for Triggers.

There are a few requirements for this program to create correct plugins:
- Your entity/trigger needs to use the CustomEntity attribute.
- Your entity/trigger can only have ONE constructor.



Note: This program is not made by the Ahorn developers. Any issues regarding this program should go to JaThePlayer#2580 on discord, not them!
