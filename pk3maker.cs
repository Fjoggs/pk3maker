using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pk3Maker
{
    class Pk3Maker
    {
        private static string mapName;
        private static List<string> finalTextureList;
        private static Dictionary<string, List<string>> pk3Structure = new Dictionary<string, List<string>>();
        static void Main()
        {
            // Structure
            // cfg-maps
            // env
            // levelshots
            // maps
            // scripts
            // sound
            // textures
            Stopwatch watch = Stopwatch.StartNew();
            mapName = "Fjo3tourney6_rc3";
            Pk3Maker.addCfgMapFileIfPresent();
            Pk3Maker.addLevelshotIfPresent();
            Pk3Maker.parseMapFile();
            string tempDirectory = Pk3Maker.makeFolders();
            Pk3Maker.makePk3(tempDirectory);
            Console.WriteLine("Finished writing pk3");
            watch.Stop();
            Console.WriteLine($"Elapsed time {watch.ElapsedMilliseconds}ms");
        }

        static string makeFolders()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Pk3Maker.mapName);
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory($"{tempDirectory}/maps");
            Pk3Maker.copyFileToTemp(tempDirectory, $"maps/{mapName}.bsp");
            Pk3Maker.copyFileToTemp(tempDirectory, $"maps/{mapName}.map");
            List<string> list = new List<string>();
            if (Pk3Maker.pk3Structure.TryGetValue("cfg-maps", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/cfg-maps");
                foreach (string cfgMap in list)
                {
                    string fileName = Path.GetFileName(cfgMap);
                    string path = $"cfg-maps/{fileName}";
                    Pk3Maker.copyFileToTemp(tempDirectory, path);
                }
            }
            if (Pk3Maker.pk3Structure.TryGetValue("levelshots", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/levelshots");
                foreach (string levelshot in list)
                {
                    string fileName = Path.GetFileName(levelshot);
                    string path = $"levelshots/{fileName}";
                    Pk3Maker.copyFileToTemp(tempDirectory, path);
                }
            }
            if (Pk3Maker.pk3Structure.TryGetValue("env", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/env");
                foreach (string env in list)
                {
                    Pk3Maker.copyFileToTemp(tempDirectory, env);
                }
            }
            if (Pk3Maker.finalTextureList.Count > 0)
            {
                Directory.CreateDirectory($"{tempDirectory}/textures");
                foreach (string texture in Pk3Maker.finalTextureList)
                {
                    Pk3Maker.copyFileToTemp(tempDirectory, texture);
                }

            }
            if (Pk3Maker.pk3Structure.TryGetValue("scripts", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/scripts");
                Pk3Maker.copyFileToTemp(tempDirectory, $"scripts/{mapName}.arena");
                foreach (string script in list)
                {
                    string fileName = Path.GetFileName(script);
                    string path = $"scripts/{fileName}";
                    Pk3Maker.copyFileToTemp(tempDirectory, path);
                }
            }

            string[] folders = Directory.GetDirectories(tempDirectory);
            foreach (string folder in folders)
            {
                Console.WriteLine(folder);
            }
            return tempDirectory;
        }

        static void makePk3(string directoryToZip)
        {
            string workingDirectory = Directory.GetCurrentDirectory();
            string pk3File = $"{workingDirectory}/{mapName}.pk3";
            if (File.Exists(pk3File))
            {
                File.Delete(pk3File);
            }
            ZipFile.CreateFromDirectory(directoryToZip, pk3File);
            Console.WriteLine($"Wrote succesfully to {pk3File}");
        }

        static void copyFileToTemp(string tempDirectory, string file)
        {
            Directory.CreateDirectory(tempDirectory + "/" + Path.GetDirectoryName(file));
            string sourceFile = $"/home/fjogen/games/quake3/baseq3/{file}";
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, $"{tempDirectory}/{file}", true);
            }
        }

        static void parseMapFile()
        {
            string file = $"/home/fjogen/games/quake3/baseq3/maps/{Pk3Maker.mapName}.map";
            string[] lines = System.IO.File.ReadAllLines(file);
            List<string> shaderNamesAndTextures = Pk3Maker.getShaderNamesAndTextures(lines);
            List<string> shaders = Pk3Maker.getShaders(shaderNamesAndTextures);
            List<string> shaderNames = Pk3Maker.getShaderNames(shaders);
            shaderNamesAndTextures = Pk3Maker.addExtensionsToTextures(shaderNamesAndTextures, shaderNames);
            List<string> additionalTextures = Pk3Maker.additionalTextures(shaders, shaderNamesAndTextures);
            Pk3Maker.pk3Structure.Add("textures", shaderNamesAndTextures);
            Pk3Maker.pk3Structure.Add("scripts", shaders);
            Pk3Maker.pk3Structure.Add("additionalTextures", additionalTextures);
        }

        static void addCfgMapFileIfPresent()
        {
            string file = $"/home/fjogen/games/quake3/baseq3/cfg-maps/{Pk3Maker.mapName}.cfg";
            if (File.Exists(file))
            {
                Pk3Maker.pk3Structure.Add("cfg-maps", new List<string> { file });
            }
            else
            {
                Console.WriteLine("No cfg-map file found. Store in baseq3/cfg-maps/Mapname.cfg");
            }
        }

        static void addLevelshotIfPresent()
        {
            string file = $"/home/fjogen/games/quake3/baseq3/levelshots/{Pk3Maker.mapName}.jpg";
            if (System.IO.File.Exists(file))
            {
                Pk3Maker.pk3Structure.Add("levelshots", new List<string> { file });
            }
            else
            {
                Console.WriteLine("No levelshot found. Use .jpg");
            }
        }

        static List<string> getShaderNamesAndTextures(string[] lines)
        {
            List<string> shaderNamesOrTextures = new List<string>();
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
                    if (Regex.IsMatch(line, @"((\w+)\/((\w)+[\/_-]*)*)+"))
                    {
                        string texture = Regex.Match(line, @"((\w+)\/((\w)+[\/_-]*)*)+").Value;
                        if (!shaderNamesOrTextures.Contains(texture) && !texture.Contains("common/") && !texture.Contains("common_alphascale/") && !texture.Contains("sfx/") && !texture.Contains("liquids/") && !texture.Contains("effects/"))
                        {
                            shaderNamesOrTextures.Add(texture);
                        }
                    }
                }
                if (line.Contains("// brush"))
                {
                    isBrush = true;
                }
            }
            return shaderNamesOrTextures;
        }

        static List<string> addExtensionsToTextures(List<string> texturesAndShaderNames, List<string> shaderNames)
        {
            // NOTE: This will fail if someone is using the same shadername as a texture for a shader that doesnt actually use
            // that texture. (Like a sky, which uses env textures)
            List<string> list = new List<string>();
            foreach (string textureOrShader in texturesAndShaderNames)
            {
                if (shaderNames.Contains($"textures/{textureOrShader}"))
                {
                    // This is a shader; add nothing
                    list.Add($"textures/{textureOrShader}");
                    continue;
                }
                bool isJpg = File.Exists($"/home/fjogen/games/quake3/baseq3/textures/{textureOrShader}.jpg");
                bool isTga = File.Exists($"/home/fjogen/games/quake3/baseq3/textures/{textureOrShader}.tga");
                if (isJpg)
                {
                    list.Add($"textures/{textureOrShader}.jpg");
                    continue;
                }
                else if (isTga)
                {
                    list.Add($"textures/{textureOrShader}.tga");
                    continue;
                }
                else
                {
                    // Console.WriteLine("This is a shader with a name with no matching texture name; adding as is to replace later");
                    list.Add($"textures/{textureOrShader}");
                    continue;
                }
            }
            return list;
        }

        static List<string> getShaders(List<string> shaderNamesAndTextures)
        {
            string[] shaderFiles = Directory.GetFiles("/home/fjogen/games/quake3/baseq3/scripts/", "*.shader");
            List<string> shaders = new List<string>();
            foreach (string file in shaderFiles)
            {
                string shader = File.ReadAllText(file);
                foreach (string texture in shaderNamesAndTextures)
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

        static List<string> getShaderNames(List<string> shaders)
        {
            List<string> shaderNames = new List<string>();
            foreach (string file in shaders)
            {
                string[] shader = File.ReadAllLines(file);
                foreach (string line in shader)
                {
                    // Disregard result if line contains whitespace (meaning it's not a shadername)
                    if (Regex.IsMatch(line, @"((\w+)\/((\w)+[\/_-]*)*)+") && !Regex.IsMatch(line, @"\s"))
                    {
                        shaderNames.Add(Regex.Match(line, @"((\w+)\/((\w)+[\/_-]*)*)+").Value.Trim());
                        continue;
                    }

                }
            }
            return shaderNames;
        }

        static List<string> additionalTextures(List<string> shaders, List<string> shaderNamesOrTextures)
        {
            List<string> additionalTextures = new List<string>();
            Pk3Maker.finalTextureList = shaderNamesOrTextures;
            List<string> env = new List<string>();
            foreach (string shader in shaders)
            {
                string[] lines = File.ReadAllLines(shader);
                bool shaderInUse = false;
                int openBrackets = 0;
                int closeBrackets = 0;
                foreach (string line in lines)
                {
                    if (shaderNamesOrTextures.Contains(line))
                    {
                        Pk3Maker.finalTextureList.Remove(line); // Removing this, as it's being replaced by a texture
                        shaderInUse = true;
                        continue;
                    }
                    if (shaderInUse)
                    {
                        if (line.Equals("{"))
                        {
                            openBrackets++;
                            continue;
                        }
                        if (line.Equals("}"))
                        {
                            closeBrackets++;
                        }
                        if (openBrackets == closeBrackets && openBrackets != 0)
                        {
                            shaderInUse = false;
                            continue;
                        }
                        string trimmedLine = line.Trim();
                        trimmedLine = trimmedLine.Replace("\\", "/");
                        if (trimmedLine.Contains("map ") || trimmedLine.Contains("clampMap ") || trimmedLine.Contains("animMap ") || trimmedLine.Contains("videoMap ") || trimmedLine.Contains("skyParms "))
                        {
                            if (trimmedLine.Equals("map $lightmap") || trimmedLine.Equals("map $whiteimage") || trimmedLine.Contains("skyParms -") || trimmedLine.Contains("skyParms full"))
                            {
                                continue;
                            }
                            else
                            {
                                trimmedLine = trimmedLine.Replace("animMap ", "");
                                trimmedLine = trimmedLine.Replace("animmap ", "");
                                trimmedLine = trimmedLine.Replace("clampMap ", "");
                                trimmedLine = trimmedLine.Replace("clampmap ", "");
                                trimmedLine = trimmedLine.Replace("videoMap ", "");
                                trimmedLine = trimmedLine.Replace("videomap ", "");
                                trimmedLine = trimmedLine.Replace("map ", "");
                                trimmedLine = trimmedLine.Replace("skyParms ", "");
                                trimmedLine = trimmedLine.Replace("skyparms ", "");
                                trimmedLine = trimmedLine.Trim(); // Trim that trimmed shit
                                if (trimmedLine.Contains("textures/sfx") || trimmedLine.Contains("textures/effects") || trimmedLine.Contains("textures/liquids"))
                                {
                                    continue;
                                }
                                if (additionalTextures.Contains(trimmedLine))
                                {
                                    continue;
                                }
                                if (trimmedLine.Contains("env/"))
                                {
                                    string envName = Regex.Match(line, @"(\w+\/\w+)+\s").Value.Trim();
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_bk.tga"))
                                    {
                                        env.Add($"{envName}_bk.tga");
                                    }
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_dn.tga"))
                                    {
                                        env.Add($"{envName}_dn.tga");
                                    }
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_ft.tga"))
                                    {
                                        env.Add($"{envName}_ft.tga");
                                    }
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_lf.tga"))
                                    {
                                        env.Add($"{envName}_lf.tga");
                                    }
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_rt.tga"))
                                    {
                                        env.Add($"{envName}_rt.tga");
                                    }
                                    if (File.Exists($"/home/fjogen/games/quake3/baseq3/{envName}_up.tga"))
                                    {
                                        env.Add($"{envName}_up.tga");
                                    }
                                    continue;
                                }
                                if (!shaderNamesOrTextures.Contains(trimmedLine.Replace(".tga", "")) || !shaderNamesOrTextures.Contains(trimmedLine.Replace(".jpg", "")))
                                {
                                    string textureWithExtension = addCorrectExtension(trimmedLine);
                                    if (!additionalTextures.Contains(textureWithExtension))
                                    {
                                        additionalTextures.Add(textureWithExtension);
                                        Pk3Maker.finalTextureList.Add(textureWithExtension);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Pk3Maker.pk3Structure.Add("env", env);
            return additionalTextures;
        }

        static string addCorrectExtension(string texture)
        {
            string temp = texture.Replace(".jpg", "");
            temp = temp.Replace(".tga", "");
            bool isJpg = File.Exists($"/home/fjogen/games/quake3/baseq3/{temp}.jpg");
            bool isTga = File.Exists($"/home/fjogen/games/quake3/baseq3/{temp}.tga");
            if (isJpg)
            {
                return $"{temp}.jpg";
            }
            else if (isTga)
            {
                return $"{temp}.tga";
            }
            else
            {
                Console.WriteLine("Getting here probably means there's an unused shader in the shader file");
                return "";
            }
        }
    }
}