using System;
using System.Windows.Forms;
using System.ServiceModel;
namespace WFAServer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ServiceHost host = null;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainHostForm(ref host));
            if(host != null && host.State == CommunicationState.Opened)
                host.Close();
        }
    }
}