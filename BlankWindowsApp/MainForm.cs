
using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlankWindowsApp
{
    public class MainForm : Form
    {
        private readonly TextBox addressBox;
        private readonly Button fetchButton;
        private readonly TextBox contentBox;

        private static readonly HttpClient client = new HttpClient();

        public MainForm()
        {
            Text = "Website Content Reader";
            Size = new Size(900, 600);
            MinimumSize = new Size(500, 350);
            StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(8)
            };

            addressBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Enter website address..."
            };

            fetchButton = new Button
            {
                Text = "دریافت محتوا",
                Dock = DockStyle.Right,
                Width = 120
            };

            fetchButton.Click += FetchButton_Click;
            addressBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    FetchButton_Click(sender, EventArgs.Empty);
                }
            };

            // The button is added first so the address box
            // occupies the remaining space.
            topPanel.Controls.Add(addressBox);
            topPanel.Controls.Add(fetchButton);

            contentBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10),
                BackColor = Color.White
            };

            Controls.Add(contentBox);
            Controls.Add(topPanel);
        }

        private async void FetchButton_Click(
            object? sender, EventArgs e)
        {
            string address = addressBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("لطفاً آدرس سایت را وارد کنید.");
                return;
            }

            if (!address.StartsWith("http://",
                    StringComparison.OrdinalIgnoreCase) &&
                !address.StartsWith("https://",
                    StringComparison.OrdinalIgnoreCase))
            {
                address = "https://" + address;
            }

            if (!Uri.TryCreate(address, UriKind.Absolute,
                    out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("آدرس سایت معتبر نیست.");
                return;
            }

            fetchButton.Enabled = false;
            contentBox.Text = "در حال دریافت محتوا...";

            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync(uri);

                response.EnsureSuccessStatusCode();

                string html = await response.Content
                    .ReadAsStringAsync();

                // Remove scripts and styles before removing HTML tags.
                html = Regex.Replace(
                    html,
                    @"<script\b[^>]*>.*?</script>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

                html = Regex.Replace(
                    html,
                    @"<style\b[^>]*>.*?</style>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

                string text = Regex.Replace(html, "<[^>]+>", " ");
                text = WebUtility.HtmlDecode(text);
                text = Regex.Replace(text, @"[ \t]+", " ");
                text = Regex.Replace(text, @"(\r?\n\s*){3,}", "\r\n\r\n");

                contentBox.Text = text.Trim();
            }
            catch (Exception ex)
            {
                contentBox.Text =
                    "خطا در دریافت محتوای سایت:\r\n\r\n" +
                    ex.Message;
            }
            finally
            {
                fetchButton.Enabled = true;
            }
        }
    }
}
