using System.IO;
using System;

namespace RunN64 {
    public static class FileEnv {
        public static string WorkingDir => Environment.CurrentDirectory;
        public static string DumpDir => WorkingDir + Path.DirectorySeparatorChar + "Dump";
        private static string s_CurrentRomPath;

        static FileEnv() {
            CheckDir(DumpDir);
        }

        public static void SetCurrentRomFile(String path) {
            s_CurrentRomPath = path;
        }

        private static void CheckDir(String path) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
        }

        public static string GetRomName() {
            return s_CurrentRomPath switch
            {
                null => throw new InvalidProgramException("A rom path needs to be set first"),
                _ => Path.GetFileNameWithoutExtension(s_CurrentRomPath),
            };
        }

        public static string GetCurrentDumpDir() {
            var path = DumpDir + Path.DirectorySeparatorChar + GetRomName();

            CheckDir(path);

            return path;
        }

        public static string GetDumpPath_RspUcode() {
            var path = GetCurrentDumpDir() + Path.DirectorySeparatorChar + "RSP_UCodes";

            CheckDir(path);

            return path;
        }

        public static FileStream Open_RspUcodeDumpFile(uint address, string hash) {
            var path = GetDumpPath_RspUcode() + Path.DirectorySeparatorChar + String.Format("{0:X8}_{1}.asm", address, hash);

            if (File.Exists(path)) {
                File.Delete(path);
            }

            return File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite);
        }

        public static string GetDumpPath_RawRdpTex() {
            var path = GetCurrentDumpDir() + Path.DirectorySeparatorChar + "RDP_Textures";

            CheckDir(path);

            return path;
        }

        public static FileStream Open_RdpTexDumpFile(uint address, int index, int size) {
            var path = GetDumpPath_RawRdpTex() + Path.DirectorySeparatorChar + String.Format("{0:X8}_{1}_{2}.raw", address, index, size);

            if (File.Exists(path)) {
                File.Delete(path);
            }

            return File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite);
        }
    }
}