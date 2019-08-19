using System;
using System.Collections.Generic;
using System.Xml;
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
using System.Threading;
using Dropbox.Api;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.Http;

namespace SampleApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    /*
        Ethan created in 2019/8/18
    */
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        [return: MarshalAs(UnmanagedType.Bool)]
        private void TokenText_GotFocus(object sender, RoutedEventArgs e)
        {
            if(TokenText.Tag.ToString() == "True")
            {
                TokenText.Tag = "False";
                TokenText.Text = "";
            }
        }

        private void TokenText_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(TokenText.Text) && TokenText.Tag.ToString() == "False")
            {
                TokenText.Text = "Enter user secret here";
                TokenText.Tag = "True";
            }
        }

        private void UpdateProdcutInfoBlockHandler(List<String> list) {
            if (list.Count != 4)
                return;
            TextBlock[] block1 = { SN, PN, IO, FW };
            for (int i = 0; i < 4; i++)
                block1[i].Text = String.Format("{0} : {1}", block1[i].Name, list[i]);
        }

        private void RetrieveProductInfo() {

            String url = "https://s3-ap-northeast-1.amazonaws.com/test.storejetcloud.com/product.xml";
            String[] require_s = { "SN", "PN", "IO", "FW" };
            List<String> require = new List<String>(require_s);

            //Get ssl error after calling dropbox API
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate {
                Console.WriteLine("Ignore SSL");
                return true;
            };
            try
            {
                XmlTextReader reader = new XmlTextReader(url);
                /*target xml
                <? xml version = "1.0" encoding = "UTF-8" ?>
                   < product >
                       < SN > D123456 - 7788 </ SN >
                       < PN > TS512GSSD370S </ PN >
                       < IO > SATA3 </ IO >
                       < FW > v2.0 </ FW >
                   </ product >
                */
                //check xml fomat
                if (reader.Read() && reader.NodeType == XmlNodeType.XmlDeclaration)
                {

                    while (reader.Read())
                    {
                        //found product
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                        {
                            //start finding requier info
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "product")
                                    break;

                                if (reader.NodeType == XmlNodeType.Element && require.Contains(reader.Name))
                                {
                                    int index = require.IndexOf(reader.Name);
                                    if (index == -1)
                                        continue;
                                    reader.Read();
                                    require[index] = reader.Value;
                                    Console.WriteLine(String.Format("{0} : {1}", require_s[index], require[index]));
                                }
                            }
                            break;
                        }

                    }
                }
                //invoke Update main UI in Block 1
                Dispatcher.Invoke(new Action<List<String>>(UpdateProdcutInfoBlockHandler), require);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Product Infomation");
            }
        }
        private Task function1_job = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (function1_job == null || function1_job.IsCompleted)
            {
                function1_job = Task.Run(() => RetrieveProductInfo());
                using (WaitingForm wf = new WaitingForm(function1_job))
                {
                    wf.ShowDialog();
                }
            }
        }

        private async Task LoginToDropBoxAsync()
        {
            DropboxCertHelper.InitializeCertPinning();

            //String accessToken = await GetAccessToken();
            String accessToken =  await GetAccessToken();
            
            if (accessToken == null || accessToken.Length == 0) {
                return;
            }

            var httpClient = new HttpClient(new WebRequestHandler { ReadWriteTimeout = 10 * 1000 })
            {
                Timeout = TimeSpan.FromMinutes(1)
            };

            var config = new DropboxClientConfig("SampleApp")
            {
                HttpClient = httpClient
            };

            var client = new DropboxClient(accessToken, config);

            //var full = Task.Run( async ()=> await client.Users.GetCurrentAccountAsync(), token).Result;
            var full = await client.Users.GetCurrentAccountAsync();
            Console.WriteLine("Account id    : {0}", full.AccountId);
            Console.WriteLine("Country       : {0}", full.Country);
            Console.WriteLine("Email         : {0}", full.Email);
            Console.WriteLine("Is paired     : {0}", full.IsPaired ? "Yes" : "No");
            Console.WriteLine("Locale        : {0}", full.Locale);
            Console.WriteLine("Name");
            Console.WriteLine("  Display  : {0}", full.Name.DisplayName);
            Console.WriteLine("  Familiar : {0}", full.Name.FamiliarName);
            Console.WriteLine("  Given    : {0}", full.Name.GivenName);
            Console.WriteLine("  Surname  : {0}", full.Name.Surname);
            Console.WriteLine("  Photo  : {0}", full.ProfilePhotoUrl);


            //invoke Update main UI in Block 2
            Dispatcher.Invoke(new Action<Dropbox.Api.Users.FullAccount>(UpdateAccountInfoBlockHandler), full);
        }
        private void UpdateAccountInfoBlockHandler(Dropbox.Api.Users.FullAccount user)
        {
            if (user == null)
                return;

            //insert user info
            FULLNAME.Text = String.Format("Name : {0}", user.Name.DisplayName);
            EMAIL.Text = String.Format("Email : {0}", user.Email);

            //Create image
            Image img = new Image();
            Uri imgUri = new Uri(user.ProfilePhotoUrl);
            img.Source = new BitmapImage(imgUri);
            PIC.Child = img;
            PIC.UpdateLayout();
            this.Activate();
        }
        private async Task<String> GetAccessToken() {
            String accessToken = null;

            try
            {
                Uri oauthUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, _appKey, RedirectUri, state: state);
                http.Prefixes.Add(localhost);

                http.Start();

                //begin to login
                System.Diagnostics.Process.Start(oauthUri.ToString());

                // Handle OAuth redirect and send URL fragment to local server using JS.
                await HandleOAuth2Redirect();

                // Handle redirect from JS and process OAuth response.
                OAuth2Response resp = await HandleJSRedirect();

                //SetForegroundWindow(this.han);
                //http.Stop();
                //http.Close();
                accessToken = resp.AccessToken;
            }
            catch (Exception ex)
            {
                String errMsg = String.Format("Get Access Token Error : {0}", ex.Message);
                Console.WriteLine(errMsg);
                MessageBox.Show(errMsg, "DropboxAPI");
            }
            finally {
                http.Stop();
                http.Close();
            }

            return accessToken;
        }
        private async Task HandleOAuth2Redirect()
        {
            var context = await http.GetContextAsync();

            // We only care about request to RedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != RedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            context.Response.ContentType = "text/html";

            // Respond with a page which runs JS and sends URL fragment as query string
            // to TokenRedirectUri.
            using (var file = File.OpenRead("index.html"))
            {
                file.CopyTo(context.Response.OutputStream);
            }

            context.Response.OutputStream.Close();
        }

        private async Task<OAuth2Response> HandleJSRedirect()
        {
            var context = await http.GetContextAsync();

            // We only care about request to TokenRedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != JSRedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            String redirectUri = new Uri(context.Request.QueryString["url_with_fragment"]).ToString();

            //retrieve code from redirectUri
            int index = redirectUri.IndexOf("code=");
            String code = redirectUri.Substring(index + 5);
            OAuth2Response response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, _appKey, _appSecret, RedirectUri.ToString());

            return response;
        }
        private Task function2_job = null;
        private const String localhost = "http://localhost:50000/";

        private readonly Uri RedirectUri = new Uri(localhost + "authorize");
        private readonly Uri JSRedirectUri = new Uri(localhost + "token");
        private readonly String _appKey = "kht9ypqs7csaj4l";
        private readonly String _appSecret = "6mp365k86ad85ms";
        private readonly String state = Guid.NewGuid().ToString("N");
        private HttpListener http;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if(function2_job == null || function2_job.IsCompleted)
            {
                http = new HttpListener();
                function2_job = Task.Run(async () => await LoginToDropBoxAsync());
                //function2_job.Start();
                using (WaitingForm wf = new WaitingForm(function2_job, http))
                {
                    wf.ShowDialog();
                }
            }
        }
    }
}
