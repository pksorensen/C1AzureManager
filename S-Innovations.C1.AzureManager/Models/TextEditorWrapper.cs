using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using S_Innovations.C1.AzureManager.MVVM.ElementWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S_Innovations.C1.AzureManager.Models
{
    public class TextEditorWrapper : ControlWrapper<TextEditor>
    {
        public string Text
        {
            get { return Elements.First().Text; }
            set { Elements.First().Text = value; }
        }
        public TextDocument Document
        {
            get { return Elements.First().Document; }
            set { Elements.First().Document = value; }
        }

    }
}
