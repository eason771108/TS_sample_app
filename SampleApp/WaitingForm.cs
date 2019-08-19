using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleApp
{
    public partial class WaitingForm : Form
    {
        private Task Worker { get; set; }
        private HttpListener httpListener { get; set; }
        public WaitingForm(Task job, System.Net.HttpListener http)
        {
            InitializeComponent();

            if (job == null)
                throw new ArgumentNullException();

            Worker = job;
            httpListener = http;
            Application.EnableVisualStyles();
        }

        public WaitingForm(Task job)
        {
            InitializeComponent();

            if (job == null)
                throw new ArgumentNullException();

            Worker = job;
            Application.EnableVisualStyles();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //Task.Factory.StartNew(Worker).ContinueWith( t=> {
            //    this.Close(); }, 
            //    TaskScheduler.FromCurrentSynchronizationContext());
            //Worker.Start();
            Worker.ContinueWith(task=> {
                this.Close(); }, 
                TaskScheduler.FromCurrentSynchronizationContext()
                );
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if(httpListener != null)
                httpListener.Stop(); 
        }
    }
}
