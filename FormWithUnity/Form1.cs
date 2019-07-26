using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormWithUnity
{
    public partial class Form1 : Form
    {
        private NCServer myNCServer;

        private bool firstTime = true;
        private int position = 0;
        private int compteur = 0;

        [DllImport("User32.dll")]
        static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private Process process;
        private IntPtr unityHWND = IntPtr.Zero;

        private const int WM_ACTIVATE = 0x0006;
        private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        public delegate void MyDelegate();
        MyDelegate d;
        private string ObjectContent;

        
        public Form1()
        {
            InitializeComponent();
            d = new MyDelegate(UpdateRichTextBoxTS); // to update the richtextbox thread safe

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
                // Doesn't work for some reason ?!
                //unityHWND = process.MainWindowHandle;
                EnumChildWindows(panel1.Handle, WindowEnum, IntPtr.Zero);
                
                unityHWNDLabel.Text = "position = " + position;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ".\nCheck if Container.exe is placed next to Child.exe.");
            }

            if (firstTime)
            {
                // starts the server
                myNCServer = new NCServer();

                // warm things up for the richtextbox
                //myNCServer.giveAccessToRichTextBox(richTextBox1); // can't do that because we can't let an other thread modify the richtextbox (i mean an other than the one that created it)
                myNCServer.OnClickMade += new NCServer.EventHandler(UpdateRichTextBox);
                firstTime = false;
            }


            // add handlers to make it work properly
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(keyPress); 
            this.panel1.Focus();

            richTextBox1.MouseLeave += new EventHandler(richTextBox1_LostFocus);
            richTextBox1.MouseClick += new MouseEventHandler(richTextBox1_GotFocus);
        }

        /// <summary>
        /// for example purpose, myCamera.cs in the client's code does the trick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void keyPress(object sender, KeyPressEventArgs e)
        {
            if (!firstTime)
            {

                switch (e.KeyChar)
                {
                    case 'z':
                        myNCServer.Top(1);
                        break;
                    case 'q':
                        myNCServer.Left(1);
                        break;
                    case 's':
                        myNCServer.Bot(1);
                        break;
                    case 'd':
                        myNCServer.Right(1);
                        break;
                    default:
                        break;

                }
            }
        }

        /// <summary>
        /// modifies the text's caracteristics
        /// </summary>
        public void UpdateRichTextBoxTS()
        {
            richTextBox1.Text = ObjectContent;
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
            
            ObjectContent = e.Message;
            this.Invoke(this.d);

            // access problem https://stackoverflow.com/questions/13728872/how-to-access-textbox-from-within-class-file
        }

        /// <summary>
        /// when we want the focus to be on the unity window
        /// </summary>
        private void ActivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
        }

        /// <summary>
        /// when we don't want the focus on the unity window
        /// </summary>
        private void DeactivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_INACTIVE, IntPtr.Zero);
        }

        /// <summary>
        /// get the unity window and get the focus on it
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lparam"></param>
        /// <returns></returns>
        private int WindowEnum(IntPtr hwnd, IntPtr lparam)
        {
            unityHWND = hwnd;
            ActivateUnityWindow();
            return 0;
        }

        /// <summary>
        /// to resize a panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_Resize(object sender, EventArgs e)
        {
            MoveWindow(unityHWND, 0, 0, panel1.Width, panel1.Height, true);
            ActivateUnityWindow();

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
        /// get the focus on unity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Activated(object sender, EventArgs e)
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
        /// press the button and u'll send messages and objects or whatever you want
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            myNCServer.SendMessage();
            myNCServer.SendMyObject();

            position = myNCServer.GetPosition();
            compteur++;
            unityHWNDLabel.Text = "position = " + position + " (Updated " + compteur + " times)";
            
            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
        }

        [DllImport("User32")]
        private static extern int ShowWindow(System.IntPtr hwnd, int nCmdShow);
        [DllImport("User32")]
        private static extern int SwitchToThisWindow(System.IntPtr hwnd, bool fUnknown);

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

            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);

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
