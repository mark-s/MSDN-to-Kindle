using System;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;

namespace PackageThis.GUI
{
    public partial class SplashForm : Form
    {
        delegate void SetTextCallback(string text);
        delegate void CloseCallback();

        static SplashForm frmSplash = null;
        static Thread splashThread = null;
        
        public SplashForm()  //Constructor
        {
            InitializeComponent();
        }

        static public void Init()
        {
            if (frmSplash == null)
            {
                splashThread = new Thread(new ThreadStart(SplashForm.ShowForm));
                splashThread.IsBackground = true;
                splashThread.SetApartmentState(ApartmentState.STA);
                splashThread.Start();
            }
        }

        static public void Done()
        {
            if (frmSplash != null)
            {
                frmSplash.SafeClose();
                splashThread = null;
                frmSplash = null;
            }
        }

        static private void ShowForm()
        {
            frmSplash = new SplashForm();
            frmSplash.timer1.Enabled = true;
            frmSplash.labelVersion.Text = String.Format("Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Application.Run(frmSplash);
        }

        static public void Status(string text)
        {
            if (frmSplash != null)
                frmSplash.SafeSetText(text);
        }


        #region --- Call Form methods safely inside the thread ---

        private void SafeSetText(string text)
        {
            if (this.statusLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SafeSetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.statusLabel.Text = text;
                this.statusLabel.Update();
            }
        }

        private void SafeClose()
        {
            if (this.statusLabel.InvokeRequired)
            {
                CloseCallback d = new CloseCallback(SafeClose);
                this.Invoke(d, new object[] {});
            }
            else
            {
                this.timer1.Enabled = false;
                this.Close();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            statusLabel.Text = statusLabel.Text + '.';   //basic progress indicator
        }

        #endregion

    }
}
