using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Codegen.CodegenAttributes;
using Codegen.CodegenAttributes.Bounds;
using Generators.Compressors;
using Generators.Scheme.Command;
using Generators.Scheme.Command.Executor;
using Generators.Scheme.Command.Executor.Interface;
using Generators.Sync.Component;
using Generators.Sync.Feature;
using Generators.Sync.Systems;
using Generators.Sync.Utility;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CodegenExecutor : ScriptableObject
{
#if UNITY_EDITOR
    public string OutputPath = "Generated";

    [ContextMenu("Generate Sync Code")]
    private void GenerateSyncCode()
    {
        #region collect

        var clearPath = Path.Combine(Application.dataPath, OutputPath, "Sync");
        if (Directory.Exists(clearPath))
            Directory.Delete(clearPath, true);

        ushort counter     = 0;
        var    compressors = new List<Tuple<string, float, float, float>>();
        var    components  = new List<Tuple<string, ushort, string[], string[], bool[], bool[]>>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var type in assembly.GetTypes())
        {
            var syncAttributes = type.GetCustomAttributes(typeof(SyncAttribute), false);

            if (syncAttributes != null && syncAttributes.Length > 0)
            {
                var componentName = type.Name;

                var fields       = type.GetFields();
                var fieldTypes   = new List<string>();
                var fieldNames   = new List<string>();
                var isEnums      = new List<bool>();
                var isCompressed = new List<bool>();

                foreach (var field in fields)
                {
                    var compressed = false;
                    switch (field.FieldType.ToString())
                    {
                        case "System.Single":
                        {
                            var compressor = field.GetCustomAttribute<BoundedFloatAttribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}", compressor.Min,
                                    compressor.Max, compressor.Precision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector2":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector2Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector3":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector3Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"{componentName.Replace("Component", "")}{field.Name}Z", compressor.ZMin,
                                    compressor.ZMax, compressor.ZPrecision));
                                compressed = true;
                            }
                        }
                            break;
                    }

                    isEnums.Add(field.FieldType.IsEnum);
                    fieldTypes.Add(field.FieldType.ToString());
                    fieldNames.Add(field.Name);
                    isCompressed.Add(compressed);
                }

                components.Add(new Tuple<string, ushort, string[], string[], bool[], bool[]>(componentName, counter,
                    fieldTypes.ToArray(), fieldNames.ToArray(), isEnums.ToArray(), isCompressed.ToArray()));
                counter++;
            }
            else
            {
                var serializeAttributes = type.GetCustomAttributes(typeof(SerializeAttribute), false);

                if (serializeAttributes != null && serializeAttributes.Length > 0)
                {
                    var componentName = type.Name;

                    var fields       = type.GetFields();
                    var fieldTypes   = new List<string>();
                    var fieldNames   = new List<string>();
                    var isEnums      = new List<bool>();
                    var isCompressed = new List<bool>();

                    foreach (var field in fields)
                    {
                        var compressed = false;
                        switch (field.FieldType.ToString())
                        {
                            case "System.Single":
                            {
                                var compressor = field.GetCustomAttribute<BoundedFloatAttribute>();
                                if (compressor != null)
                                {
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}", compressor.Min,
                                        compressor.Max, compressor.Precision));
                                    compressed = true;
                                }
                            }
                                break;
                            case "UnityEngine.Vector2":
                            {
                                var compressor = field.GetCustomAttribute<BoundedVector2Attribute>();
                                if (compressor != null)
                                {
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                        compressor.XMax, compressor.XPrecision));
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                        compressor.YMax, compressor.YPrecision));
                                    compressed = true;
                                }
                            }
                                break;
                            case "UnityEngine.Vector3":
                            {
                                var compressor = field.GetCustomAttribute<BoundedVector3Attribute>();
                                if (compressor != null)
                                {
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                        compressor.XMax, compressor.XPrecision));
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                        compressor.YMax, compressor.YPrecision));
                                    compressors.Add(new Tuple<string, float, float, float>(
                                        $"{componentName.Replace("Component", "")}{field.Name}Z", compressor.ZMin,
                                        compressor.ZMax, compressor.ZPrecision));
                                    compressed = true;
                                }
                            }
                                break;
                        }

                        isEnums.Add(field.FieldType.IsEnum);
                        fieldTypes.Add(field.FieldType.ToString());
                        fieldNames.Add(field.Name);
                        isCompressed.Add(compressed);
                    }

                    var componentTemplate = new SyncComponentGenerator();
                    var d = new Dictionary<string, object>
                    {
                        {"ComponentName", componentName},
                        {"ComponentId", counter},
                        {"FieldTypes", fieldTypes.ToArray()},
                        {"FieldNames", fieldNames.ToArray()},
                        {"IsEnums", isEnums.ToArray()},
                        {"IsCompressed", isCompressed.ToArray()}
                    };

                    componentTemplate.Session = d;
                    componentTemplate.Initialize();
                    var output = componentTemplate.TransformText();

                    SaveFile("Sync/Components/", $"{componentName}.cs", output);
                    counter++;
                }
            }
        }

        #endregion

        #region components

        foreach (var tuple in components)
        {
            var componentTemplate = new SyncComponentGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentName", tuple.Item1},
                {"ComponentId", tuple.Item2},
                {"FieldTypes", tuple.Item3},
                {"FieldNames", tuple.Item4},
                {"IsEnums", tuple.Item5},
                {"IsCompressed", tuple.Item6}
            };

            componentTemplate.Session = d;
            componentTemplate.Initialize();
            var output = componentTemplate.TransformText();

            SaveFile("Sync/Components/", $"{tuple.Item1}.cs", output);
        }

        #endregion

        #region capture removed components

        foreach (var tuple in components)
        {
            var componentTemplate = new SyncRemovedComponentSystemGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentName", tuple.Item1.Replace("Component", "")},
                {"ComponentId", tuple.Item2},
                {"IsTag", tuple.Item3.Length == 0}
            };

            componentTemplate.Session = d;
            componentTemplate.Initialize();
            var output = componentTemplate.TransformText();

            SaveFile("Sync/Capture/", $"ServerCaptureRemoved{tuple.Item1.Replace("Component", "")}System.cs", output);
        }

        #endregion

        #region capture changed/added components

        foreach (var tuple in components)
        {
            var componentTemplate = new SyncChangedComponentSystemGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentName", tuple.Item1.Replace("Component", "")},
                {"ComponentId", tuple.Item2},
                {"IsTag", tuple.Item3.Length == 0}
            };

            componentTemplate.Session = d;
            componentTemplate.Initialize();
            var output = componentTemplate.TransformText();

            SaveFile("Sync/Capture/", $"ServerCaptureChanged{tuple.Item1.Replace("Component", "")}System.cs", output);
        }

        #endregion

        #region pack entity utility

        {
            var packUtilityTemplate = new PackEntityUtilityGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentNames", components.Select(x => x.Item1).ToArray()},
                {"ComponentIds", components.Select(x => x.Item2).ToArray()},
                {"IsTags", components.Select(x => x.Item3.Length == 0).ToArray()}
            };

            packUtilityTemplate.Session = d;
            packUtilityTemplate.Initialize();
            var output = packUtilityTemplate.TransformText();

            SaveFile("Sync/Utility/", "PackEntityUtility.cs", output);
        }

        #endregion

        #region unpack entity utility

        {
            var template = new UnpackEntityUtilityGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentNames", components.Select(x => x.Item1).ToArray()},
                {"ComponentIds", components.Select(x => x.Item2).ToArray()},
                {"IsTags", components.Select(x => x.Item3.Length == 0).ToArray()}
            };

            template.Session = d;
            template.Initialize();
            var output = template.TransformText();

            SaveFile("Sync/Utility/", "UnpackEntityUtility.cs", output);
        }

        #endregion

        #region feature

        {
            var template = new SyncFeatureGenerator();
            var d = new Dictionary<string, object>
            {
                {"ComponentNames", components.Select(x => x.Item1.Replace("Component", "")).ToArray()}
            };

            template.Session = d;
            template.Initialize();
            var output = template.TransformText();

            SaveFile("Sync/Feature/", "ServerStateCaptureFeature.cs", output);
        }

        #endregion

        #region compressors

        {
            var template = new CompressorsGenerator();
            var d = new Dictionary<string, object>
            {
                {"Prefix", "Sync"},
                {"CompressorNames", compressors.Select(x => x.Item1).ToArray()},
                {"Mins", compressors.Select(x => x.Item2).ToArray()},
                {"Maxs", compressors.Select(x => x.Item3).ToArray()},
                {"Precisions", compressors.Select(x => x.Item4).ToArray()}
            };

            template.Session = d;
            template.Initialize();
            var output = template.TransformText();

            SaveFile("Sync/Compressors/", "SyncCompressors.cs", output);
        }

        #endregion

        AssetDatabase.Refresh();
    }

    [ContextMenu("Generate Command Code")]
    private void GenerateCommandCode()
    {
        #region collect

        var clearPath = Path.Combine(Application.dataPath, OutputPath, "Command");
        if (Directory.Exists(clearPath))
            Directory.Delete(clearPath, true);

        ushort serverCounter  = 0;
        ushort clientCounter  = 0;
        var    compressors    = new List<Tuple<string, float, float, float>>();
        var    serverCommands = new List<Tuple<string, ushort, string[], string[], bool[], bool[]>>();
        var    clientCommands = new List<Tuple<string, ushort, string[], string[], bool[], bool[]>>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var type in assembly.GetTypes())
        {
            var serverAttribute = type.GetCustomAttributes(typeof(CommandToServerAttribute), false);

            if (serverAttribute != null && serverAttribute.Length > 0)
            {
                var name = type.Name.Replace("Scheme", "");

                var fields       = type.GetFields();
                var fieldTypes   = new List<string>();
                var fieldNames   = new List<string>();
                var isEnums      = new List<bool>();
                var isCompressed = new List<bool>();

                foreach (var field in fields)
                {
                    var compressed = false;
                    switch (field.FieldType.ToString())
                    {
                        case "System.Single":
                        {
                            var compressor = field.GetCustomAttribute<BoundedFloatAttribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}", compressor.Min,
                                    compressor.Max, compressor.Precision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector2":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector2Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector3":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector3Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Client{name.Replace("Component", "")}{field.Name}Z", compressor.ZMin,
                                    compressor.ZMax, compressor.ZPrecision));
                                compressed = true;
                            }
                        }
                            break;
                    }

                    isEnums.Add(field.FieldType.IsEnum);
                    fieldTypes.Add(field.FieldType.ToString());
                    fieldNames.Add(field.Name);
                    isCompressed.Add(compressed);
                }

                clientCommands.Add(new Tuple<string, ushort, string[], string[], bool[], bool[]>(name, clientCounter,
                    fieldTypes.ToArray(), fieldNames.ToArray(), isEnums.ToArray(), isCompressed.ToArray()));
                clientCounter++;
            }

            var clientAttribute = type.GetCustomAttributes(typeof(CommandToClientAttribute), false);

            if (clientAttribute != null && clientAttribute.Length > 0)
            {
                var name = type.Name.Replace("Scheme", "");

                var fields       = type.GetFields();
                var fieldTypes   = new List<string>();
                var fieldNames   = new List<string>();
                var isEnums      = new List<bool>();
                var isCompressed = new List<bool>();

                foreach (var field in fields)
                {
                    var compressed = false;
                    switch (field.FieldType.ToString())
                    {
                        case "System.Single":
                        {
                            var compressor = field.GetCustomAttribute<BoundedFloatAttribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}", compressor.Min,
                                    compressor.Max, compressor.Precision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector2":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector2Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressed = true;
                            }
                        }
                            break;
                        case "UnityEngine.Vector3":
                        {
                            var compressor = field.GetCustomAttribute<BoundedVector3Attribute>();
                            if (compressor != null)
                            {
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}X", compressor.XMin,
                                    compressor.XMax, compressor.XPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}Y", compressor.YMin,
                                    compressor.YMax, compressor.YPrecision));
                                compressors.Add(new Tuple<string, float, float, float>(
                                    $"Server{name}{field.Name}Z", compressor.ZMin,
                                    compressor.ZMax, compressor.ZPrecision));
                                compressed = true;
                            }
                        }
                            break;
                    }

                    isEnums.Add(field.FieldType.IsEnum);
                    fieldTypes.Add(field.FieldType.ToString());
                    fieldNames.Add(field.Name);
                    isCompressed.Add(compressed);
                }

                serverCommands.Add(new Tuple<string, ushort, string[], string[], bool[], bool[]>(name, serverCounter,
                    fieldTypes.ToArray(), fieldNames.ToArray(), isEnums.ToArray(), isCompressed.ToArray()));
                serverCounter++;
            }
        }

        #endregion

        #region execution interfaces

        var serverSystem = new SchemeCommandExecutorInterfaceGenerator();
        var d = new Dictionary<string, object>
        {
            {"Using", "Client"},
            {"Type", "Server"},
            {"SchemeNames", clientCommands.Select(x => x.Item1).ToArray()}
        };

        serverSystem.Session = d;
        serverSystem.Initialize();
        var output = serverSystem.TransformText();

        SaveFile("Command/Execution/", "IServerHandler.cs", output);

        var clientSystem = new SchemeCommandExecutorInterfaceGenerator();
        d = new Dictionary<string, object>
        {
            {"Using", "Server"},
            {"Type", "Client"},
            {"SchemeNames", serverCommands.Select(x => x.Item1).ToArray()}
        };

        clientSystem.Session = d;
        clientSystem.Initialize();
        output = clientSystem.TransformText();

        SaveFile("Command/Execution/", "IClientHandler.cs", output);

        #endregion

        #region executors

        var serverExecutor = new SchemeCommandExecutorGenerator();
        d = new Dictionary<string, object>
        {
            {"Using", "Client"},
            {"Type", "Server"},
            {"SchemeNames", clientCommands.Select(x => x.Item1).ToArray()},
            {"SchemeIds", clientCommands.Select(x => x.Item2).ToArray()}
        };

        serverExecutor.Session = d;
        serverExecutor.Initialize();
        output = serverExecutor.TransformText();

        SaveFile("Command/Execution/", "ServerCommandExecutor.cs", output);

        var clientExecutor = new SchemeCommandExecutorGenerator();
        d = new Dictionary<string, object>
        {
            {"Using", "Server"},
            {"Type", "Client"},
            {"SchemeNames", serverCommands.Select(x => x.Item1).ToArray()},
            {"SchemeIds", serverCommands.Select(x => x.Item2).ToArray()}
        };

        clientExecutor.Session = d;
        clientExecutor.Initialize();
        output = clientExecutor.TransformText();

        SaveFile("Command/Execution/", "ClientCommandExecutor.cs", output);

        #endregion

        #region commands

        foreach (var tuple in serverCommands)
        {
            var componentTemplate = new SchemeCommandGenerator();
            d = new Dictionary<string, object>
            {
                {"Namespace", "Server"},
                {"CommandName", tuple.Item1},
                {"CommandId", tuple.Item2},
                {"FieldTypes", tuple.Item3},
                {"FieldNames", tuple.Item4},
                {"IsEnums", tuple.Item5},
                {"IsCompressed", tuple.Item6}
            };

            componentTemplate.Session = d;
            componentTemplate.Initialize();
            output = componentTemplate.TransformText();

            SaveFile("Command/Server/", $"{tuple.Item1}Command.cs", output);
        }

        foreach (var tuple in clientCommands)
        {
            var componentTemplate = new SchemeCommandGenerator();
            d = new Dictionary<string, object>
            {
                {"Namespace", "Client"},
                {"CommandName", tuple.Item1},
                {"CommandId", tuple.Item2},
                {"FieldTypes", tuple.Item3},
                {"FieldNames", tuple.Item4},
                {"IsEnums", tuple.Item5},
                {"IsCompressed", tuple.Item6}
            };

            componentTemplate.Session = d;
            componentTemplate.Initialize();
            output = componentTemplate.TransformText();

            SaveFile("Command/Client/", $"{tuple.Item1}Command.cs", output);
        }

        #endregion

        #region compressors

        {
            var template = new CompressorsGenerator();
            d = new Dictionary<string, object>
            {
                {"Prefix", "Command"},
                {"CompressorNames", compressors.Select(x => x.Item1).ToArray()},
                {"Mins", compressors.Select(x => x.Item2).ToArray()},
                {"Maxs", compressors.Select(x => x.Item3).ToArray()},
                {"Precisions", compressors.Select(x => x.Item4).ToArray()}
            };

            template.Session = d;
            template.Initialize();
            output = template.TransformText();

            SaveFile("Command/Compressors/", "SyncCompressors.cs", output);
        }

        #endregion

        AssetDatabase.Refresh();
    }

    private void SaveFile(string directory, string fileName, string text)
    {
        try
        {
            var path = Path.Combine(Application.dataPath, OutputPath, directory);
            Directory.CreateDirectory(path);
            path = Path.Combine(path, fileName);
            File.WriteAllText(path, text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while saving file {name}:" + e.Message);
            throw;
        }
    }
#endif
}