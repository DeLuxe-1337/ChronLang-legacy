﻿using ChronIR.IR.Internal;
using ChronIR.IR.Internal.GC;
using ChronIR.IR.Operation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChronIR
{
    public enum ChronModuleCompile
    {
        Compile
    }
    public class ChronModule
    {
        private ChronContext CurrentContext;
        private ChronContext Context;
        private List<ChronStatement> Statements = new();
        public ChronModule(ChronContext context)
        {
            Context = context;

            CurrentContext = context;
            Initialize();
        }

        public void DefineCompilerInfo(string name, string value)
        {
            CurrentContext.writer.WriteLine($"#define {name} {value}");
        }
        public void DefineInclusion(string name)
        {
            CurrentContext.writer.WriteLine($"#include \"{name}\"");
        }
        public void SetupChronRuntime()
        {
            DefineCompilerInfo("CHRON_DEBUG", CurrentContext.BuildMode == BuildModeOption.Debug ? "1" : "0");
            DefineCompilerInfo("CHRON_MODULE_NAME", $"\"{CurrentContext.Name}\"");
            DefineInclusion("Backend/include.h");
        }
        internal void Initialize()
        {
            CurrentContext.env.AddScope(new("Root"));

            CurrentContext.writer = new Writer($"{CurrentContext.Name}.chron.c");

            CurrentContext.writer.WriteLine($"/*\n\n\tThis is a ChronScript module generated by ChronIR(0.1)\n" +
                $"\t->\tModule Name: {CurrentContext.Name}\n" +
                $"\t->\tBuild Mode: {CurrentContext.BuildMode} \n" +
                $"\n*/");
        }
        public void AddStatement(ChronStatement statement) => Statements.Add(statement);
        public void Write()
        {
            foreach(var statement in Statements)
            {

                statement.Write(CurrentContext);
            }
            CurrentContext.End();
        }

        // void Compile(string) needs to be cleaned up

        private static string RootDirectory = AppContext.BaseDirectory;
        private static string WorkingDirectory = Environment.CurrentDirectory;
        public void Compile(string compiler = "Backend\\compile")
        {
            string sourceFilePath = Path.Combine(WorkingDirectory, $"{CurrentContext.Name}.chron.c");
            string targetFilePath = Path.Combine(RootDirectory, $"{CurrentContext.Name}.chron.c");

            string compiledExecutable = Path.Combine(RootDirectory, $"{CurrentContext.Name}.chron.exe");
            string outputPath = Path.Combine(WorkingDirectory, $"{CurrentContext.Name}.chron.exe");

            {
                if (!File.Exists(targetFilePath))
                    File.Copy(sourceFilePath, targetFilePath);

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                if (File.Exists(compiledExecutable))
                    File.Delete(compiledExecutable);
            }

            {
                Directory.SetCurrentDirectory(RootDirectory);

                Process.Start($"{compiler}.bat", $"{CurrentContext.Name}.chron").WaitForExit();

                Directory.SetCurrentDirectory(WorkingDirectory);
            }

            {
                if (File.Exists(compiledExecutable) && !File.Exists(outputPath))
                    File.Copy(compiledExecutable, outputPath);

                if (File.Exists(compiledExecutable) && compiledExecutable != outputPath)
                    File.Delete(compiledExecutable);

                if (File.Exists(targetFilePath) && targetFilePath != sourceFilePath)
                    File.Delete(targetFilePath);
            }
        }
    }
}
