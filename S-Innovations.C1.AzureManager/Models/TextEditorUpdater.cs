using S_Innovations.C1.AzureManager.ViewModels;
using System;
using System.Threading;

namespace S_Innovations.C1.AzureManager.Models
{
    public class TextEditorUpdater : IDisposable
    {
        private bool disposed = false;
        private Thread renewalThread;


        private LogFolder _logs;
        private TextEditorWrapper _editor;
        public TextEditorUpdater(LogFolder logs, TextEditorWrapper editor)
        {
            _logs = logs;
            _editor = editor;
            _editor.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            _editor.Dispatcher.Invoke(set_init_text);

            renewalThread = new Thread(() =>
            {
                while (!disposed)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    try
                    {

                        if (_editor.IsVisual())
                            _editor.Dispatcher.Invoke(update);
                    }
                    catch (Exception ex)
                    {
                        var x = 5;
                    }
                }

            });
            renewalThread.Start();
        }

        void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (renewalThread != null)
                    {
                        renewalThread.Abort();

                        renewalThread = null;
                    }
                }
                disposed = true;
            }
        }
        private void set_init_text()
        {

            _editor.Text = _logs.Text;
        }
        private void update()
        {
            foreach (var line in _logs.update())
            {
                _editor.Document.Insert(0, "\n");
                _editor.Document.Insert(0, line);

            }

        }

    }
}
