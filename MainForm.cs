using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace YMH219SpeechApp;

public sealed class MainForm : Form
{
    private readonly SpeechSynthesizer synthesizer = new();
    private SpeechRecognitionEngine? recognizer;
    private readonly RichTextBox input = new();
    private readonly RichTextBox recognized = new();
    private readonly ListView history = new();
    private readonly ComboBox voicePicker = new();
    private readonly TrackBar speed = new();
    private readonly Label status = new();
    private readonly Label speedValue = new();
    private readonly Label characterCount = new();
    private readonly Button speakButton = new();
    private readonly Button listenButton = new();
    private readonly Button stopButton = new();
    private bool listening;

    public MainForm()
    {
        Text = "YMH219 | Speech Studio";
        MinimumSize = new Size(900, 650);
        Size = new Size(1120, 780);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        KeyDown += HandleShortcuts;
        BuildInterface();
        LoadVoices();
        synthesizer.SpeakCompleted += (_, _) => BeginInvoke(() => SetStatus("Hazır", false));
        FormClosing += (_, _) => DisposeSpeech();
    }

    private void BuildInterface()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 18), ColumnCount = 1, RowCount = 3 };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        Controls.Add(shell);

        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(new Label { Text = "Konuşma Atölyesi", Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold), ForeColor = Color.FromArgb(25, 45, 75), AutoSize = true, Location = new Point(0, 0) });
        header.Controls.Add(new Label { Text = "Metni seslendir, konuşmayı yazıya dönüştür.", ForeColor = Color.FromArgb(103, 116, 135), AutoSize = true, Location = new Point(2, 43) });
        var headerBadge = new Label { Text = "YMH219  •  SPEECHAPP", AutoSize = true, BackColor = Color.FromArgb(230, 238, 250), ForeColor = Color.FromArgb(53, 105, 193), Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold), Padding = new Padding(12, 7, 12, 7), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        header.Controls.Add(headerBadge);
        header.Resize += (_, _) => headerBadge.Location = new Point(header.ClientSize.Width - headerBadge.Width, 13);
        shell.Controls.Add(header, 0, 0);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        shell.Controls.Add(main, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 0, 12, 0) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        left.Controls.Add(SectionHeading("METİNDEN SESE", "01"), 0, 0);
        left.Controls.Add(BuildTextCard(), 0, 1);
        left.Controls.Add(BuildSpeechControls(), 0, 2);
        main.Controls.Add(left, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 57));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 43));
        right.Controls.Add(SectionHeading("SESTEN METNE", "02"), 0, 0);
        var speechCard = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(16), BackColor = Color.White, Margin = new Padding(0, 0, 0, 12) };
        AddCardBorder(speechCard);
        speechCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        speechCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var speechActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        listenButton.Text = "●  Dinlemeyi Başlat"; StyleButton(listenButton, true); listenButton.Width = 176; listenButton.Click += StartListening;
        stopButton.Text = "■  Durdur"; StyleButton(stopButton, false); stopButton.Width = 105; stopButton.Enabled = false; stopButton.Click += (_, _) => StopListening("Dinleme durduruldu.");
        var copyResult = new Button { Text = "Kopyala", Width = 90, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(67, 87, 115), BackColor = Color.FromArgb(240, 244, 249), Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0) };
        copyResult.FlatAppearance.BorderSize = 0; copyResult.Click += CopyRecognizedText;
        var clearResult = new Button { Text = "Sonucu sil", Width = 98, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(103, 116, 135), BackColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
        clearResult.FlatAppearance.BorderSize = 0; clearResult.Click += (_, _) => ClearRecognizedText();
        var recognitionTips = new ToolTip();
        recognitionTips.SetToolTip(stopButton, "Kısayol: Esc");
        recognitionTips.SetToolTip(copyResult, "Kısayol: Ctrl + Shift + C");
        speechActions.Controls.Add(listenButton); speechActions.Controls.Add(stopButton); speechActions.Controls.Add(copyResult); speechActions.Controls.Add(clearResult); speechCard.Controls.Add(speechActions, 0, 0);
        recognized.Dock = DockStyle.Fill; recognized.BorderStyle = BorderStyle.None; recognized.Font = new Font("Segoe UI", 10.5f); recognized.ForeColor = Color.FromArgb(37, 49, 68); recognized.ReadOnly = true; recognized.BackColor = Color.White; recognized.Text = "Konuşma tanıma sonucu burada görünecek.";
        speechCard.Controls.Add(recognized, 0, 1);
        right.Controls.Add(speechCard, 0, 1);

        var historyCard = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(14), BackColor = Color.White };
        historyCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); historyCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var historyHeader = new Panel { Dock = DockStyle.Fill };
        historyHeader.Controls.Add(new Label { Text = "TANIMA GEÇMİŞİ", Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 97, 121), AutoSize = true, Location = new Point(0, 2) });
        var exportHistory = new LinkLabel { Text = "Dışa aktar", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(240, 2) };
        exportHistory.LinkClicked += SaveHistory;
        var clearHistory = new LinkLabel { Text = "Geçmişi temizle", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(330, 2) };
        clearHistory.LinkClicked += (_, _) => history.Items.Clear();
        historyHeader.Resize += (_, _) =>
        {
            clearHistory.Left = historyHeader.ClientSize.Width - clearHistory.Width;
            exportHistory.Left = clearHistory.Left - exportHistory.Width - 14;
        };
        historyHeader.Controls.Add(exportHistory); historyHeader.Controls.Add(clearHistory);
        historyCard.Controls.Add(historyHeader, 0, 0);
        history.Dock = DockStyle.Fill; history.View = View.Details; history.FullRowSelect = true; history.BorderStyle = BorderStyle.None; history.HeaderStyle = ColumnHeaderStyle.Nonclickable; history.Columns.Add("Saat", 78); var resultColumn = history.Columns.Add("Tanıma sonucu", 280);
        history.SizeChanged += (_, _) => resultColumn.Width = Math.Max(140, history.ClientSize.Width - history.Columns[0].Width - 8);
        historyCard.Controls.Add(history, 0, 1); AddCardBorder(historyCard); right.Controls.Add(historyCard, 0, 2);
        main.Controls.Add(right, 1, 0);
        var footer = new Panel { Dock = DockStyle.Fill };
        status.Text = "●  Hazır"; status.ForeColor = Color.FromArgb(54, 130, 101); status.AutoSize = true; status.Location = new Point(0, 9); footer.Controls.Add(status);
        var course = new Label { Text = "YMH219  ·  SpeechApp", ForeColor = Color.FromArgb(140, 150, 165), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(footer.Width - 170, 9) }; footer.Resize += (_, _) => course.Left = footer.ClientSize.Width - course.Width; footer.Controls.Add(course);
        shell.Controls.Add(footer, 0, 2);
    }

    private Control BuildTextCard()
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(16), BackColor = Color.White, Margin = new Padding(0) };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddCardBorder(card);
        var top = new Panel { Dock = DockStyle.Fill };
        top.Controls.Add(new Label { Text = "Seslendirilecek metin", Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold), ForeColor = Color.FromArgb(45, 62, 86), AutoSize = true, Location = new Point(0, 4) });
        characterCount.Text = "0 karakter"; characterCount.Font = new Font("Segoe UI", 9); characterCount.ForeColor = Color.FromArgb(135, 148, 166); characterCount.AutoSize = true; characterCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        top.Controls.Add(characterCount); top.Resize += (_, _) => characterCount.Left = top.ClientSize.Width - characterCount.Width;
        card.Controls.Add(top, 0, 0);
        input.Dock = DockStyle.Fill; input.BorderStyle = BorderStyle.None; input.Font = new Font("Segoe UI", 11); input.BackColor = Color.White; input.ForeColor = Color.FromArgb(37, 49, 68); input.AcceptsTab = true; input.Text = "Merhaba! Bu uygulama metni sese, konuşmayı da metne dönüştürür.";
        input.TextChanged += (_, _) => characterCount.Text = $"{input.TextLength:N0} karakter";
        characterCount.Text = $"{input.TextLength:N0} karakter";
        card.Controls.Add(input, 0, 1);
        return card;
    }

    private Control BuildSpeechControls()
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14, 10, 14, 9), BackColor = Color.White, Margin = new Padding(0, 12, 0, 0) };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 33)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        speakButton.Text = "▶  Seslendir"; StyleButton(speakButton, true); speakButton.Width = 137; speakButton.Click += Speak;
        var save = new Button { Text = "↓  Metni Kaydet", Width = 135, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(67, 87, 115), BackColor = Color.FromArgb(240, 244, 249), Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0) }; save.FlatAppearance.BorderSize = 0; save.Click += SaveText;
        var clear = new Button { Text = "Temizle", Width = 78, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(103, 116, 135), BackColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(5, 0, 0, 0) }; clear.FlatAppearance.BorderSize = 0; clear.Click += (_, _) => input.Clear();
        var speechTips = new ToolTip();
        speechTips.SetToolTip(speakButton, "Kısayol: Ctrl + Enter");
        actions.Controls.Add(speakButton); actions.Controls.Add(save); actions.Controls.Add(clear); card.Controls.Add(actions, 0, 0);
        var voiceRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 5, 0, 0) };
        voiceRow.Controls.Add(new Label { Text = "Ses", AutoSize = true, ForeColor = Color.FromArgb(103, 116, 135), Margin = new Padding(0, 5, 8, 0) });
        voicePicker.DropDownStyle = ComboBoxStyle.DropDownList; voicePicker.Width = 210; voicePicker.Height = 27; voicePicker.Margin = new Padding(0, 0, 0, 0); voicePicker.SelectedIndexChanged += (_, _) => { if (voicePicker.SelectedItem is string voiceName) try { synthesizer.SelectVoice(voiceName); } catch (Exception ex) { ShowError("Ses seçilemedi", ex.Message); } };
        voiceRow.Controls.Add(voicePicker); card.Controls.Add(voiceRow, 0, 1);
        var speedRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        speedRow.Controls.Add(new Label { Text = "Konuşma hızı", AutoSize = true, ForeColor = Color.FromArgb(103, 116, 135), Margin = new Padding(0, 7, 8, 0) });
        speed.Minimum = -5; speed.Maximum = 5; speed.Value = 0; speed.TickStyle = TickStyle.None; speed.Width = 155; speed.Height = 30; speed.ValueChanged += (_, _) => speedValue.Text = speed.Value.ToString(CultureInfo.InvariantCulture);
        speedRow.Controls.Add(speed); speedValue.Text = "0"; speedValue.ForeColor = Color.FromArgb(103, 116, 135); speedValue.AutoSize = true; speedValue.Margin = new Padding(5, 7, 0, 0); speedRow.Controls.Add(speedValue); card.Controls.Add(speedRow, 0, 2);
        return card;
    }

    private static Control SectionHeading(string title, string number)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label { Text = number, ForeColor = Color.FromArgb(74, 127, 217), Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold), AutoSize = true, Location = new Point(0, 7) });
        panel.Controls.Add(new Label { Text = title, ForeColor = Color.FromArgb(70, 86, 109), Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold), AutoSize = true, Location = new Point(28, 7) });
        return panel;
    }

    private static void AddCardBorder(Control card)
    {
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(228, 233, 241));
            e.Graphics.DrawRectangle(pen, 0, 0, card.ClientSize.Width - 1, card.ClientSize.Height - 1);
        };
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.Height = 34; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = primary ? Color.FromArgb(53, 105, 193) : Color.FromArgb(239, 243, 249); button.ForeColor = primary ? Color.White : Color.FromArgb(67, 87, 115); button.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold); button.Cursor = Cursors.Hand; button.Margin = new Padding(0);
    }

    private void LoadVoices()
    {
        try
        {
            var voiceNames = synthesizer.GetInstalledVoices().Where(v => v.Enabled).Select(v => v.VoiceInfo.Name).ToArray();
            voicePicker.Items.AddRange(voiceNames);
            if (voicePicker.Items.Count > 0) voicePicker.SelectedIndex = 0;
            if (voicePicker.Items.Count == 0) SetStatus("Sistemde kullanılabilir ses bulunamadı.", true);
        }
        catch { SetStatus("Ses hizmeti kullanılamıyor.", true); }
    }

    private void Speak(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(input.Text)) { ShowError("Metin gerekli", "Seslendirmek için önce metin alanına bir şey yazın."); return; }
        try { synthesizer.SpeakAsyncCancelAll(); synthesizer.Rate = speed.Value; synthesizer.SpeakAsync(input.Text); SetStatus("Metin seslendiriliyor…", false); }
        catch (Exception ex) { ShowError("Seslendirme başlatılamadı", ex.Message); }
    }

    private void HandleShortcuts(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Enter)
        {
            Speak(this, EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape && listening)
        {
            StopListening("Dinleme durduruldu.");
            e.Handled = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            CopyRecognizedText(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void StartListening(object? sender, EventArgs e)
    {
        if (listening) return;
        try
        {
            recognizer ??= CreateRecognizer();
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            listening = true; listenButton.Enabled = false; stopButton.Enabled = true; SetStatus("Mikrofon dinleniyor…", false);
        }
        catch (Exception ex) { SetStatus("Mikrofon veya konuşma tanıma hizmeti kullanılamıyor.", true); ShowError("Dinleme başlatılamadı", "Mikrofonu ve Windows konuşma tanıma dil ayarlarını kontrol edin.\n\n" + ex.Message); }
    }

    private SpeechRecognitionEngine CreateRecognizer()
    {
        var installedRecognizers = SpeechRecognitionEngine.InstalledRecognizers();
        var turkishRecognizer = installedRecognizers.FirstOrDefault(r =>
            string.Equals(r.Culture.Name, "tr-TR", StringComparison.OrdinalIgnoreCase));

        if (turkishRecognizer is null)
        {
            var installedLanguages = installedRecognizers.Count == 0
                ? "Hiçbir Windows konuşma tanıma motoru bulunamadı."
                : "Bulunan diller: " + string.Join(", ", installedRecognizers.Select(r => r.Culture.Name)) + ".";

            throw new InvalidOperationException(
                "Türkçe konuşma tanıma motoru bu bilgisayarda kurulu değil. " + installedLanguages +
                " Windows Ayarları > Zaman ve Dil > Dil ve Bölge bölümünden Türkçe dil seçeneklerini ve konuşma özelliklerini kontrol edin.");
        }

        var engine = new SpeechRecognitionEngine(turkishRecognizer);
        engine.LoadGrammar(new DictationGrammar());
        engine.SetInputToDefaultAudioDevice();
        engine.SpeechRecognized += (_, e) =>
        {
            if (e.Result.Confidence < 0.35 || string.IsNullOrWhiteSpace(e.Result.Text)) return;
            BeginInvoke(() =>
            {
                if (recognized.Text == "Konuşma tanıma sonucu burada görünecek.") recognized.Clear();
                recognized.AppendText((recognized.TextLength > 0 ? Environment.NewLine : "") + e.Result.Text);
                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss")); item.SubItems.Add(e.Result.Text); history.Items.Insert(0, item);
            });
        };
        engine.SpeechRecognitionRejected += (_, _) => { };
        engine.RecognizeCompleted += (_, e) => BeginInvoke(() =>
        {
            listening = false; listenButton.Enabled = true; stopButton.Enabled = false;
            if (e.Error is not null) { SetStatus("Konuşma tanıma hatası.", true); ShowError("Konuşma tanıma hatası", e.Error.Message); }
            else SetStatus("Hazır", false);
        });
        return engine;
    }

    private void StopListening(string message)
    {
        if (recognizer is null || !listening) return;
        try { recognizer.RecognizeAsyncCancel(); SetStatus(message, false); }
        catch (Exception ex) { ShowError("Dinleme durdurulamadı", ex.Message); }
    }

    private void SaveText(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(input.Text)) { ShowError("Kaydedilecek metin yok", "Önce metin alanına içerik yazın."); return; }
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt|Tüm dosyalar (*.*)|*.*", DefaultExt = "txt", FileName = "konusma-metni.txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(dialog.FileName, input.Text); SetStatus("Metin dosyaya kaydedildi.", false); }
        catch (Exception ex) { ShowError("Dosya kaydedilemedi", ex.Message); }
    }

    private void SaveHistory(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (history.Items.Count == 0)
        {
            ShowError("Geçmiş boş", "Dışa aktarmak için önce en az bir konuşma tanınmalı.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt|Tüm dosyalar (*.*)|*.*",
            DefaultExt = "txt",
            FileName = "konusma-gecmisi.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var lines = history.Items.Cast<ListViewItem>().Reverse()
                .Select(item => $"[{item.Text}] {item.SubItems[1].Text}");
            File.WriteAllLines(dialog.FileName, lines);
            SetStatus("Konuşma geçmişi kaydedildi.", false);
        }
        catch (Exception ex)
        {
            ShowError("Geçmiş kaydedilemedi", ex.Message);
        }
    }

    private void CopyRecognizedText(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(recognized.Text) || recognized.Text == "Konuşma tanıma sonucu burada görünecek.")
        {
            ShowError("Kopyalanacak sonuç yok", "Önce mikrofondan bir konuşma tanıyın.");
            return;
        }

        try
        {
            Clipboard.SetText(recognized.Text);
            SetStatus("Tanıma sonucu panoya kopyalandı.", false);
        }
        catch (Exception ex)
        {
            ShowError("Sonuç kopyalanamadı", ex.Message);
        }
    }

    private void ClearRecognizedText()
    {
        recognized.Clear();
        recognized.Text = "Konuşma tanıma sonucu burada görünecek.";
        SetStatus("Sonuç alanı temizlendi.", false);
    }

    private void SetStatus(string text, bool error)
    {
        status.Text = "●  " + text; status.ForeColor = error ? Color.FromArgb(183, 72, 72) : Color.FromArgb(54, 130, 101);
    }

    private void ShowError(string title, string message) => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void DisposeSpeech()
    {
        try { recognizer?.RecognizeAsyncCancel(); recognizer?.Dispose(); } catch { }
        try { synthesizer.SpeakAsyncCancelAll(); synthesizer.Dispose(); } catch { }
    }
}
