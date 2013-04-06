using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using S_Innovations.C1.AzureManager.ExtensionMethods;
using S_Innovations.C1.AzureManager.Models;
using S_Innovations.C1.AzureManager.TemplateSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace S_Innovations.C1.AzureManager.ViewModels
{


    /// <summary>
    /// DoneCommitting: 2013-04-01 12:16:10 : Done committing files in 16ms
    /// </summary>
    public class LogFoldingStratagy : AbstractFoldingStrategy
    {
        Regex r = new Regex("(.*) : Done committing files in (.*)ms", RegexOptions.Compiled);
        NewFolding lastfold;
        int linenr;
        bool prev;
        int startfold;
       int endfold;

        int n;

        public void UpdateFoldings(FoldingManager fm, IEnumerable<String> linesadded)
        {

            var last = fm.GetNextFolding(lastfold.StartOffset);
            foreach (var line in linesadded)
            {
                 var M = r.Match(line);
                 if (M.Success)
                 {
                     endfold = n + line.Length;
                     if (!prev)
                     {
                         prev = true;
                         startfold = n;
                         lastfold = new NewFolding(startfold, endfold);
                         fm.CreateFolding(startfold, endfold).Title = "FileSync Complete";
                     }
                     else
                     {
                         last.EndOffset = endfold;
                     }
                    
                     
                 }
                 else
                 {
                     if (prev)
                     {
                       
                             lastfold = new NewFolding(startfold, endfold);
                             fm.CreateFolding(startfold, endfold).Title = "FileSync Complete";
                         
                     }
                         
                     prev = false;
                 }

                 linenr++;
                 n += line.Length+1;
            }
        }
   
        public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            
           var folds =  new List<NewFolding>();

            
            linenr = 1;
            prev = false;
            startfold = 0; endfold = 0;
            n = 0;
            var l = document.GetLineByNumber(linenr);
           
            int sep = 0;
           
            do
            {
                int N = l.TotalLength;
                var line = document.GetText(n, N);
                var M = r.Match(line);
                if (M.Success)
                {
                    if (!prev)
                    {
                        prev = true;
                        startfold = n;
                        
                    }
                    endfold = n + l.Length;   

                }
                else
                {

                    if (prev)
                        folds.Add(lastfold=new NewFolding{ StartOffset=startfold, EndOffset= endfold, Name="FileSync Complete", DefaultClosed=true});
                    prev = false;
                    
                   
                }



                linenr++;
                n += N;
            }
            while ((l = l.NextLine) != null);


            return folds;
        }
    }
    public class DeploymentLogViewerViewModel : TabItemViewModel
    {

        public TextEditorWrapper LogViewer { get; set; }
        // LogUpdater = null;
        public TextDocument Document{get;set;}
        public C1Container Container { get; set; }

        ~DeploymentLogViewerViewModel()
        {
            
        }
 
        public DeploymentLogViewerViewModel(C1Container container)
            : base(TabControlType.C1DeploymentLogViewer, "LogViewer")
        {
            LogViewer = new TextEditorWrapper();    
           
            Container = container;
            //Document = new TextDocument();
            //LogUpdater = new TextEditorUpdater(container.LogFolder, LogViewer, Document);
        }
        bool isStarted = false;
        void LogFolder_LogUpdated(object sender, List<string> e)
        {
            if (LogViewer.IsVisual() && isStarted)
            {
                
                LogViewer.Dispatcher.Invoke((UpdateDelegate)update, new object[] { e });
            }else{
                foreach (var s in e)
                    Olds.Enqueue(s);
                }
            
        }

    
        /// <summary>
        /// Should always be raised on the UI THREAD
        /// </summary>
        public override async void RaiseActiveEvent()
        {
            if (!isStarted)
                return;

            while (LogViewer.Dispatcher == null)
                await Task.Delay(500);

        //    LogViewer.Dispatcher.Invoke((UpdateDelegate)update, new object[] {null});
            
            foldingManager = FoldingManager.Install(LogViewer.Element.TextArea);
            fs.UpdateFoldings(foldingManager, LogViewer.Element.Document);
            
            update();

        }
        public delegate void UpdateDelegate(List<string> e);
        Queue<String> Olds = new Queue<string>();
        void update(List<string> e = null)
        {


            if (Olds.Count > 0)
            {
                foreach (var line in Olds)
                {
                    
                    Document.Insert(Document.TextLength, line);
                    Document.Insert(Document.TextLength, "\n");
                  

                }
                // TODO NEED TO WORK ON THIS; TO BUGGY BECAUSE OF SHARED ELEMENTS IN THE VIEW.
               // fs.UpdateFoldings(foldingManager, Olds);
                Olds.Clear();
            }

            if (e != null)
            {
                foreach (var line in e)
                {
                   
                    Document.Insert(Document.TextLength, line);
                    Document.Insert(Document.TextLength, "\n");
                   

                }
                // TODO NEED TO WORK ON THIS; TO BUGGY BECAUSE OF SHARED ELEMENTS IN THE VIEW.
               // fs.UpdateFoldings(foldingManager, e);
            }
           //LogViewer.Element.TextArea.s
          //  LogViewer.Element
              
            
        }
        FoldingManager foldingManager;
            LogFoldingStratagy fs;
        public async Task Start()
        {
            while (LogViewer.Dispatcher == null)
                await Task.Delay(500);
            await LogViewer.Dispatcher.InvokeAsync(() =>
            {
                Document = new TextDocument();
                RaisePropertyChanged("Document");
            });

            var logfolder = await Container.LogFolderLazyAsync.Value;
            logfolder.LogUpdated += LogFolder_LogUpdated;
            Document.Text = logfolder.Text;
            

            
            foldingManager = FoldingManager.Install(LogViewer.Element.TextArea);
            fs = new LogFoldingStratagy();
            fs.UpdateFoldings(foldingManager, LogViewer.Element.Document);
            
             
         
            isStarted = true;
            
            //while (!LogViewer.IsVisual())
           //     await Task.Delay(500);
           // LogUpdater.Start();
        }
        public void Stop()
        {
            Container.LogFolderLazyAsync.Value.Result.LogUpdated -= LogFolder_LogUpdated;
            Document = null;
            isStarted = false;
        }
        public override void RaiseRemoved()
        {
            Stop();
        }
        public override void RaiseDeActiveEvent()
        {
            FoldingManager.Uninstall(foldingManager);
            
        }

    }
}
