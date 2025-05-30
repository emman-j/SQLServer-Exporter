using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLServerExporter.Library.Service
{
    public class TextBoxWriter : TextWriter
    {
        private TextBox _textBox;

        public TextBoxWriter(TextBox textBox)
        {
            _textBox = textBox;
        }

        public override void WriteLine(string value)
        {
            if (_textBox.InvokeRequired)
            {
                _textBox.Invoke(new Action(() => _textBox.AppendText(Environment.NewLine + value)));
            }
            else
            {
                _textBox.AppendText(Environment.NewLine + value);
            }
        }
        public override void Write(string value)
        {
            if (_textBox.InvokeRequired)
            {
                _textBox.Invoke(new Action(() => _textBox.AppendText(value)));
            }
            else
            {
                _textBox.AppendText(value);
            }
        }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
