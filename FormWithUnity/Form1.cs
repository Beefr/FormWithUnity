using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FormWithUnity
{
    public partial class Form1 : Form
    {
        // the server
        private NCServer myNCServer;
        

        // for testing purpose
        private int position = 0;

        // to resize the unity process
        [DllImport("User32.dll")]
        static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        // to get the unity process
        internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

        // the unity process
        private Process process;
        private IntPtr unityHWND = IntPtr.Zero;

        // to update the richtextbox safely
        private delegate void MyDelegate(string content);

        
        public Form1()
        {
            InitializeComponent();

            try
            {
                // add unity to the panel
                process = new Process();
                process.StartInfo.FileName = "Child.exe";
                process.StartInfo.Arguments = "-parentHWND " + panel1.Handle.ToInt32() + " " + Environment.CommandLine;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                process.WaitForInputIdle();

                // to be able to resize it
                EnumChildWindows(panel1.Handle, WindowEnum, IntPtr.Zero);
                
                unityHWNDLabel.Text = "position = " + position;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ".\nCheck if Container.exe is placed next to Child.exe.");
            }
            
            // starts the server
            myNCServer = new NCServer();
            // add an handler to update the richtextbox
            //myNCServer.giveAccessToRichTextBox(richTextBox1); // can't do that because we can't let an other thread modify the richtextbox (i mean an other than the one that created it)
            myNCServer.OnGetSelected += new NCServer.EventHandler(UpdateRichTextBox); // calls UpdateRichTextBox that invokes the delegate for UpdateRichTextBoxTS (Thread Safe)


            richTextBox1.MouseLeave += new EventHandler(richTextBox1_LostFocus);
            richTextBox1.MouseClick += new MouseEventHandler(richTextBox1_GotFocus);
        }


    
        /// <summary>
        /// modifies the text's characteristics
        /// </summary>
        void UpdateRichTextBoxTS(string content)
        {
            richTextBox1.Text = content;
        }

        /// <summary>
        /// we update the richtextbox
        /// </summary>
        /// <param name="a"></param>
        /// <param name="e"></param>
        void UpdateRichTextBox(object a, Event e)
        {
            // Appels inter-thread-safe https://docs.microsoft.com/fr-fr/dotnet/framework/winforms/controls/how-to-make-thread-safe-calls-to-windows-forms-controls
            // https://www.infoworld.com/article/2996770/how-to-work-with-delegates-in-c.html
            
            this.BeginInvoke(new MyDelegate(UpdateRichTextBoxTS), e.Message); // source: https://docs.microsoft.com/fr-fr/dotnet/api/system.windows.forms.control.begininvoke?view=netframework-4.8

            // as the text changes, it fires an event of textchanged, which sends an update to the clients

            // access problem https://stackoverflow.com/questions/13728872/how-to-access-textbox-from-within-class-file
        }

        
        

        /// <summary>
        /// to resize the unity process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_Resize(object sender, EventArgs e)
        {
            MoveWindow(unityHWND, 0, 0, panel1.Width, panel1.Height, true);
            //ActivateUnityWindow();

        }

        /// <summary>
        /// Close Unity application
        /// </summary>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                myNCServer.ShutDown();

                process.CloseMainWindow();

                Thread.Sleep(1000);
                while (process.HasExited == false)
                    process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /// <summary>
        /// get the unity process 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lparam"></param>
        /// <returns></returns>
        private int WindowEnum(IntPtr hwnd, IntPtr lparam)
        {
            unityHWND = hwnd;
            //ActivateUnityWindow();
            return 0;
        }



        /// <summary>
        /// press the button and u'll send messages and objects or whatever you want
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            myNCServer.SendMessage();
            myNCServer.SendMyObject();

            position = myNCServer.GetPosition();
            unityHWNDLabel.Text = "position = " + position;

            //ActivateUnityWindow();
        }

        /// <summary>
        /// if the text of the richtextbox changed, we tell the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length > 0)
            {
                myNCServer.SendObjectUpdate(richTextBox1.Text);
            }
        }

        /// <summary>
        /// if the mouse is not on the richtextbox anymore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_LostFocus(object sender, EventArgs e)
        {

            HideCaret(richTextBox1.Handle);
            richTextBox1.ReadOnly = true;

            //ActivateUnityWindow();

        }
        
        /// <summary>
        /// if we put the mouse on the richtextbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_GotFocus(object sender, EventArgs e)
        {
            richTextBox1.ReadOnly = false;
        }

        [DllImport("user32.dll", EntryPoint = "HideCaret")]
        private static extern int HideCaret(IntPtr hwnd);

    

        /*
       

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        //private const int WM_ACTIVATE = 0x0006;
        //private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        //private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        
        
        [DllImport("User32")]
        private static extern int ShowWindow(System.IntPtr hwnd, int nCmdShow);
        [DllImport("User32")]
        private static extern int SwitchToThisWindow(System.IntPtr hwnd, bool fUnknown);
        


        /// <summary>
        /// when we want the focus to be on the unity window
        /// </summary>
        private void ActivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
        }

        /// <summary>
        /// get the focus on unity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Activated(object sender, EventArgs e) // see Form1.Designer this.Activated
        {
            ActivateUnityWindow();
        }

        
        /// <summary>
        /// remove the focus on unity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Deactivate(object sender, EventArgs e)
        {
            DeactivateUnityWindow();
        }

        

        /// <summary>
        /// when we don't want the focus on the unity window
        /// </summary>
        private void DeactivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_INACTIVE, IntPtr.Zero);
        }
      
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const Int32 WM_NCLBUTTONCLK = 161;
            const Int32 WM_NCLBUTTONDBLCLK = 163;
            if (m.Msg == WM_NCLBUTTONCLK)
            {
                TitleBarClicked(); // Implement this function and do what you need to do in this function
            }
            if (m.Msg == WM_NCLBUTTONDBLCLK)
            {
                TitlebarDoubleClicked(); // Implement this function and do what you need to do in this function
            }
        }

        private void TitlebarDoubleClicked() {

        }

        private void TitleBarClicked()
        {
           
        }//*/

    }
}
