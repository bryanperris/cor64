using System;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System.IO;

namespace genlibs {
    public class RspLibrary : ILibrary
    {
        private readonly static string BASEDIR = Path.Combine(Environment.CurrentDirectory, "rsp");
        private readonly static string OUTDIR = Path.Combine(BASEDIR, "..", "..", "RunN64", "External", "Rsp");

        public override void Postprocess(Driver driver, ASTContext ctx)
        {

        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            
        }

        public override void Setup(Driver driver)
        {
            if (Directory.Exists(OUTDIR))
                Directory.Delete(OUTDIR, true);

            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            options.OutputDir = OUTDIR;

            var module = options.AddModule("rsp");
            module.SharedLibraryName = "rsp.so";
            module.IncludeDirs.Add(BASEDIR);
            module.Headers.Add("module.h");
            module.Headers.Add("rsp.h");

            module.OutputNamespace = "RunN64.External.Rsp";
        }

        public override void SetupPasses(Driver driver)
        {
            
        }
    }
}