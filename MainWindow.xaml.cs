using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExplorerCloseEventListener
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    using ExplorerCloseEventListener.GetAssemblyFullName;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr _garbageHook;

        private IntPtr _targetWindowHandler = new IntPtr(0);

        public MainWindow()
        {
            InitializeComponent();
            Dictionary<AccessibleEvents, NativeMethods.WinEventProc> events = InitializeWinEventToHandlerMap();

            //Hook window close event - close our HoverContorl on Target window close.
            NativeMethods.WinEventProc eventHandler = new NativeMethods.WinEventProc(events[AccessibleEvents.Destroy].Invoke);
            GCHandle gch = GCHandle.Alloc(eventHandler);

            _garbageHook = NativeMethods.SetWinEventHook(AccessibleEvents.Destroy, AccessibleEvents.Destroy, IntPtr.Zero, eventHandler
            , 0, 0, NativeMethods.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT);
        }

        private void OpenExplorerAndListenClick(object sender, RoutedEventArgs e)
        {
            Process process = Process.Start("explorer.exe");
            process.WaitForInputIdle();

            _targetWindowHandler = Find("explorer.exe", "Libraries");
        }

        private Dictionary<AccessibleEvents, NativeMethods.WinEventProc> InitializeWinEventToHandlerMap()
        {
            Dictionary<AccessibleEvents, NativeMethods.WinEventProc> dictionary = new Dictionary<AccessibleEvents, NativeMethods.WinEventProc>();
            //You can add more events like ValueChanged - for more info please read - 
            //http://msdn.microsoft.com/en-us/library/system.windows.forms.accessibleevents.aspx
            dictionary.Add(AccessibleEvents.Destroy, new NativeMethods.WinEventProc(this.DestroyCallback));

            return dictionary;
        }

        private void DestroyCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {
            //Make sure AccessibleEvents equals to LocationChange and the current window is the Target Window.
            if (accEvent == AccessibleEvents.Destroy && windowHandle.ToInt32() == _targetWindowHandler.ToInt32())
            {
                //Queues a method for execution. The method executes when a thread pool thread becomes available.
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.DestroyHelper));
            }
        }

        private void DestroyHelper(object state)
        {
            MessageBox.Show("Good bye!");
            //Removes an event hook function created by a previous call to 
            NativeMethods.UnhookWinEvent(_garbageHook);

        }

        public static IntPtr Find(string moduleName, string mainWindowTitle)
        {
            //Search the window using Module and Title
            IntPtr WndToFind = NativeMethods.FindWindow(moduleName, mainWindowTitle);
            if (WndToFind.Equals(IntPtr.Zero))
            {
                if (!string.IsNullOrEmpty(mainWindowTitle))
                {
                    //Search window using Title only.
                    WndToFind = NativeMethods.FindWindowByCaption(WndToFind, mainWindowTitle);
                    if (WndToFind.Equals(IntPtr.Zero))
                        return new IntPtr(0);
                }
            }
            return WndToFind;
        }
    }
}
