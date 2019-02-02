using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

using static Pk3Maker.Shader;

namespace Pk3Maker
{
    class Pk3Maker
    {
        private static string mapName;
        private static string pathToQuake3;
        private static List<string> finalTextureList;

        private static List<string> soundList = new List<string>();
        private static Dictionary<string, List<string>> pk3Structure = new Dictionary<string, List<string>>();
        static int Main(string[] args)
        {
            Stopwatch watch = Stopwatch.StartNew();
            if (args.Length == 0)
            {
                Console.WriteLine("Missing mapName argument");
                return 1;
            }
            Pk3Maker.mapName = args[0];
            Console.WriteLine("Map name:", Pk3Maker.mapName);
            Pk3Maker.pathToQuake3 = "/home/fjogen/games/quake3";
            // Pk3Maker.pathToQuake3 = "/home/vegfjogs/games/quake3";
            Pk3Maker.addCfgMapFileIfPresent();
            Pk3Maker.addLevelshotIfPresent();
            Pk3Maker.parseMapFile();
            string tempDirectory = Pk3Maker.makeFolders();
            Pk3Maker.makePk3(tempDirectory);
            // Pk3Maker.makeZip();
            Console.WriteLine("Finished writing pk3");
            watch.Stop();
            Console.WriteLine($"Elapsed time {watch.ElapsedMilliseconds}ms");
            return 0;
        }

        // Figure out how to prevent pk3 being used while creating .zip
        private static void makeZip()
        {
            string pk3dir = Path.Combine(Path.GetTempPath(), "pk3");
            string zipFile = $"{pk3dir}/{Pk3Maker.mapName}.zip";
            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }
            ZipFile.CreateFromDirectory(pk3dir, zipFile);
            Console.WriteLine($"Wrote succesfully to {zipFile}");
            File.Copy(zipFile, $"{Pk3Maker.pathToQuake3}/baseq3", true);
            Console.WriteLine($"Copied {zipFile} to {Pk3Maker.pathToQuake3}/baseq3");
        }

