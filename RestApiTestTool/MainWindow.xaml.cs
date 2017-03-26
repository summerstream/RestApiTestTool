using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Web.Script.Serialization;
using System.Net;
using System.IO;
//using RestSharp;
using System.Threading;
using System.Windows.Threading;
using JsonPrettyPrinterPlus;

namespace RestApiTestTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        int _repeatTimes = 1;
        string _postData = string.Empty;
        string _cookie = string.Empty;
        string _URL = string.Empty;
        bool _useProxy = false;
        private static JavaScriptSerializer serializer = new JavaScriptSerializer();
        private static Config _config;
        CancellationTokenSource _cancellationSource;
        bool _isPost = true;
        ResultType _resultExample;
        int _timeout = 5;
        private static List<Collection> _urls;
        List<string> _category;

        public MainWindow()
        {
            InitializeComponent();
            comboboxUrls.IsEditable = true;
            comboboxUrls.IsTextSearchEnabled = true;
            comboboxUrls.IsTextSearchCaseSensitive = false;
            checkboxPost.IsChecked = true;
            checkboxPretty.IsChecked = false;
            comboboxCategory.IsEditable = true;
            comboboxCategory.IsEnabled = true;
            comboboxCategory.IsTextSearchCaseSensitive = false;
            //test config.cs
            //List<Collection> list= ConfigManager.GetCollections();
            //MessageBox.Show(list.Count().ToString());
            LoadConfig();

        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            _repeatTimes = int.Parse(textboxRepeatTimes.Text);
            _postData = textboxRequestBody.Text.Trim();
            _cookie = textboxRequestCookies.Text.Trim();
            _isPost = checkboxPost.IsChecked ?? true;
            _URL = comboboxUrls.Text.Trim();
            _timeout = int.Parse(textboxTimeout.Text.Trim());
            if (!(_URL.Contains("http://") || _URL.Contains("https://")))
            {
                _URL = "http://" + _URL;
            }

            _useProxy = checkboxProxy.IsChecked ?? false;
            this.progressBar.IsEnabled = true;
            this.progressBar.Maximum = _repeatTimes;
            UpdateProgressBar(0);
            _cancellationSource = new CancellationTokenSource();
            Thread thread = new Thread(new ThreadStart(MethodAsync));
            thread.Start();
        }
        private void MethodAsync()
        {
            List<Task<ResultType>> tasks = new List<Task<ResultType>>();
            for (int i = 0; i < _repeatTimes; i++)
            {
                tasks.Add(Do(_cancellationSource.Token));
            }
            int counter = 0;
            while (counter < _repeatTimes)
            {
                if (_cancellationSource.IsCancellationRequested)
                {
                    UpdateAverageTime(tasks);
                    return;
                }
                counter = 0;
                tasks.ForEach((item) =>
                {
                    if (item.IsCompleted)
                    {
                        counter++;
                    }
                });
                UpdateProgressBar(counter);
                Task<ResultType> result = tasks.Where((m) =>
                {
                    return m.IsCompleted;
                }).FirstOrDefault();
                if (result != null)
                {
                    UpdateResult(result.Result);
                }
                else
                {

                }


                Thread.Sleep(10);
            }
            UpdateAverageTime(tasks);
        }
        private void UpdateAverageTime(List<Task<ResultType>> tasks)
        {
            List<double> times = new List<double>();
            times = tasks.Where(m => m.IsCompleted).Select(m => m.Result.Time).ToList();
            double totalTime = 0;
            times.ForEach(m => totalTime += m);
            string timeStr = "first 200 records:\n";
            times.Take(200).All((n) =>
            {
                timeStr += n + " ";
                return true;
            });

            string time = string.Format("{0}\n平均时间: {1}\n最大时间:{2}", timeStr, (totalTime / times.Count).ToString("F2"), times.Max().ToString("F2"));
            UpdateTime(time);
        }
        private void UpdateTime(string value)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.textboxTime.SetValue(TextBox.TextProperty, value); }, null);
        }


        private void UpdateProgressBar(double value)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.progressBar.SetValue(ProgressBar.ValueProperty, value); }, null);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.txtProgress.SetValue(TextBlock.TextProperty, value.ToString()); }, null);
        }
        private void UpdateResult(ResultType result)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.textboxHead.SetValue(TextBox.TextProperty, result != null ? result.Head : ""); }, null);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.textboxRaw.SetValue(TextBox.TextProperty, result != null ? result.Result : "please wait..."); }, null);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { this.textboxPretty.SetValue(TextBox.TextProperty, result != null ? result.Result.PrettyPrintJson() : "please wait..."); }, null);
        }

        private Task<ResultType> Do(CancellationToken token)
        {
            return Task.Run<ResultType>(() =>
            {
                return SendRequest(_URL, _postData,_cookie);
            }, token);
        }

        private ResultType SendRequest(string url, string postData,string cookies)
        {
            System.Diagnostics.Stopwatch watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            ResultType result = new ResultType();
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = _timeout * 1000;
                //check if using proxy
                if (_useProxy)
                {
                    WebProxy proxy = new WebProxy("hkproxy.cn1.global.xxx.com:8080", true);
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    request.Proxy = proxy;
                }
                if (!string.IsNullOrWhiteSpace(cookies))
                {
                    //request.CookieContainer = new CookieContainer();
                    //var cookieItems = cookies.Split(';');
                    //foreach (var item in cookieItems)
                    //{
                    //    Cookie c = new Cookie() {

                    //    };
                    //    request.CookieContainer.
                    //}
                    request.Headers["Cookie"] = cookies;
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                if (!_isPost)
                {
                    request.Method = "get";
                }
                request.Method = "post";
                request.ContentType = "text/json";
                var cachPolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                request.CachePolicy = cachPolicy;
                request.UseDefaultCredentials = true;
                using (StreamWriter sr = new StreamWriter(request.GetRequestStream()))
                {
                    sr.Write(postData);
                    sr.Flush();
                    sr.Close();
                }

                watcher.Restart();

                var response = (HttpWebResponse)request.GetResponse();

                watcher.Stop();
                result.Time = watcher.ElapsedMilliseconds;
                string head = string.Empty;
                foreach (var key in response.Headers.AllKeys)
                {
                    head += string.Format("{0}:{1}\n", key, response.Headers[key]);
                }
                result.Head = head;
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    string responseBody = sr.ReadToEnd();
                    byte[] buffer = UTF8Encoding.UTF8.GetBytes(responseBody);
                    result.Result = UTF8Encoding.UTF8.GetString(buffer);
                }
                return result;
            }
            catch (WebException ex)
            {
                watcher.Stop();
                return new ResultType
                {
                    Time = watcher.ElapsedMilliseconds,
                    Result = ex.Message,
                    Head = ex.Message
                };
            }
            catch (Exception ex)
            {
                watcher.Stop();
                return new ResultType
                {
                    Time = watcher.ElapsedMilliseconds,
                    Result = ex.Message,
                    Head = ex.Message
                };
            }
            finally
            {

            }
        }
        private void LoadConfig()
        {
            _config = ConfigManager.GetConfig();
            comboboxUrls.IsEnabled = true;
            comboboxCategory.IsEnabled = true;
            _urls = _config.collections;
            _category = _config.collections.Where(m => !string.IsNullOrWhiteSpace(m.Type)).Select(m => m.Type).Distinct().ToList();
            comboboxUrls.ItemsSource = _urls.Select(m => m.Url);
            comboboxCategory.ItemsSource = _category;
        }

        private void urls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboboxUrls.SelectedIndex < 0)
            {
                return;
            }
            var c = _urls[comboboxUrls.SelectedIndex];
            //update textbox post data
            textboxRequestBody.Text = c.Data;
            //update combox category
            if (c.Type != null)
            {
                comboboxCategory.SelectedItem = c.Type.ToString();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            Collection c = new Collection
            {
                Url = comboboxUrls.Text.Trim(),
                Data = textboxRequestBody.Text.Trim(),
                Type = comboboxCategory.Text.Trim()
            };
            //save to config.json file
            ConfigManager.SaveCollection(c);
            //flush the comboboxUrls
            LoadConfig();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (comboboxUrls.SelectedIndex < 0)
            {
                return;
            }
            var c = _urls[comboboxUrls.SelectedIndex];
            if (c != null)
            {
                ConfigManager.DeleteCollection(c);
                LoadConfig();
            }
            comboboxUrls.IsDropDownOpen = true;
            comboboxUrls.ItemsSource = _urls.Select(m => m.Url);
        }

        private void comboboxUrls_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            comboboxUrls.IsDropDownOpen = true;
            if (string.IsNullOrWhiteSpace(comboboxUrls.Text))
            {
                comboboxUrls.ItemsSource = _urls.Select(c => c.Url);
            }
            else
            {
                comboboxUrls.ItemsSource = _urls.Where(c => c.Url.ToLower().Contains(comboboxUrls.Text.ToLower())).Select(c => c.Url);
            }
        }

        private void comboboxUrls_DropDownOpened(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboboxCategory.Text.Trim()))
            {
                comboboxUrls.ItemsSource = _config.collections.Select(m => m.Url);
            }
            else
            {
                comboboxUrls.ItemsSource = _urls.Select(m => m.Url);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationSource.Cancel();
        }

        private void checkboxPretty_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textboxRequestBody.Text))
            {
                if (checkboxPretty.IsChecked ?? false)
                {
                    textboxRequestBody.Text = textboxRequestBody.Text.PrettyPrintJson();
                }
            }
        }

        private void comboboxCategory_DropDownOpened(object sender, EventArgs e)
        {
            LoadConfig();
            LoadCategory();
        }

        private void comboboxCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCategory();
        }
        private void LoadCategory()
        {
            if (comboboxCategory.SelectedIndex > -1)
            {
                _urls = _config.collections.Where(m => m.Type != null).Where(m => m.Type.Equals(_category[comboboxCategory.SelectedIndex])).ToList();
            }
        }
    }
    class ResultType
    {
        public double Time { set; get; }
        public string Result { get; set; }
        public string Head { get; set; }
    }
}
