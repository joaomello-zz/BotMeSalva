using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        Semaphore _pool = new Semaphore(0, 1);
        int i = 0;
        Aula aulaAtual = new Aula();
        List<Aula> aula = new List<Aula>();

        public Form1()
        {
            InitializeComponent();
        }

        public ChromiumWebBrowser chromeBrowser { get; private set; }

        private async void ChromeBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            var t = e.Frame;
            var urlBase = edtUrl.Text.LastIndexOf("/") > 0 ? edtUrl.Text.Remove(edtUrl.Text.LastIndexOf("/")) : "LauraMello";
            if (!t.IsMain && t.Url.IndexOf("fast.player.liquidplatform.com") > -1)
            {
                var res = await t.EvaluateScriptAsync(@"(function () { 
                                regex = /https:\/\/[\S]+?\.mp4/g;
                                str = document.documentElement.innerHTML;
                                m = null;

                                while ((m = regex.exec(str)) !== null) {
                                    // This is necessary to avoid infinite loops with zero-width matches
                                    if (m.index === regex.lastIndex) {
                                        regex.lastIndex++;
                                    }

                                    // The result can be accessed through the `m`-variable.
                                    return m[0];
                                }
                            })();");

                aulaAtual.Url = res.Result.ToString();
                await Task.Run(() => _pool.WaitOne());

                aula.Add(new Aula
                {
                    Titulo = aulaAtual.Titulo,
                    Next = aulaAtual.Next,
                    Url = aulaAtual.Url
                });
                i++;

                if (i < edtQuantidade.Value)
                {
                    chromeBrowser.Load(aulaAtual.Next);
                }
                else
                {
                    var str = new StringBuilder();
                    foreach (var item in aula)
                    {
                        str.AppendLine($@"call curl -k {item.Url} > ""{item.Titulo.RemoveAccents()}.mp4""");
                        System.IO.File.WriteAllText(Path.Combine(textBox1.Text, "teste.bat"), str.ToString());
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(textBox1.Text, "teste.bat"),
                        WorkingDirectory = textBox1.Text
                    }).WaitForExit();
                    File.Delete(Path.Combine(textBox1.Text, "teste.bat"));
                    i = 0;

                    MessageBox.Show("Fim!");
                }
            }
            else if (t.IsMain && t.Url.IndexOf(urlBase) > -1)
            {
                var res = await chromeBrowser.EvaluateScriptAsync(@"(function () { 
                                return document.getElementsByClassName('RobotoBold')[0].innerHTML;
                            })();");

                aulaAtual.Titulo = res.Result.ToString();

                res = await chromeBrowser.EvaluateScriptAsync(@"(function () { 
                                return document.getElementsByClassName('lesson-buttons-right pull-right target-console')[0].getAttribute('href');
                            })();");

                aulaAtual.Next = res.Result?.ToString();
                _pool.Release(1);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            i = 0;
            aula.Clear();
            chromeBrowser.Load(edtUrl.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory;
            settings.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2896.3 Mobile Safari/537.36";
            // Initialize cef with the provided settings
            Cef.Initialize(settings);
            Cef.GetGlobalCookieManager().SetStoragePath("MyCookiePath", true);
            // Create a browser component
            chromeBrowser = new ChromiumWebBrowser("http://mesalva.com");
            chromeBrowser.FrameLoadEnd += ChromeBrowser_FrameLoadEnd;
            //chromeBrowser.RequestHandler = new RequestHandler();
            //chromeBrowser.
            // Add it to the form and fill it to the form window.
            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;
        }
    }
}
