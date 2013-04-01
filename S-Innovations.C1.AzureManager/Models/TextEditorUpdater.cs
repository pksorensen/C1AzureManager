using ICSharpCode.AvalonEdit.Document;
using S_Innovations.C1.AzureManager.ViewModels;
using System;
using System.Threading;

namespace S_Innovations.C1.AzureManager.Models
{
    public class TextEditorUpdater 
    {



        private LogFolder _logs;
        private TextEditorWrapper _editor;
        private TextDocument _doc;
        public TextEditorUpdater(LogFolder logs, TextEditorWrapper editor, TextDocument doc)
        {
            _logs = logs;
            _editor = editor;
            _doc = doc;
            
           

        }
        public void Start()
        {
            
            _editor.Dispatcher.Invoke(set_init_text);
           
            
        }

        
        private void set_init_text()
        {

            _editor.Text = _logs.Text;
        }
        private void update()
        {
            //foreach (var line in _logs.update())
            //{
            //    _doc.Insert(0, "\n");
            //    _doc.Insert(0, line);

            //}

        }

    }
}
