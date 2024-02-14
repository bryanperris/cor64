using System.Text;
using System;
using System.Collections.Generic;
using Nuke.Common.Tools.DotNet;

public static class DefinesHelper {
    public static DotNetBuildSettings AddDefines(this DotNetBuildSettings settings, Defines defines) {
        StringBuilder defineListBuilder = new StringBuilder();
        int count = 0;

        foreach (var define in defines.DefinfitionList) {
            if (count > 0) defineListBuilder.Append(' ');
            defineListBuilder.Append(define);
            Console.WriteLine("Define: {0}", define);
            count++;
        }

        if (settings.Configuration == "Debug") {
            defineListBuilder.Append(" DEBUG");
        }

        return settings.AddProperty("DefineConstants", defineListBuilder.ToString());
    }
}


public class Defines {
    private readonly List<String> m_CompilerDefines = new();

    public IEnumerable<String> DefinfitionList => m_CompilerDefines;

    public void EnableTestingMode() {
        Define("TESTING");
    }

    public Defines() {
        // Experimental
        // Define("LITTLE_ENDIAN");
        // Define("TIMESTAMPS_IN_LOG");
        // Define("DEBUG_MI");
        // Define("DEBUG_TRACE_LOG");
        // Define("DEBUG_TRACE_LOG_GEN");
        // Define("DEBUG_DMA_CMDS");
        // Define("DEBUG_DMA_HEX");
        // Define("DEBUG_DMA_CMDS_RSP_ONLY");
        // Define("ENABLE_CPU_HOOKS");
        Define("FASTER_VI");
        // Define("PRINT_ELF_SYMBOLS");

        // Little-Endian Host
        Define("HOST_LITTLE_ENDIAN");

        // Big Endian with little-endian ALU
        Define("LITTLE_ENDIAN_EXECUTION");

        Define("SKIP_ULTRA_AUDIO");

        // Define("DEBUG_PIF_COMMANDS");

        // Fast TLB not working
        // Define("FAST_TLB");


        CpuCoprocessorDefines();
        CpuDefines();
        RdpDefines();
        CoreHooks();
    }

    public void Define(String define) {
        m_CompilerDefines.Add(define);
    }

    public void CpuCoprocessorDefines() {
        // Define("DEBUG_COPROCESSOR");
        // Define("DEBUG_CAUSE_REG");
        // Define("DEBUG_INTERRUPTS");
        // Define("VERY_DELAYED_INTERRUPTS");
        // Define("DEBUG_STATUS_REGISTER");
        // Define("FILTER_RCP_INTERRUPTS");
        // Define("DEBUG_INTERRUPTS_PENDING");
        // Define("DEBUG_ERET");
        // Define("DEBUG_EPC");
        // Define("DEBUG_TLB");
        // Define("DEBUG_TLB_TRANSLATE");
    }

    public void CpuDefines() {
        // Define("DEBUG_MIPS_TIMER");
        // Define("HACK_FASTER_TIMER");
        // Define("TRACE_LOG_HALT");
        // Define("SAFE_MEMORY_ACCESS");
        // Define("CPU_PROFILER");
        // Define("CPU_FORCE_32");
        // Define("N64_MEMORY_BOUNDS_CHECK");
        // Define("CPU_CHECK_RESERVED");
        Define("CPU_ALWAYS_64");
        // Define("FPU_DEBUG_INST");
        // Define("DISABLE_TLB_SUPPORT");
        // Define("FORCE_FPU_64");
        // Define("DEBUG_MEMORY_DEVICE_ACCESS");
    }

    public void RdpDefines() {
        // Define("DEBUG_RDP_COMMANDS");
        // Define("DEBUG_RDP_TRI_COMMANDS");
    }

    public void CoreHooks() {

        // Define("ENABLE_ISVIEWER");
        // Define("DEBUG_OS");
    }
}