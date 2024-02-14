using System;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System.IO;

namespace genlibs {
    public class Glide64Library : ILibrary
    {
        private readonly static string BASEDIR = Path.Combine(Environment.CurrentDirectory, "glide64");
        private readonly static string OUTDIR = Path.Combine(BASEDIR, "..", "..", "RunN64", "External", "GLide64");

        public Glide64Library()
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Setup(Driver driver)
        {
            if (Directory.Exists(OUTDIR))
                Directory.Delete(OUTDIR, true);

            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            
            options.OutputDir = OUTDIR;

            var module = options.AddModule("GLide64");
            module.SharedLibraryName = "cor64-GLideN64.so";
            module.IncludeDirs.Add(BASEDIR);
            module.IncludeDirs.Add(Path.Combine(BASEDIR, "src"));
            module.IncludeDirs.Add(Path.Combine(BASEDIR, "src", "cor64"));
            module.IncludeDirs.Add(Path.Combine(BASEDIR, "src", "inc"));
            module.Headers.Add("src/cor64/ZilmarGFX_1_3_Cor64.h");

            module.OutputNamespace = "RunN64.External.GLide64";
        }

        public void SetupPasses(Driver driver)
        {
            
        }
    }
}