using cor64;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunN64.Forms
{
    public class DebuggerForm : Form
    {
        private N64System m_System;
        ListBox m_CodeView;

        public DebuggerForm()
        {
            SuspendLayout();

            Size = new System.Drawing.Size(500, 500);
            Text = "N64 Debugger";

            PrivateFontCollection collection = new PrivateFontCollection();
            GuiCommon.LoadEmbeddedFont(collection, GuiCommon.FONT_FIRA_REG);

            m_CodeView = new ListBox();
            m_CodeView.Location = new System.Drawing.Point(0, 0);
            m_CodeView.Size = new System.Drawing.Size(200, 450);
            m_CodeView.Font = new System.Drawing.Font(collection.Families[0], 8.0f, System.Drawing.FontStyle.Regular);
            Controls.Add(m_CodeView);
            

            ResumeLayout(false);

            MockUi();
        }

        private void MockUi()
        {
            var items = m_CodeView.Items;

            for (int i = 0; i < ComputeCodeSize(); i++ )
            {
                items.Add("test line");
            }
        }

        private int ComputeCodeSize()
        {
            return (int)Math.Floor((float)m_CodeView.Height / m_CodeView.Font.Height);
        }
    }
}
