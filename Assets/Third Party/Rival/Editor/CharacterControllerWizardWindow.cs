using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rival
{
    public class CharacterControllerWizardWindow : EditorWindow
    {
        private TextField generatedCharacterName;
        private TextField generatedNamespaceName;
        private TextField generatedPath;
        private HelpBox generatedFilesHelpBox;
        private Button generateButton;

        private const string _kKinematicTemplatesFolder = "Rival/Templates/Kinematic";

        [MenuItem("Window/Rival/Character Controller Wizard")]
        public static void ShowWindow()
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(CharacterControllerWizardWindow));
            window.titleContent = new GUIContent("Character Controller Wizard");
            window.minSize = new Vector2(250, 250);
        }

        private void OnEnable()
        {
            VisualElement root = rootVisualElement;

            AddSpace(root, 15f);

            generatedCharacterName = new TextField("Character Name");
            generatedCharacterName.value = "MyCharacter";
            generatedCharacterName.RegisterCallback<ChangeEvent<string>>(OnCharacterNameChanged);
            root.Add(generatedCharacterName);

            generatedNamespaceName = new TextField("Namespace (optional)");
            generatedNamespaceName.value = "";
            generatedNamespaceName.RegisterCallback<ChangeEvent<string>>(OnNamespaceNameChanged);
            root.Add(generatedNamespaceName);

            generatedPath = new TextField("Generated Files Path");
            generatedPath.value = "_GENERATED/Rival";
            generatedPath.RegisterCallback<ChangeEvent<string>>(OnGeneratedPathChanged);
            root.Add(generatedPath);

            AddSpace(root, 15f);

            generatedFilesHelpBox = new HelpBox("", HelpBoxMessageType.None);
            generatedPath.RegisterCallback<ChangeEvent<string>>(OnGeneratedPathChanged);
            root.Add(generatedFilesHelpBox);

            AddSpace(root, 15f);

            generateButton = new Button(OnGenerateButtonClicked);
            generateButton.text = "Generate";
            root.Add(generateButton);

            RefreshGeneratedFilesHelpBox();
        }

        public void AddSpace(VisualElement root, float height)
        {
            VisualElement space = new VisualElement();
            space.style.minHeight = height;
            root.Add(space);
        }

        public void OnCharacterNameChanged(ChangeEvent<string> newValue)
        {
            if (string.IsNullOrEmpty(generatedCharacterName.value))
            {
                generatedCharacterName.value = "MyCharacter";
            }
            else
            {
                string tmpName = generatedCharacterName.value;
                PostProcessName(ref tmpName, true);
                generatedCharacterName.value = tmpName;
            }

            RefreshGeneratedFilesHelpBox();
        }

        public void OnNamespaceNameChanged(ChangeEvent<string> newValue)
        {
            if (!string.IsNullOrEmpty(generatedNamespaceName.value))
            {
                string tmpName = generatedNamespaceName.value;
                PostProcessName(ref tmpName, true);
                generatedNamespaceName.value = tmpName;
            }
        }

        public void OnGeneratedPathChanged(ChangeEvent<string> newValue)
        {
            RefreshGeneratedFilesHelpBox();
        }

        public void OnDynamicCharacterChanged(ChangeEvent<bool> newValue)
        {
            RefreshGeneratedFilesHelpBox();
        }

        public void OnGenerateButtonClicked()
        {
            string fullPath = Application.dataPath + "/" + generatedPath.text;
            Directory.CreateDirectory(fullPath);

            HandleFileCodegen(fullPath, _kKinematicTemplatesFolder, "TemplateCharacterComponent", "Component.cs");
            HandleFileCodegen(fullPath, _kKinematicTemplatesFolder, "TemplateCharacterAuthoring", "Authoring.cs");
            HandleFileCodegen(fullPath, _kKinematicTemplatesFolder, "TemplateCharacterSystem", "System.cs");
            HandleFileCodegen(fullPath, _kKinematicTemplatesFolder, "TemplateCharacterProcessor", "Processor.cs");

            AssetDatabase.Refresh();
        }

        public void RefreshGeneratedFilesHelpBox()
        {
            string generatedFileBeginning = "Assets/" + generatedPath.text + "/" + generatedCharacterName.text;

            generatedFilesHelpBox.text =
                "Files to be generated or overwritten: \n" +
                "------------------------------------------------------------- \n";

            generatedFilesHelpBox.text +=
                generatedFileBeginning + "Component.cs \n" +
                generatedFileBeginning + "Authoring.cs \n" +
                generatedFileBeginning + "System.cs \n" +
                generatedFileBeginning + "Processor.cs";
        }

        public void HandleFileCodegen(string outputFolderPath, string templateFolderPath, string templateFileName, string generatedFileSuffix)
        {
            TextAsset templateTextAsset = Resources.Load(Path.Combine(templateFolderPath, templateFileName)) as TextAsset;
            StreamWriter writer = File.CreateText(Path.Combine(outputFolderPath, generatedCharacterName.text + generatedFileSuffix));
            ProcessTemplateText(writer, templateTextAsset, generatedCharacterName.text, generatedNamespaceName.text);
            writer.Close();
        }

        public void PostProcessName(ref string name, bool capitalize)
        {
            name = name.Replace("-", "_");
            name = name.Replace(" ", "_");

            if (capitalize)
            {
                name = name.Substring(0, 1).ToUpper() + name.Substring(1);
            }
        }

        public void ProcessTemplateText(StreamWriter fileWriter, TextAsset templateFileTextAsset, string characterName, string namespaceName)
        {
            bool hasNamespace = !string.IsNullOrEmpty(namespaceName);

            using (StringReader reader = new StringReader(templateFileTextAsset.text))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("//CODEGEN(Namespace)"))
                    {
                        if (hasNamespace)
                        {
                            fileWriter.Write("namespace " + namespaceName + Environment.NewLine);
                        }
                    }
                    else if (line.Contains("//CODEGEN(NamespaceOpen)"))
                    {
                        if (hasNamespace)
                        {
                            fileWriter.Write("{" + Environment.NewLine);
                        }
                    }
                    else if (line.Contains("//CODEGEN(NamespaceClose)"))
                    {
                        if (hasNamespace)
                        {
                            fileWriter.Write("}" + Environment.NewLine);
                        }
                    }
                    else if (line.Contains("CODEGEN(RemoveLine)"))
                    {
                        // skip line
                    }
                    else
                    {
                        line = line.Replace("TemplateCharacter", characterName);

                        // Remove tabs
                        if(!hasNamespace)
                        {
                            int tabLength = 4;
                            if(line.Length > tabLength && line.Substring(0, tabLength) == "    ")
                            {
                                line = line.Substring(tabLength, line.Length - tabLength);
                            }
                        }

                        fileWriter.Write(line + Environment.NewLine);
                    }
                }
            }
        }
    }
}