        static string makeFolders()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Pk3Maker.mapName);
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory($"{tempDirectory}/maps");
            Pk3Maker.copyFileToTemp(tempDirectory, $"maps/{Pk3Maker.mapName}.bsp");
            Pk3Maker.copyFileToTemp(tempDirectory, $"maps/{Pk3Maker.mapName}.map");
            Pk3Maker.copyFileToTemp(tempDirectory, $"maps/{Pk3Maker.mapName}.aas");
            // Add Readme
            Pk3Maker.copyFileToTemp(tempDirectory, $"{Pk3Maker.mapName}.txt");
            List<string> list = new List<string>();

            string[] folders = Directory.GetDirectories(tempDirectory);
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

            folders = Directory.GetDirectories(tempDirectory);
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

            folders = Directory.GetDirectories(tempDirectory);
            if (Pk3Maker.pk3Structure.TryGetValue("env", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/env");
                foreach (string env in list)
                {
                    Pk3Maker.copyFileToTemp(tempDirectory, env);
                }
            }

            folders = Directory.GetDirectories(tempDirectory);
            if (Pk3Maker.finalTextureList.Count > 0)
            {
                Directory.CreateDirectory($"{tempDirectory}/textures");
                foreach (string texture in Pk3Maker.finalTextureList)
                {
                    Pk3Maker.copyFileToTemp(tempDirectory, texture);
                }

            }

            folders = Directory.GetDirectories(tempDirectory);
            if (Pk3Maker.pk3Structure.TryGetValue("scripts", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/scripts");
                copyArenaFile(tempDirectory);
                foreach (string script in list)
                {
                    string fileName = Path.GetFileName(script);
                    string path = $"scripts/{fileName}";
                    Pk3Maker.copyFileToTemp(tempDirectory, path);
                }
            }

            folders = Directory.GetDirectories(tempDirectory);
            if (Pk3Maker.pk3Structure.TryGetValue("sound", out list))
            {
                Directory.CreateDirectory($"{tempDirectory}/sound");
                foreach (string sound in list)
                {
                    Pk3Maker.copyFileToTemp(tempDirectory, sound);
                }
            }

            folders = Directory.GetDirectories(tempDirectory);
            foreach (string folder in folders)
            {
                Console.WriteLine(folder);
            }
            return tempDirectory;
        }

        static void copyArenaFile(string tempDirectory)
        {
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/scripts/{Pk3Maker.mapName}.arena"))
            {
                Pk3Maker.copyFileToTemp(tempDirectory, $"scripts/{Pk3Maker.mapName}.arena");
            }
            // Should probably not include the main arena file if current version does not exist
            // else
            // {
            //     string[] arenaFiles = Directory.GetFiles($"{Pk3Maker.pathToQuake3}/baseq3/scripts/", "*.arena");
            //     string arenaFileWithoutVersioning = Path.GetFileNameWithoutExtension(
            //         arenaFiles.Where(arenaFile => Pk3Maker.mapName.Contains(Path.GetFileNameWithoutExtension(arenaFile))).First()
            //     );
            //     Pk3Maker.copyFileToTemp(tempDirectory, $"scripts/{arenaFileWithoutVersioning}.arena");
            // }

        }

        static void makePk3(string tempDirectory)
        {
            string pk3dir = Path.Combine(Path.GetTempPath(), "pk3");
            Directory.CreateDirectory(pk3dir);
            string pk3File = $"{pk3dir}/{Pk3Maker.mapName}.pk3";
            if (File.Exists(pk3File))
            {
                File.Delete(pk3File);
            }
            ZipFile.CreateFromDirectory(tempDirectory, pk3File);
            Console.WriteLine($"Wrote succesfully to {pk3File}");
            File.Copy(pk3File, $"{Pk3Maker.pathToQuake3}/baseq3", true);
            Console.WriteLine($"Copied {pk3File} to {Pk3Maker.pathToQuake3}/baseq3");
        }

        static void copyFileToTemp(string tempDirectory, string file)
        {
            Directory.CreateDirectory(tempDirectory + "/" + Path.GetDirectoryName(file));
            string sourceFile = $"{Pk3Maker.pathToQuake3}/baseq3/{file}";
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, $"{tempDirectory}/{file}", true);
            }
            else
            {
                Console.WriteLine($"Could not copy file from {sourceFile} to {tempDirectory}/{file}");
            }
        }

        static void parseMapFile()
        {
            List<string> shaderNamesAndTextures = Pk3Maker.getShaderNamesAndTextures();
            List<string> shaderFiles = Pk3Maker.getShaderFiles(shaderNamesAndTextures);
            List<Shader> shaderNames = Pk3Maker.getShaderNames(shaderFiles);
            List<string> shaderNamesAndTexturesWithExtensions = Pk3Maker.addExtensionsToTextures(shaderNamesAndTextures, shaderNames);
            Pk3Maker.finalTextureList = shaderNamesAndTexturesWithExtensions;
            List<Shader> usedShaders = Pk3Maker.extractAllUsedShaders(shaderNamesAndTexturesWithExtensions, shaderNames);
            List<string> texturesFromShaders = Pk3Maker.texturesFromShaders(shaderFiles, usedShaders);
            Pk3Maker.pk3Structure.Add("textures", Pk3Maker.finalTextureList);
            Pk3Maker.pk3Structure.Add("scripts", shaderFiles);
            Pk3Maker.pk3Structure.Add("additionalTextures", texturesFromShaders);
        }

        static void addCfgMapFileIfPresent()
        {
            string file = $"{Pk3Maker.pathToQuake3}/baseq3/cfg-maps/{Pk3Maker.mapName}.cfg";
            if (File.Exists(file))
            {
                Pk3Maker.pk3Structure.Add("cfg-maps", new List<string> { file });
            }
            else
            {
                Console.WriteLine($"No cfg-map file found in baseq3/cfg-maps/{Pk3Maker.mapName}.cfg");
            }
        }

        static void addLevelshotIfPresent()
        {
            string file = $"{Pk3Maker.pathToQuake3}/baseq3/levelshots/{Pk3Maker.mapName}.jpg";
            if (System.IO.File.Exists(file))
            {
                Pk3Maker.pk3Structure.Add("levelshots", new List<string> { file });
            }
            else
            {
                Console.WriteLine("No levelshot found. Use .jpg");
            }
        }

        static List<string> getShaderNamesAndTextures()
        {
            string file = $"{Pk3Maker.pathToQuake3}/baseq3/maps/{Pk3Maker.mapName}.map";
            string[] lines = File.ReadAllLines(file);
            List<string> shaderNamesOrTextures = new List<string>();
            bool isBrush = false;
            bool isEntity = false;
            string trimmedLine;
            int openBrackets = 0;
            int closeBrackets = 0;
            foreach (string line in lines)
            {
                openBrackets = 0;
                closeBrackets = 0;
                trimmedLine = line.Trim();
                if (isBrush)
                {
                    if (trimmedLine.Contains("// entity"))
                    {
                        isEntity = true;
                        isBrush = false;
                        continue;
                    }
                    if (Regex.IsMatch(trimmedLine, @"((\w+)\/((\w)+[\/_-]*)*)+"))
                    {
                        string texture = Regex.Match(trimmedLine, @"((\w+)\/((\w)+[\/_-]*)*)+").Value;
                        if (!shaderNamesOrTextures.Contains(texture) && !texture.Contains("common/") && !texture.Contains("common_alphascale/") && !texture.Contains("sfx/") && !texture.Contains("liquids/") && !texture.Contains("effects/"))
                        {
                            shaderNamesOrTextures.Add(texture);
                        }
                    }
                }
                else if (isEntity)
                {
                    if (Path.GetExtension(trimmedLine).Equals(".ase\""))
                    {
                        string aseFile = trimmedLine.Replace("\"model\"", "").Trim().Replace("\"", "");
                        string pathToAseFile = $"{Pk3Maker.pathToQuake3}/baseq3/{aseFile}";
                        if (File.Exists(pathToAseFile))
                        {
                            foreach (string aseLine in File.ReadLines(pathToAseFile))
                            {
                                if (aseLine.Contains("*BITMAP") && !Path.GetExtension(aseLine).Equals(""))
                                {
                                    // For some reason, some files report textures with forwardslashes, while some uses backwards
                                    string forwardSlash = Regex.Match(aseLine.Trim(), @"textures\\.*[\.jpg|\.tga]+").Value;
                                    string backwardsSlash = Regex.Match(aseLine.Trim(), @"textures/.*[\.jpg|\.tga]+").Value;
                                    string texturePath = "";
                                    if (forwardSlash.Equals(""))
                                    {
                                        // This means that the file used backwards slashes
                                        texturePath = backwardsSlash;
                                    }
                                    else
                                    {
                                        texturePath = forwardSlash;
                                    }
                                    texturePath = texturePath.Replace("\\", "/");
                                    texturePath = texturePath.Replace("textures/", ""); // We haven't added /textures yet
                                    texturePath = texturePath.Replace(".jpg", "").Replace(".tga", ""); // We haven't added extensions yet
                                    if (!shaderNamesOrTextures.Contains(texturePath) && !texturePath.Contains("common/") && !texturePath.Contains("common_alphascale/") && !texturePath.Contains("sfx/") && !texturePath.Contains("liquids/") && !texturePath.Contains("effects/"))
                                    {
                                        shaderNamesOrTextures.Add(texturePath);
                                    }
                                    break; // .ase files can only contain 1 material/bitmap AFAIK
                                }
                            }
                        }
                    }
                    else if (trimmedLine.Contains("{"))
                    {
                        openBrackets++;
                    }
                    else if (trimmedLine.Contains("}"))
                    {
                        closeBrackets++;
                    }
                    else if (openBrackets == closeBrackets && openBrackets != 0)
                    {
                        // Finished parsing
                        openBrackets = 0;
                        closeBrackets = 0;
                        isEntity = false;
                        continue;
                    }
                    else if (Regex.IsMatch(trimmedLine, @"((\w+)\/((\w)+[\/_-]*)*)+"))
                    {
                        string asset = Regex.Match(trimmedLine, @"((\w+)\/((\w)+[\/_-]*)*)+").Value;
                        if (Path.GetExtension(trimmedLine).Equals(".wav\""))
                        {
                            if (!Pk3Maker.soundList.Contains($"{asset}.wav"))
                            {
                                Pk3Maker.soundList.Add($"{asset}.wav");
                            }
                        }
                        if (!shaderNamesOrTextures.Contains(asset) && !asset.Contains("common/") && !asset.Contains("common_alphascale/") && !asset.Contains("sfx/") && !asset.Contains("liquids/") && !asset.Contains("effects/"))
                        {
                            asset = asset.Replace("textures/", ""); // We haven't added /textures yet
                            asset = asset.Replace(".jpg", "").Replace(".tga", ""); // We haven't added extensions yet
                            shaderNamesOrTextures.Add(asset);
                        }
                    }
                }
                if (trimmedLine.Contains("// entity"))
                {
                    isBrush = false;
                    isEntity = true;
                    continue;
                }
                if (trimmedLine.Contains("// brush"))
                {
                    isBrush = true;
                }
            }
            if (Pk3Maker.soundList.Count() > 0)
            {
                Pk3Maker.pk3Structure.Add("sound", Pk3Maker.soundList);
            }
            return shaderNamesOrTextures;
        }

        static List<string> addExtensionsToTextures(List<string> texturesAndShaderNames, List<Shader> shaderNames)
        {
            List<string> list = new List<string>();
            foreach (string textureOrShader in texturesAndShaderNames)
            {
                bool isJpg = false;
                bool isTga = false;
                Shader currentShader = shaderNames.Find(x => x.shaderName.Equals($"textures/{textureOrShader}"));
                if (currentShader != null)
                {
                    // "This is a shader; adding as is to replace later";
                    list.Add($"textures/{textureOrShader}");
                    continue;
                }
                isJpg = File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/textures/{textureOrShader}.jpg");
                isTga = File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/textures/{textureOrShader}.tga");
                if (isJpg)
                {
                    list.Add($"textures/{textureOrShader}.jpg");
                }
                else if (isTga)
                {
                    list.Add($"textures/{textureOrShader}.tga");
                }
            }
            return list;
        }

        static List<string> getShaderFiles(List<string> shaderNamesAndTextures)
        {
            string[] allShaderFiles = Directory.GetFiles($"{Pk3Maker.pathToQuake3}/baseq3/scripts/", "*.shader");
            List<string> shaderFiles = new List<string>();
            List<Shader> stuff = new List<Shader>();
            foreach (string shaderFile in allShaderFiles)
            {
                List<string> list = new List<string>();
                string shader = File.ReadAllText(shaderFile);
                string[] lines = File.ReadAllLines(shaderFile);
                foreach (string shaderName in shaderNamesAndTextures)
                {
                    if (shader.Contains(shaderName))
                    {
                        // We need to loop the file to make sure the shaderName is an actual shader and not a texture
                        // being used by a shader (meaning we discard it)
                        foreach (string line in lines)
                        {
                            if (line.Contains(shaderName) && Pk3Maker.isShaderName(line))
                            {
                                if (!shaderFiles.Contains(shaderFile))
                                {
                                    shaderFiles.Add(shaderFile);
                                }
                                list.Add(shaderName);
                                stuff.Add(new Shader(shaderName, shaderFile, Regex.IsMatch(shaderName, "{")));
                            }
                        }
                    }
                }
            }
            Lookup<string, string> lookupWithShaderNameAsKey = (Lookup<string, string>)stuff.ToLookup(shader => shader.shaderName, shader => shader.shaderFile);
            Lookup<string, string> lookupWithShaderFileAsKey = (Lookup<string, string>)stuff.ToLookup(shader => shader.shaderFile, shader => shader.shaderName);

            foreach (IGrouping<string, string> shaderGroup in lookupWithShaderNameAsKey)
            {
                // Print the key value of the IGrouping.
                Console.WriteLine(shaderGroup.Key);
                // Iterate through each value in the IGrouping and print its value.
                foreach (string str in shaderGroup)
                    Console.WriteLine("    {0}", str);
            }

            foreach (IGrouping<string, string> shaderGroup in lookupWithShaderFileAsKey)
            {
                // Print the key value of the IGrouping.
                Console.WriteLine(shaderGroup.Key);
                // Iterate through each value in the IGrouping and print its value.
                foreach (string str in shaderGroup)
                    Console.WriteLine("    {0}", str);
            }

            return shaderFiles;
        }

        static List<string> removeDuplicateShaders(string topPrioShader)
        {
            List<string> shaders = new List<string>();
            return shaders;
        }

        static List<Shader> getShaderNames(List<string> shaderFiles)
        {
            List<Shader> shaderNames = new List<Shader>();
            foreach (string file in shaderFiles)
            {
                string[] shader = File.ReadAllLines(file);
                foreach (string line in shader)
                {
                    if (Pk3Maker.isShaderName(line))
                    {
                        shaderNames.Add(createShader(line, Path.GetFileName(file)));
                    }

                }
            }
            groupShadersByFile(shaderNames);
            groupShadersByShaderName(shaderNames);
            return shaderNames;
        }

        static List<Shader> groupShadersByFile(List<Shader> shaders)
        {
            var groupedResult = from shader in shaders
                                group shader by shader.shaderFile;

            foreach (var shader in groupedResult)
            {
                Console.WriteLine("Shader file: {0}", shader.Key); //Each group has a key 

                foreach (Shader s in shader) // Each group has inner collection
                    Console.WriteLine("Shader name: {0}", s.shaderName);
            }
            return new List<Shader>();
        }

        static List<Shader> groupShadersByShaderName(List<Shader> shaders)
        {
            var groupedResult = from shader in shaders
                                group shader by shader.shaderName;

            foreach (var shader in groupedResult)
            {
                int count = 0;
                string tempName = "";
                foreach (Shader s in shader) // Each group has inner collection
                {
                    if (count > 0)
                    {
                        Console.WriteLine("Shaders with duplicate shaders");
                        Console.WriteLine("Shader name: {0}", shader.Key); //Each group has a key 
                        Console.WriteLine("Shader file: {0}", tempName);
                        Console.WriteLine("Shader file: {0}", s.shaderFile);
                    }
                    tempName = s.shaderFile;
                    count++;
                }
            }
            return new List<Shader>();
        }

        static bool isShaderName(string line)
        {
            // In case the shader has opening brace on the same line as the shader name, remove it
            line = line.Replace("{", "").Trim();
            // Disregard result if line contains whitespace (meaning it's not a shadername)
            return Regex.IsMatch(line, @"((\w+)\/((\w)+[\/_-]*)*)+") && !Regex.IsMatch(line, @"\s");
        }

        static Shader createShader(string line, string shaderFile)
        {
            bool hasCurlyBrace = false;
            if (line.Contains("{"))
            {
                line = line.Replace("{", "");
                hasCurlyBrace = true;
            }
            string shaderName = Regex.Match(line, @"((\w+)\/((\w)+[\/_-]*)*)+").Value.Trim();
            Shader shader = new Shader(shaderName, shaderFile, hasCurlyBrace);
            return shader;
        }

        static List<Shader> extractAllUsedShaders(List<string> shaderNamesOrTextures, List<Shader> shaderNames)
        {
            List<Shader> usedShaders = new List<Shader>();
            foreach (string shaderNameOrTexture in shaderNamesOrTextures)
            {
                if (Path.GetExtension(shaderNameOrTexture).Equals(""))
                {
                    foreach (Shader shader in shaderNames)
                    {
                        if (shader.shaderName.Equals(shaderNameOrTexture))
                        {
                            usedShaders.Add(shader);
                        }
                    }
                }
            }
            return usedShaders;
        }

        static List<string> texturesFromShaders(List<string> shaders, List<Shader> usedShaders)
        {
            List<string> additionalTextures = new List<string>();
            List<string> env = new List<string>();
            Shader currentShader;
            foreach (string shader in shaders)
            {
                string[] lines = File.ReadAllLines(shader);
                bool shaderInUse = false;
                int openBrackets = 0;
                int closeBrackets = 0;
                foreach (string line in lines)
                {
                    if (!Pk3Maker.isShaderName(line) && !shaderInUse)
                    {
                        continue;
                    }
                    currentShader = usedShaders.Find(x => x.shaderName == line.Replace("{", "").Trim());
                    if (currentShader != null)
                    {
                        Pk3Maker.finalTextureList.Remove(line); // Removing this, as it's being replaced by a texture
                        shaderInUse = true;
                        continue;
                    }
                    if (shaderInUse)
                    {
                        string trimmedLine = line.Trim();
                        if (trimmedLine.Equals("{") || (currentShader != null && currentShader.hasCurlyBraceInName))
                        {
                            openBrackets++;
                            continue;
                        }
                        if (trimmedLine.Equals("}"))
                        {
                            closeBrackets++;
                            continue;
                        }
                        if (openBrackets == closeBrackets && openBrackets != 0)
                        {
                            // End of shader reached
                            // reset state
                            openBrackets = 0;
                            closeBrackets = 0;
                            shaderInUse = false;
                            continue;
                        }
                        if ((!trimmedLine.Contains("skyParms") && Path.GetExtension(trimmedLine).Equals("")) || Path.GetExtension(trimmedLine).Any(char.IsDigit))
                        {
                            // 1. Skyparms doesn't contain extension; don't skip;
                            // 2. Not a texture, skip iteration;
                            // 3. "Extension" is actually coordinates, skipping: e.g ("q3map_sunExt .80 .86 .94 100 300 80 2 32")
                            continue;
                        }
                        if (Regex.IsMatch(trimmedLine, "qer_editorimage"))
                        {
                            // We ignore editorimages
                            continue;
                        }
                        trimmedLine = trimmedLine.Replace("\\", "/");
                        if (trimmedLine.Contains("map ") || trimmedLine.Contains("q3map_lightimage ") || trimmedLine.Contains("clampMap ") || trimmedLine.Contains("animMap ") || trimmedLine.Contains("videoMap ") || trimmedLine.Contains("skyParms "))
                        {
                            if (trimmedLine.Equals("map $lightmap") || trimmedLine.Equals("map $whiteimage") || trimmedLine.Contains("skyParms -") || trimmedLine.Contains("skyParms full"))
                            {
                                continue;
                            }
                            else
                            {
                                string trimmedLineWithoutType = Pk3Maker.removeTextureTypeAndTrim(trimmedLine);
                                if (Pk3Maker.isVanillaTexture(trimmedLineWithoutType))
                                {
                                    continue;
                                }
                                if (additionalTextures.Contains(trimmedLineWithoutType))
                                {
                                    // Already added; skipping
                                    continue;
                                }
                                if (trimmedLineWithoutType.Contains("env/"))
                                {
                                    env = Pk3Maker.addEnvTexture(line);
                                    continue;
                                }
                                string trimmedLineWithoutExtension = Pk3Maker.removeExtensions(trimmedLineWithoutType);
                                string textureWithExtension = Pk3Maker.addCorrectExtension(trimmedLineWithoutExtension);
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
            Pk3Maker.pk3Structure.Add("env", env);
            return additionalTextures;
        }

        private static string removeExtensions(string line)
        {
            return line.Replace(".tga", "").Replace(".jpg", "");
        }

        static string addCorrectExtension(string line)
        {
            bool isJpg = File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{line}.jpg");
            bool isTga = File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{line}.tga");
            if (isJpg)
            {
                return $"{line}.jpg";
            }
            else if (isTga)
            {
                return $"{line}.tga";
            }
            else
            {
                Console.WriteLine($"Could not find texture {line}");
                return "";
            }
        }

        static bool isVanillaTexture(string line)
        {
            return (line.Contains("textures/sfx") || line.Contains("textures/effects") || line.Contains("textures/liquids"));
        }

        static string removeTextureTypeAndTrim(string line)
        {
            line = line.Replace("animMap ", "");
            line = line.Replace("animmap ", "");
            line = line.Replace("clampMap ", "");
            line = line.Replace("clampmap ", "");
            line = line.Replace("videoMap ", "");
            line = line.Replace("videomap ", "");
            line = line.Replace("map ", "");
            line = line.Replace("skyParms ", "");
            line = line.Replace("skyparms ", "");
            line = line.Replace("q3map_lightimage ", "");
            return line.Trim(); // Trim that trimmed shit
        }

        static List<string> addEnvTexture(string line)
        {
            List<string> env = new List<string>();
            string envName = Regex.Match(line, @"(\w+\/\w+)+\s").Value.Trim();
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_bk.tga"))
            {
                env.Add($"{envName}_bk.tga");
            }
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_dn.tga"))
            {
                env.Add($"{envName}_dn.tga");
            }
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_ft.tga"))
            {
                env.Add($"{envName}_ft.tga");
            }
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_lf.tga"))
            {
                env.Add($"{envName}_lf.tga");
            }
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_rt.tga"))
            {
                env.Add($"{envName}_rt.tga");
            }
            if (File.Exists($"{Pk3Maker.pathToQuake3}/baseq3/{envName}_up.tga"))
            {
                env.Add($"{envName}_up.tga");
            }
            return env;
        }
    }
}