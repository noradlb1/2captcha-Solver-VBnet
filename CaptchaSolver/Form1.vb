Imports Microsoft.Web.WebView2.WinForms
Imports Microsoft.Web.WebView2.Core
Imports System.Drawing
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports Newtonsoft.Json

Public Class Form1
    ' إعدادات 2Captcha محسنة
    Private Const API_KEY As String = "af0c8a8b7b5c0ec6b7930be"
    Private Const BASE_URL As String = "http://2captcha.com"
    Private Const FAST_CHECK_INTERVAL As Integer = 5000 ' 5 ثواني بدلاً من 10
    Private Const MAX_CONCURRENT_REQUESTS As Integer = 3 ' طلبات متوازية

    Private httpClient As HttpClient

    ' كاش للحلول السريعة
    Private recentSolutions As New Dictionary(Of String, String)
    Private solutionCache As New Dictionary(Of String, Date)

    ' متغيرات للإحصائيات
    Private solvedCount As Integer = 0
    Private totalTime As TimeSpan = TimeSpan.Zero
    Private successRate As Double = 0.0

    ' متغيرات العناصر
    Private WebView21 As WebView2
    Private TextBox1 As TextBox ' URL
    Private TextBox2 As TextBox ' Site Key  
    Private Label1 As Label     ' Status
    Private Button1 As Button  ' Navigate
    Private Button2 As Button  ' Solve reCAPTCHA
    Private Button3 As Button  ' Check Balance
    Private Button4 As Button  ' Reload Page
    Private Button5 As Button  ' Multi Solve
    Private Button6 As Button  ' Statistics
    Private CheckBox1 As CheckBox ' Debug Mode
    Private CheckBox2 As CheckBox ' Auto Solve
    Private ProgressBar1 As ProgressBar

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' البحث عن العناصر في النموذج
        FindExistingControls()

        ' إنشاء العناصر إذا لم توجد
        CreateControlsIfNeeded()

        ' تهيئة HTTP Client محسن للسرعة
        httpClient = New HttpClient()
        httpClient.Timeout = TimeSpan.FromMinutes(10)
        httpClient.DefaultRequestHeaders.Add("User-Agent", "reCAPTCHA-Solver/2.0")
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json,text/plain,*/*")

        ' تنظيف الكاش القديم
        CleanOldCache()

        ' تهيئة WebView
        InitializeWebView()

        UpdateStatus("⚡ النظام السريع جاهز للعمل", Color.Green)
    End Sub

    Private Sub FindExistingControls()
        ' البحث عن العناصر الموجودة في النموذج
        For Each ctrl As Control In Me.Controls
            Select Case ctrl.Name
                Case "WebView21", "WebView2", "webView"
                    WebView21 = TryCast(ctrl, WebView2)
                Case "TextBox1", "txtUrl"
                    TextBox1 = TryCast(ctrl, TextBox)
                Case "TextBox2", "txtSiteKey"
                    TextBox2 = TryCast(ctrl, TextBox)
                Case "Label1", "lblStatus"
                    Label1 = TryCast(ctrl, Label)
                Case "Button1", "btnNavigate"
                    Button1 = TryCast(ctrl, Button)
                Case "Button2", "btnSolve"
                    Button2 = TryCast(ctrl, Button)
                Case "Button3", "btnBalance"
                    Button3 = TryCast(ctrl, Button)
                Case "Button4", "btnReload"
                    Button4 = TryCast(ctrl, Button)
                Case "CheckBox1", "chkDebug"
                    CheckBox1 = TryCast(ctrl, CheckBox)
            End Select
        Next
    End Sub

    Private Sub CreateControlsIfNeeded()
        ' إنشاء العناصر إذا لم توجد
        If WebView21 Is Nothing Then
            Me.Size = New Size(1200, 800)
            Me.Text = "⚡ reCAPTCHA Solver - نسخة سريعة By MONSTERMC"

            ' WebView
            WebView21 = New WebView2 With {
                .Name = "WebView21",
                .Location = New Point(10, 100),
                .Size = New Size(1160, 600),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            }
            Me.Controls.Add(WebView21)
        End If

        If TextBox1 Is Nothing Then
            TextBox1 = New TextBox With {
                .Name = "TextBox1",
                .Location = New Point(70, 10),
                .Size = New Size(350, 25),
                .Text = "https://2captcha.com/demo/recaptcha-v2"
            }
            Me.Controls.Add(TextBox1)
        End If

        If TextBox2 Is Nothing Then
            TextBox2 = New TextBox With {
                .Name = "TextBox2",
                .Location = New Point(70, 40),
                .Size = New Size(350, 25),
                .ReadOnly = True,
                .BackColor = Color.LightGray
            }
            Me.Controls.Add(TextBox2)
        End If

        If Label1 Is Nothing Then
            Label1 = New Label With {
                .Name = "Label1",
                .Location = New Point(10, 70),
                .Size = New Size(800, 25),
                .Text = "جاري التحميل...",
                .ForeColor = Color.Blue
            }
            Me.Controls.Add(Label1)
        End If

        ' الأزرار
        If Button1 Is Nothing Then
            Button1 = New Button With {
                .Name = "Button1",
                .Location = New Point(430, 10),
                .Size = New Size(70, 25),
                .Text = "انتقل",
                .BackColor = Color.LightBlue
            }
            AddHandler Button1.Click, AddressOf Button1_Click
            Me.Controls.Add(Button1)
        End If

        If Button2 Is Nothing Then
            Button2 = New Button With {
                .Name = "Button2",
                .Location = New Point(510, 10),
                .Size = New Size(90, 25),
                .Text = "⚡ حل سريع",
                .BackColor = Color.LightGreen
            }
            AddHandler Button2.Click, AddressOf Button2_Click
            Me.Controls.Add(Button2)
        End If

        If Button3 Is Nothing Then
            Button3 = New Button With {
                .Name = "Button3",
                .Location = New Point(610, 10),
                .Size = New Size(80, 25),
                .Text = "الرصيد",
                .BackColor = Color.Yellow
            }
            AddHandler Button3.Click, AddressOf Button3_Click
            Me.Controls.Add(Button3)
        End If

        If Button4 Is Nothing Then
            Button4 = New Button With {
                .Name = "Button4",
                .Location = New Point(700, 10),
                .Size = New Size(70, 25),
                .Text = "إعادة تحميل",
                .BackColor = Color.Orange
            }
            AddHandler Button4.Click, AddressOf Button4_Click
            Me.Controls.Add(Button4)
        End If

        ' الأزرار الجديدة
        Button5 = New Button With {
            .Name = "Button5",
            .Location = New Point(780, 10),
            .Size = New Size(80, 25),
            .Text = "حل متعدد",
            .BackColor = Color.Purple,
            .ForeColor = Color.White
        }
        AddHandler Button5.Click, AddressOf Button5_Click
        Me.Controls.Add(Button5)

        Button6 = New Button With {
            .Name = "Button6",
            .Location = New Point(870, 10),
            .Size = New Size(70, 25),
            .Text = "إحصائيات",
            .BackColor = Color.DarkBlue,
            .ForeColor = Color.White
        }
        AddHandler Button6.Click, AddressOf Button6_Click
        Me.Controls.Add(Button6)

        ' CheckBoxes
        If CheckBox1 Is Nothing Then
            CheckBox1 = New CheckBox With {
                .Name = "CheckBox1",
                .Location = New Point(430, 40),
                .Size = New Size(100, 25),
                .Text = "وضع التطوير",
                .Checked = True
            }
            Me.Controls.Add(CheckBox1)
        End If

        CheckBox2 = New CheckBox With {
            .Name = "CheckBox2",
            .Location = New Point(540, 40),
            .Size = New Size(90, 25),
            .Text = "حل تلقائي",
            .ForeColor = Color.DarkGreen
        }
        Me.Controls.Add(CheckBox2)

        ' Progress Bar
        ProgressBar1 = New ProgressBar With {
            .Name = "ProgressBar1",
            .Location = New Point(640, 40),
            .Size = New Size(200, 25),
            .Style = ProgressBarStyle.Continuous
        }
        Me.Controls.Add(ProgressBar1)

        ' Labels
        Dim lblUrl As New Label With {
            .Text = "الموقع:",
            .Location = New Point(10, 12),
            .Size = New Size(50, 20)
        }
        Me.Controls.Add(lblUrl)

        Dim lblSiteKey As New Label With {
            .Text = "Site Key:",
            .Location = New Point(10, 42),
            .Size = New Size(50, 20)
        }
        Me.Controls.Add(lblSiteKey)
    End Sub

    Private Async Sub InitializeWebView()
        Try
            Await WebView21.EnsureCoreWebView2Async()

            ' إعدادات متقدمة
            WebView21.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            WebView21.CoreWebView2.Settings.AreDevToolsEnabled = True
            WebView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = True

            ' إضافة معالج تحميل الصفحة
            AddHandler WebView21.CoreWebView2.DOMContentLoaded, AddressOf OnDOMContentLoaded

            UpdateStatus("⚡ WebView2 جاهز بسرعة فائقة", Color.Green)

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في WebView2: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub UpdateStatus(message As String, Optional color As Color = Nothing)
        Try
            If Label1 IsNot Nothing Then
                Label1.Text = $"{DateTime.Now:HH:mm:ss} - {message}"
                If color <> Nothing Then Label1.ForeColor = color
            End If

            ' طباعة في Console إذا كان Debug مفعل
            If CheckBox1 IsNot Nothing AndAlso CheckBox1.Checked Then
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}")
            End If

        Catch ex As Exception
            Console.WriteLine($"Error updating status: {ex.Message}")
        End Try
    End Sub

    ' Button1 - التنقل للموقع
    Private Sub Button1_Click(sender As Object, e As EventArgs)
        Try
            If String.IsNullOrEmpty(TextBox1.Text) Then
                UpdateStatus("⚠️ يرجى إدخال رابط الموقع", Color.Red)
                Return
            End If

            WebView21.CoreWebView2.Navigate(TextBox1.Text)
            UpdateStatus("🌐 جاري التنقل للموقع...", Color.Blue)

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في التنقل: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Async Sub OnDOMContentLoaded(sender As Object, e As CoreWebView2DOMContentLoadedEventArgs)
        Try
            UpdateStatus("📄 تم تحميل الصفحة، جاري استخراج Site Key...", Color.Blue)

            ' انتظار قصير للتأكد من تحميل العناصر
            Await Task.Delay(1500) ' تقليل من 2000 لـ 1500

            ' استخراج Site Key
            Await ExtractSiteKey()

            ' حل تلقائي إذا كان مفعل
            If CheckBox2 IsNot Nothing AndAlso CheckBox2.Checked Then
                If Not String.IsNullOrEmpty(TextBox2.Text) AndAlso TextBox2.Text <> "غير موجود" Then
                    UpdateStatus("🤖 بدء الحل التلقائي...", Color.Blue)
                    Button2_Click(Nothing, Nothing)
                End If
            End If

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في معالجة الصفحة: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Async Function ExtractSiteKey() As Task
        Dim script As String = "
            (() => {
                let siteKey = '';
                console.log('⚡ Starting fast Site Key extraction...');
                
                // طرق سريعة لاستخراج Site Key
                const methods = [
                    () => document.querySelector('[data-sitekey]')?.getAttribute('data-sitekey'),
                    () => {
                        const iframe = document.querySelector('iframe[src*=recaptcha]');
                        if (iframe) {
                            const match = iframe.src.match(/k=([^&]+)/);
                            return match ? match[1] : null;
                        }
                    },
                    () => {
                        const scripts = Array.from(document.querySelectorAll('script'));
                        for (let script of scripts) {
                            const match = script.innerHTML.match(/6L[a-zA-Z0-9_-]{38,42}/);
                            if (match) return match[0];
                        }
                    }
                ];
                
                for (let i = 0; i < methods.length; i++) {
                    try {
                        const key = methods[i]();
                        if (key && key.length > 10 && key.startsWith('6L')) {
                            siteKey = key;
                            console.log(`⚡ Method ${i + 1} found sitekey:`, siteKey);
                            break;
                        }
                    } catch(e) {
                        console.error(`Method ${i + 1} error:`, e);
                    }
                }
                
                return siteKey || 'غير موجود';
            })();
        "

        Try
            Dim result As String = Await WebView21.CoreWebView2.ExecuteScriptAsync(script)
            Dim siteKey As String = If(result IsNot Nothing, result.Trim(""""c), "")

            If Not String.IsNullOrEmpty(siteKey) AndAlso siteKey <> "غير موجود" Then
                TextBox2.Text = siteKey
                UpdateStatus($"✅ Site Key: {siteKey}", Color.Green)
            Else
                UpdateStatus("❌ لم يتم العثور على Site Key", Color.Red)
                UpdateStatus("💡 جرب F12 والبحث عن data-sitekey", Color.Blue)
            End If

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في استخراج Site Key: {ex.Message}", Color.Red)
        End Try
    End Function

    ' Button2 - الحل السريع المحسن
    Private Async Sub Button2_Click(sender As Object, e As EventArgs)
        Try
            Button2.Enabled = False
            Button2.Text = "⚡ جاري..."
            Dim startTime As DateTime = DateTime.Now

            ' التحقق من المتطلبات
            If String.IsNullOrEmpty(TextBox2.Text) OrElse TextBox2.Text = "غير موجود" Then
                UpdateStatus("❌ لم يتم العثور على Site Key!", Color.Red)
                Return
            End If

            Dim siteKey As String = TextBox2.Text.Trim()
            Dim pageUrl As String = WebView21.CoreWebView2.Source

            ' فحص الكاش أولاً (سرعة فائقة!)
            If CheckSolutionCache(siteKey) Then
                Dim cachedSolution As String = recentSolutions(siteKey)
                UpdateStatus("⚡ حل فوري من الكاش!", Color.Green)
                Await InjectSolution(cachedSolution)
                UpdateStatus("🎉 تم الحل من الكاش في أقل من ثانية!", Color.Green)
                Return
            End If

            ' فحص الرصيد السريع
            Dim hasBalance As Boolean = Await CheckBalanceFast()
            If Not hasBalance Then
                UpdateStatus("❌ رصيد غير كافٍ!", Color.Red)
                Return
            End If

            UpdateStatus($"🚀 بدء الحل السريع المتوازي", Color.Blue)
            ProgressBar1.Value = 25

            ' الحل المتوازي السريع
            Dim solution As String = Await SolveWithParallelRequests(siteKey, pageUrl)

            If Not String.IsNullOrEmpty(solution) Then
                Dim elapsedTime As TimeSpan = DateTime.Now - startTime
                UpdateStatus($"⚡ تم الحل في {elapsedTime.TotalSeconds:F1} ثانية!", Color.Green)

                ' حفظ في الكاش
                SaveToCache(siteKey, solution)

                ' حقن الحل
                ProgressBar1.Value = 75
                Await InjectSolution(solution)
                ProgressBar1.Value = 100

                UpdateStatus($"🎉 حل سريع مكتمل! ⚡", Color.Green)

                ' تحديث الإحصائيات
                solvedCount += 1
                totalTime = totalTime.Add(elapsedTime)
                successRate = (solvedCount / Math.Max(solvedCount, 1)) * 100

                ' صوت النجاح
                Try
                    Console.Beep(1000, 150)
                    Console.Beep(1500, 150)
                Catch
                End Try
            Else
                UpdateStatus("❌ فشل في الحل السريع", Color.Red)
                ProgressBar1.Value = 0
            End If

        Catch ex As Exception
            UpdateStatus($"❌ خطأ: {ex.Message}", Color.Red)
        Finally
            Button2.Enabled = True
            Button2.Text = "⚡ حل سريع"
            ProgressBar1.Value = 0
        End Try
    End Sub

    ' حل متوازي للسرعة القصوى
    Private Async Function SolveWithParallelRequests(siteKey As String, pageUrl As String) As Task(Of String)
        Try
            UpdateStatus("⚡ إرسال 3 طلبات متوازية...", Color.Blue)

            Dim tasks As New List(Of Task(Of String))

            For i As Integer = 1 To MAX_CONCURRENT_REQUESTS
                tasks.Add(SolveV2Fast(siteKey, pageUrl, i))
                Await Task.Delay(50) ' تأخير أقل بين الطلبات
            Next

            ' انتظار أول حل ناجح
            While tasks.Count > 0
                Dim completedTask = Await Task.WhenAny(tasks)
                Dim result As String = Await completedTask

                If Not String.IsNullOrEmpty(result) Then
                    UpdateStatus("⚡ حصلنا على حل سريع!", Color.Green)
                    Return result
                End If

                tasks.Remove(completedTask)
            End While

            Return Nothing

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في الحل المتوازي: {ex.Message}", Color.Red)
            Return Nothing
        End Try
    End Function

    Private Async Function SolveV2Fast(siteKey As String, pageUrl As String, requestNum As Integer) As Task(Of String)
        Try
            ' بناء البيانات
            Dim postData As String = $"key={API_KEY}&method=userrecaptcha&googlekey={siteKey}&pageurl={Uri.EscapeDataString(pageUrl)}"
            Dim content As New StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")

            ' إرسال سريع
            Dim response = Await httpClient.PostAsync($"{BASE_URL}/in.php", content)
            Dim responseText = Await response.Content.ReadAsStringAsync()

            If responseText.StartsWith("OK|") Then
                Dim requestId As String = responseText.Split("|"c)(1)
                UpdateStatus($"✅ طلب #{requestNum} مرسل! ID: {requestId}", Color.Green)

                ' انتظار سريع
                Return Await WaitForSolutionFast(requestId, requestNum)
            Else
                Return Nothing
            End If

        Catch
            Return Nothing
        End Try
    End Function

    Private Async Function WaitForSolutionFast(requestId As String, requestNum As Integer) As Task(Of String)
        Try
            ' فحص سريع كل 5 ثواني
            For i As Integer = 0 To 60 ' 5 دقائق
                Await Task.Delay(FAST_CHECK_INTERVAL)

                Dim checkUrl As String = $"{BASE_URL}/res.php?key={API_KEY}&action=get&id={requestId}"
                Dim resultResponse = Await httpClient.GetStringAsync(checkUrl)

                If resultResponse.StartsWith("OK|") Then
                    Dim solution As String = resultResponse.Split("|"c)(1)
                    Dim minutes As Double = Math.Round((i + 1) * 5 / 60, 1)
                    UpdateStatus($"⚡ طلب #{requestNum} نجح في {minutes} دقيقة!", Color.Green)
                    Return solution

                ElseIf Not resultResponse.Contains("CAPCHA_NOT_READY") Then
                    Return Nothing
                End If
            Next

            Return Nothing

        Catch
            Return Nothing
        End Try
    End Function

    Private Async Function CheckBalanceFast() As Task(Of Boolean)
        Try
            Dim balanceUrl As String = $"{BASE_URL}/res.php?key={API_KEY}&action=getbalance"
            Dim response As String = Await httpClient.GetStringAsync(balanceUrl)

            If IsNumeric(response) Then
                Dim balance As Double = Double.Parse(response)
                If balance > 0.001 Then
                    UpdateStatus($"✅ رصيد: ${balance:F3}", Color.Green)
                    Return True
                End If
            End If

            Return False
        Catch
            Return False
        End Try
    End Function

    Private Async Function InjectSolution(solution As String) As Task
        Try
            UpdateStatus("💉 حقن الحل...", Color.Blue)

            Dim script As String = $"
                (() => {{
                    try {{
                        const token = '{solution}';
                        
                        // ملء textarea
                        const textareas = document.querySelectorAll('textarea[name=g-recaptcha-response]');
                        textareas.forEach(textarea => {{
                            textarea.value = token;
                            textarea.innerHTML = token;
                        }});
                        
                        // تحديث grecaptcha
                        if (window.grecaptcha) {{
                            window.grecaptcha.getResponse = function() {{ return token; }};
                        }}
                        
                        // تشغيل callbacks
                        const elements = document.querySelectorAll('.g-recaptcha, [data-sitekey]');
                        elements.forEach(element => {{
                            const callback = element.getAttribute('data-callback');
                            if (callback && window[callback]) {{
                                window[callback](token);
                            }}
                        }});
                        
                        // تفعيل الأزرار
                        const buttons = document.querySelectorAll('button, input[type=submit]');
                        buttons.forEach(btn => {{
                            btn.disabled = false;
                            btn.removeAttribute('disabled');
                        }});
                        
                        return 'success';
                        
                    }} catch(e) {{
                        return 'error: ' + e.message;
                    }}
                }})();
            "

            Dim result As String = Await WebView21.CoreWebView2.ExecuteScriptAsync(script)
            UpdateStatus($"✅ حقن مكتمل: {result?.Trim(""""c)}", Color.Green)

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في الحقن: {ex.Message}", Color.Red)
        End Try
    End Function

    ' Button3 - فحص الرصيد
    Private Async Sub Button3_Click(sender As Object, e As EventArgs)
        Try
            Button3.Enabled = False
            UpdateStatus("💰 فحص الرصيد...", Color.Blue)

            Dim balanceUrl As String = $"{BASE_URL}/res.php?key={API_KEY}&action=getbalance"
            Dim response As String = Await httpClient.GetStringAsync(balanceUrl)

            If IsNumeric(response) Then
                Dim balance As Double = Double.Parse(response)
                UpdateStatus($"💰 رصيدك: ${balance:F4} دولار", Color.Green)

                Dim v2Solutions As Integer = Math.Floor(balance / 0.002)
                UpdateStatus($"📊 يمكنك حل ~{v2Solutions} reCAPTCHA", Color.Blue)

                If balance < 0.01 Then
                    UpdateStatus("⚠️ رصيد منخفض!", Color.Orange)
                End If
            Else
                UpdateStatus($"❌ خطأ: {response}", Color.Red)
            End If

        Catch ex As Exception
            UpdateStatus($"❌ خطأ: {ex.Message}", Color.Red)
        Finally
            Button3.Enabled = True
        End Try
    End Sub

    ' Button4 - إعادة تحميل
    Private Sub Button4_Click(sender As Object, e As EventArgs)
        Try
            WebView21.CoreWebView2.Reload()
            UpdateStatus("🔄 تم إعادة التحميل", Color.Blue)
        Catch ex As Exception
            UpdateStatus($"❌ خطأ: {ex.Message}", Color.Red)
        End Try
    End Sub

    ' Button5 - حل متعدد المواقع
    Private Async Sub Button5_Click(sender As Object, e As EventArgs)
        Try
            Button5.Enabled = False
            Dim urls As String() = {
                "https://2captcha.com/demo/recaptcha-v2",
                "https://www.google.com/recaptcha/api2/demo"
            }

            UpdateStatus("🚀 بدء الحل المتعدد...", Color.Blue)
            Dim successCount As Integer = 0

            For Each url In urls
                Try
                    UpdateStatus($"📍 انتقال لـ: {url}", Color.Blue)
                    WebView21.CoreWebView2.Navigate(url)
                    Await Task.Delay(3000)

                    Await ExtractSiteKey()

                    If Not String.IsNullOrEmpty(TextBox2.Text) AndAlso TextBox2.Text <> "غير موجود" Then
                        Dim solution = Await SolveV2Fast(TextBox2.Text, url, 1)
                        If Not String.IsNullOrEmpty(solution) Then
                            Await InjectSolution(solution)
                            successCount += 1
                            UpdateStatus($"✅ تم حل: {url}", Color.Green)
                        End If
                    End If

                Catch ex As Exception
                    UpdateStatus($"❌ خطأ في {url}: {ex.Message}", Color.Red)
                End Try
            Next

            UpdateStatus($"🎉 انتهى! تم حل {successCount}/{urls.Length} مواقع", Color.Green)

        Finally
            Button5.Enabled = True
        End Try
    End Sub

    ' Button6 - عرض الإحصائيات
    Private Sub Button6_Click(sender As Object, e As EventArgs)
        Try
            Dim avgTime As Double = If(solvedCount > 0, totalTime.TotalSeconds / solvedCount, 0)
            Dim statsMessage As String = $"📊 إحصائيات الأداء السريع:{vbCrLf}" &
                                       $"✅ تم الحل: {solvedCount} مرة{vbCrLf}" &
                                       $"⏱️ متوسط الوقت: {avgTime:F1} ثانية{vbCrLf}" &
                                       $"📈 معدل النجاح: {successRate:F1}%{vbCrLf}" &
                                       $"💰 التكلفة التقديرية: ${solvedCount * 0.002:F3}{vbCrLf}" &
                                       $"⚡ حلول في الكاش: {recentSolutions.Count}"

            MessageBox.Show(statsMessage, "⚡ إحصائيات reCAPTCHA Solver", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            UpdateStatus($"❌ خطأ في الإحصائيات: {ex.Message}", Color.Red)
        End Try
    End Sub

    ' إدارة الكاش للسرعة
    Private Function CheckSolutionCache(siteKey As String) As Boolean
        If recentSolutions.ContainsKey(siteKey) AndAlso solutionCache.ContainsKey(siteKey) Then
            ' التحقق من أن الحل لا يزال صالحاً (أقل من دقيقة)
            Dim cacheTime As Date = solutionCache(siteKey)
            If DateTime.Now.Subtract(cacheTime).TotalSeconds < 60 Then
                Return True
            Else
                ' إزالة الحل القديم
                recentSolutions.Remove(siteKey)
                solutionCache.Remove(siteKey)
            End If
        End If
        Return False
    End Function

    Private Sub SaveToCache(siteKey As String, solution As String)
        recentSolutions(siteKey) = solution
        solutionCache(siteKey) = DateTime.Now
        UpdateStatus("💾 تم حفظ الحل في الكاش", Color.Blue)
    End Sub

    Private Sub CleanOldCache()
        Dim keysToRemove As New List(Of String)

        For Each kvp In solutionCache
            If DateTime.Now.Subtract(kvp.Value).TotalMinutes > 1 Then
                keysToRemove.Add(kvp.Key)
            End If
        Next

        For Each key In keysToRemove
            recentSolutions.Remove(key)
            solutionCache.Remove(key)
        Next
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            If httpClient IsNot Nothing Then
                httpClient.Dispose()
            End If
            If WebView21 IsNot Nothing Then
                WebView21.Dispose()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error during cleanup: {ex.Message}")
        End Try
    End Sub

End Class