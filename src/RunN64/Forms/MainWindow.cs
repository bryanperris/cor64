using System;
using System.Drawing;
using System.Windows.Forms;
using cor64;
using cor64.Mips;
using RunN64.Forms.EmuControls;

namespace RunN64Forms
{
    public class MainWindow : Form
    {
        private DisassemblyView m_DisasmView;

        public MainWindow()
        {
            SuspendLayout();

            m_DisasmView = new DisassemblyView()
            {
                Location = new Point(0, 0),
                Size = new Size(300, 500),
                Dock = System.Windows.Forms.DockStyle.Left
            };

            Controls.Add(m_DisasmView);

            ResumeLayout(false);
        }

        public void AttachSystem(N64System system)
        {
            m_DisasmView.AttachSystem(system.DeviceCPU);
        }
    }
}
