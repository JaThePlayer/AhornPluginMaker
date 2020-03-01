using System;
using System.Collections.Generic;
using System.IO;

namespace AhornPluginMaker
{
    class AhornPlugin
    {
        string csFile;
        public PlacementTypes Type;
        public string FullName;
        public string Plugin;
        /// <summary>
        /// paramName -> [type, defaultValue]
        /// </summary>
        public Dictionary<string, string[]> Params = new Dictionary<string, string[]>();
        string getParamsString()
        {
            string str = "";
            if (Type == PlacementTypes.Trigger)
            {
                str += ", width::Integer=16, height::Integer=16";
            }
            if (Params.Count > 0)
            {
                foreach (string prop in Params.Keys)
                {
                    str += $", {prop}::{Params[prop][0]}={Params[prop][1]}";
                }
            }
            return str;
        }

        public string ModName => FullName.Split('/')[0].Trim('"');
        public string Name
        {
            get
            {
                string[] split = FullName.Split('/');
                if (split.Length > 1)
                {
                    return split[1].Trim('"');
                }
                return FullName.Trim('"');
            }
        }
        string className;

        public AhornPlugin(string csfilePath)
        {
            csFile = File.ReadAllText(csfilePath);
            getName();
            getType();
            getParams();
            getPlugin();
            string pluginPath = Path.Combine(Path.GetDirectoryName(csfilePath), Path.GetFileNameWithoutExtension(csfilePath) + ".jl");
            File.WriteAllText(pluginPath, Plugin);
            Console.WriteLine($"\nSaved plugin to {pluginPath}\n");
        }



        void getName()
        {
            foreach (string a in customEntityDefs)
            {
                if (csFile.Contains(a))
                {
                    string def = csFile.Remove(0, csFile.IndexOf(a) + 1);
                    def = def.Remove(def.IndexOf(')')).TrimStart(a.ToCharArray()).Split(',')[0].Trim('"');
                    FullName = def;
                    break;
                }
            }
        }

        void getType()
        {
            string classDef = csFile.Remove(0, csFile.IndexOf("class"));
            classDef = classDef.Remove(classDef.IndexOf('{')).Trim();
            className = classDef.Remove(classDef.IndexOf(':')).Remove(0, classDef.IndexOf("class") + 5).Trim();
            string inheritedType = classDef.Remove(0, classDef.IndexOf(':') + 1).Trim();
            if (inheritedType == "Trigger")
            {
                Type = PlacementTypes.Trigger;
            }
            else
            {
                Type = PlacementTypes.Entity;
            }
        }

        void getParams()
        {
            // Hardest bit, since parameters can be hidden anywhere in the ctor
            string ctor = csFile.Remove(0, csFile.IndexOf($"{className}("));
            // now we need to detect the end of the ctor - plz give me a better way to do this D:
            ctor = ctor.Remove(ctor.IndexOf("public")).Trim();
            // now let's get the name of the variable holding the EntityData
            string ctorParams = ctor.Split(':')[0];
            string dataName = ctorParams.Remove(0, ctorParams.IndexOf("EntityData") + 11).Split(',')[0];
            // now let's hunt for functions getting parameters
            string dataCall = $"{dataName}.";
            ctor = ctor.Remove(0, ctor.IndexOf('{'));
            while (ctor.Contains(dataCall))
            {
                ctor = ctor.Remove(0, ctor.IndexOf(dataCall) + dataCall.Length);
                string funcName = ctor.Remove(ctor.IndexOf('(')).Trim();
                if (entityDataFuncs.ContainsKey(funcName))
                {
                    string func = getFunc();

                    string[] vals = func.Split(',');
                    string type = entityDataFuncs[funcName];
                    string defaultVal = vals.Length > 1 ? vals[1] : defaultVals[funcName];
                    if (funcName == "HexColor")
                    {
                        if (defaultVal.Contains("Calc.HexToColor"))
                            defaultVal = defaultVal.Substring("Calc.HexToColor".Length + 1).TrimStart('(').TrimEnd(')');
                    }
                    Params.Add(vals[0].Trim('"'), new string[] { type, defaultVal.Trim() });
                }


                string getFunc()
                {
                    string f = ctor.Remove(ctor.IndexOf(';')).Trim();
                    int openBrackets = 1;
                    string fparams = f.Remove(0, f.IndexOf('(') + 1).Trim();
                    int i;
                    for (i = 0; i < fparams.Length; i++)
                    {
                        if (fparams[i] == '(')
                        {
                            openBrackets++;
                        }
                        else if (fparams[i] == ')')
                        {
                            openBrackets--;
                            if (openBrackets == 0)
                            {
                                break;
                            }
                        }
                    }
                    fparams = fparams.Remove(i);
                    return fparams;
                }
            }
        }

        /// <summary>
        /// name -> juliaType
        /// </summary>
        static Dictionary<string, string> entityDataFuncs = new Dictionary<string, string>()
        {
            { "Attr", "String" },
            { "Bool", "Bool"},
            { "Char", "Char"},
            { "Float", "Number"},
            { "HexColor", "String" },
            { "Int", "Integer" }
        };

        static Dictionary<string, string> defaultVals = new Dictionary<string, string>()
        {
            { "Attr", "\"\"" },
            { "Bool", "false"},
            { "Char", "\'\'"},
            { "Float", "1.0"},
            { "HexColor", "\"ffffff\"" },
            { "Int", "1" }
        };

        static List<string> customEntityDefs = new List<string>()
        {
            "[Celeste.Mod.Entities.CustomEntity(",
            "[CustomEntity("
        };

        //static List<string> entityTypes = new List<string>()
        //{
        //    "Entity", "Solid", "Platform"
        //};

        public enum PlacementTypes
        {
            Entity,
            Trigger
        }

        void getPlugin()
        {
            string placementName = FullName.Replace("/", "") + "Placement";
            string plugin = $"module {FullName.Replace("/", "")}Module\n\nusing ..Ahorn, Maple\n\n"
            + $"@mapdef {Type.ToString()} \"{FullName}\" {placementName}(x::Integer, y::Integer{getParamsString()})\n\n"
            + "const placements = Ahorn.PlacementDict(\n	"
            + $"{$"\"{humanize(Name)} ({humanize(ModName)})\" => Ahorn.EntityPlacement(\n"}"
            + $"		{placementName}";
            if (Type == PlacementTypes.Trigger)
            {
                plugin += ",\n		\"rectangle\"\n";
            }
            else
            {
                plugin += "\n";
            }

            plugin += $"	)\n)\n\n";
            // placement done
            // Drawing code, only for Entities
            if (Type == PlacementTypes.Entity)
            {
                plugin += "sprite = \"path/to/sprite00.png\"\n\n"
                       + $"function Ahorn.selection(entity::{placementName})\n"
                       + $"    x, y = Ahorn.position(entity)\n"
                       + $"    return Ahorn.getSpriteRectangle(sprite, x, y)\n"
                       + "end\n\n"
                       + $"Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::{placementName}, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)\n\n";

            }

            plugin += "end";
            Plugin = plugin;
            Console.WriteLine(Plugin);
        }

        string humanize(string s)
        {
            string newString = char.ToUpper(s[0]).ToString();
            bool lastWasUpper = true;
            for (int i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]))
                {
                    if (!lastWasUpper)
                        newString += " ";
                    lastWasUpper = true;
                }
                else
                {
                    lastWasUpper = false;
                }
                newString += s[i];
            }
            return newString;
        }
    }
}
