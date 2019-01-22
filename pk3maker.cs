using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pk3Maker
{
    class Pk3Maker
    {
        static void Main()
        {
            List<List<string>> list = parseMapFile();
            foreach (List<string> type in list)
            {
                foreach (string item in type)
                {
                    Console.WriteLine(item);
                }
            }
        }

        static List<List<string>> parseMapFile()
        {
            string file = "/home/fjogen/games/quake3/baseq3/maps/Fjo3tourney6_b6.map";
            string[] lines = System.IO.File.ReadAllLines(file);
            List<string> textures = getTextures(lines);
            List<string> shaders = getShaders(textures);
            List<List<string>> list = new List<List<string>>();
            list.Add(textures);
            list.Add(shaders);
            return list;
        }

        static List<string> getTextures(string[] lines)
        {
            List<string> textures = new List<string>();
            bool isBrush = false;

            foreach (string line in lines)
            {
                if (isBrush)
                {
                    if (line.Contains("// entity"))
                    {
                        isBrush = false;
                        continue;
                    }
                    if (Regex.IsMatch(line, @"(\w+\/\w+)+"))
                    {
                        string texture = Regex.Match(line, @"(\w+\/\w+)+").Value;
                        if (!textures.Contains(texture) && !texture.Contains("common/") && !texture.Contains("common_alphascale/"))
                        {
                            textures.Add(texture);
                        }
                    }
                }
                if (line.Contains("// brush"))
                {
                    isBrush = true;
                }
            }
            return textures;
        }

        static List<string> getShaders(List<string> textures)
        {
            string[] shaderFiles = Directory.GetFiles("/home/fjogen/games/quake3/baseq3/scripts/", "*.shader");
            List<string> shaders = new List<string>();
            foreach (string file in shaderFiles)
            {
                string shader = System.IO.File.ReadAllText(file);
                foreach (string texture in textures)
                {
                    if (shader.Contains(texture))
                    {
                        shaders.Add(file);
                        break;
                    }
                }
            }
            return shaders;
        }
    }
}