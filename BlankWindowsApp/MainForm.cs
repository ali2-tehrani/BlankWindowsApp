using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BlankWindowsApp
{
    public class MainForm : Form
    {
        private readonly TextBox addressBox;
        private readonly Button fetchButton;
        private readonly Button analyzeButton;
        private readonly TextBox contentBox;
        private readonly DataGridView resultsGrid;

        private static readonly HttpClient client = new HttpClient();

        // Pattern:
        // a3: any text % a2 - b2, c1, a1 - b1,
        // to:b3 c2, CID: any text|
        private const string Pattern =
            @"^\s*(?<a3>[^:\r\n]+?)\s*:\s*.*?%\s*" +
            @"(?<a2>.*?)\s*-\s*(?<b2>.*?)\s*,\s*" +
            @"(?<c1>.*?)\s*,\s*(?<a1>.*?)\s*-\s*" +
            @"(?<b1>.*?)\s*,\s*to:\s*(?<b3>\S+)\s+" +
            @"(?<c2>.*?)\s*,\s*CID:\s*.*?\|\s*$";

        public MainForm()
        {
            Text = "Website Content Reader";
            Size = new Size(1100, 700);
            MinimumSize = new Size(650, 450);
            StartPosition = FormStartPosition.CenterScreen;

            // Top panel: address and buttons
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4)
            };

            topPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            topPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 120));

            topPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 160));

            addressBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "https://example.com"
            };

            fetchButton = new Button
            {
                Text = "دریافت محتوا",
                Dock = DockStyle.Fill
            };

            analyzeButton = new Button
            {
                Text = "استخراج و بررسی الگو",
                Dock = DockStyle.Fill
            };

            fetchButton.Click += FetchButton_Click;
            analyzeButton.Click += AnalyzeButton_Click;

            addressBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    FetchButton_Click(sender, EventArgs.Empty);
                }
            };

            topPanel.Controls.Add(addressBox, 0, 0);
            topPanel.Controls.Add(fetchButton, 1, 0);
            topPanel.Controls.Add(analyzeButton, 2, 0);

            // Website content text box
            contentBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = false,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10),
                BackColor = Color.White
            };

            var contentGroup = new GroupBox
            {
                Text = "محتوای سایت (قابل ویرایش)",
                Dock = DockStyle.Fill,
                Padding = new Padding(6)
            };

            contentGroup.Controls.Add(contentBox);

            // Results table
            resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false
            };

            // Exactly eight columns, in the requested order
            resultsGrid.Columns.Add("a1", "a1");
            resultsGrid.Columns.Add("a2", "a2");
            resultsGrid.Columns.Add("a3", "a3");
            resultsGrid.Columns.Add("b1", "b1");
            resultsGrid.Columns.Add("b2", "b2");
            resultsGrid.Columns.Add("b3", "b3");
            resultsGrid.Columns.Add("c1", "c1");
            resultsGrid.Columns.Add("c2", "c2");

            var resultsGroup = new GroupBox
            {
                Text = "نتایج استخراج (۸ متغیر)",
                Dock = DockStyle.Fill,
                Padding = new Padding(6)
            };

            resultsGroup.Controls.Add(resultsGrid);

            // Split the form into content and results areas
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 330,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            split.Panel1.Controls.Add(contentGroup);
            split.Panel2.Controls.Add(resultsGroup);

            Controls.Add(split);
            Controls.Add(topPanel);
        }

        // Download website content
        private async void FetchButton_Click(
            object? sender, EventArgs e)
        {
            string address = addressBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show(
                    "لطفاً آدرس سایت را وارد کنید.",
                    "آدرس سایت",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!address.StartsWith(
                    "http://",
                    StringComparison.OrdinalIgnoreCase) &&
                !address.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase))
            {
                address = "https://" + address;
            }

            if (!Uri.TryCreate(
                    address,
                    UriKind.Absolute,
                    out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show(
                    "آدرس سایت معتبر نیست.",
                    "خطا",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            fetchButton.Enabled = false;
            analyzeButton.Enabled = false;
            contentBox.Text = "در حال دریافت محتوا...";

            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync(uri);

                response.EnsureSuccessStatusCode();

                string html =
                    await response.Content.ReadAsStringAsync();

                // Remove scripts and styles
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

                // Preserve line boundaries for easier pattern matching
                html = Regex.Replace(
                    html,
                    @"<\s*br\s*/?\s*>|</\s*(p|div|li|tr|h[1-6])\s*>",
                    "\r\n",
                    RegexOptions.IgnoreCase);

                // Remove remaining HTML tags
                html = Regex.Replace(html, "<[^>]+>", " ");

                html = WebUtility.HtmlDecode(html);

                // Normalize line endings and spaces
                html = Regex.Replace(
                    html, @"[ \t]+", " ");

                html = Regex.Replace(
                    html, @"(\r?\n\s*){3,}", "\r\n\r\n");

                contentBox.Text = html.Trim();
                resultsGrid.Rows.Clear();
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
                analyzeButton.Enabled = true;
            }
        }

        // Find matching lines and extract eight variables
        private void AnalyzeButton_Click(
            object? sender, EventArgs e)
        {
            resultsGrid.Rows.Clear();

            string[] lines = contentBox.Text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

            int matchedCount = 0;

            foreach (string line in lines)
            {
                Match match = Regex.Match(
                    line,
                    Pattern,
                    RegexOptions.IgnoreCase);

                if (!match.Success)
                    continue;

                // Column order: a1, a2, a3, b1, b2, b3, c1, c2
                resultsGrid.Rows.Add(
                    match.Groups["a1"].Value.Trim(),
                    match.Groups["a2"].Value.Trim(),
                    match.Groups["a3"].Value.Trim(),
                    match.Groups["b1"].Value.Trim(),
                    match.Groups["b2"].Value.Trim(),
                    match.Groups["b3"].Value.Trim(),
                    match.Groups["c1"].Value.Trim(),
                    match.Groups["c2"].Value.Trim()
                );

                matchedCount++;
            }

            MessageBox.Show(
                matchedCount > 0
                    ? $"تعداد سطرهای منطبق: {matchedCount}"
                    : "هیچ سطری با الگوی موردنظر پیدا نشد.",
                "نتیجه بررسی",
                MessageBoxButtons.OK,
                matchedCount > 0
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);
        }
    }
}
