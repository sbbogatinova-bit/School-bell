Imports System.IO
Imports System.Media

Public Class Form1

    ' Споделен soundplayer за всички звънци
    Dim player As SoundPlayer

    ' Памет за звънците, за да не звънят многократно в една и съща минута
    Dim lastring As String = ""
    Dim lastWarning As String = ""

    ' Път до csv файла за графика
    Dim scheduleFile As String = Application.StartupPath & "\schedule.csv"

    ' -----------------------------
    ' форма load
    ' -----------------------------
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSchedule()
        Timer1.Start()
    End Sub

    ' -----------------------------
    ' таймер за текущо време и звънци
    ' -----------------------------
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        ' 1. Текущо време
        Dim nowTime As DateTime = DateTime.Now
        lblClock.Text = nowTime.ToString("HH:mm:ss")
        Dim currentTime As String = nowTime.ToString("HH:mm")

        ' 2. Фиксирано определяне на деня
        Dim daysOfWeek() As String = {"sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"}
        Dim dayName As String = daysOfWeek(CInt(nowTime.DayOfWeek))

        ' Проверка за уикенд
        If dayName = "saturday" Or dayName = "sunday" Then
            TimeTillClass.Text = "-"
            TimeTillBreak.Text = "-"
            Return
        End If

        ' 3. Намиране на колоните
        Dim prefix As String = dayName.Substring(0, 3)
        Dim startColName As String = prefix & "_col"
        Dim endColName As String = prefix & "e_col"
        Dim startColIndex As Integer = -1
        Dim endColIndex As Integer = -1

        For Each col As DataGridViewColumn In DataGridView1.Columns
            If col.Name.ToLower() = startColName Then startColIndex = col.Index
            If col.Name.ToLower() = endColName Then endColIndex = col.Index
        Next

        ' 4. Пускане на звук (използваме обща логика за начало и край)
        PlayScheduledBell(startColIndex, currentTime, dayName)
        PlayScheduledBell(endColIndex, currentTime, dayName)

        ' 5. Изчисляване на време до следващ час и междучасие
        Dim nextClass As DateTime? = Nothing
        Dim nextBreak As DateTime? = Nothing

        If startColIndex <> -1 Then
            For Each row As DataGridViewRow In DataGridView1.Rows
                Dim cell As DataGridViewCell = row.Cells(startColIndex)
                If cell.Value IsNot Nothing AndAlso Not String.IsNullOrEmpty(cell.Value.ToString()) Then
                    Dim t As DateTime
                    If DateTime.TryParseExact(cell.Value.ToString(), "HH:mm", Nothing, Globalization.DateTimeStyles.None, t) Then
                        t = nowTime.Date + t.TimeOfDay
                        If t > nowTime Then
                            nextClass = t
                            Exit For
                        End If
                    End If
                End If
            Next
        End If

        If endColIndex <> -1 Then
            For Each row As DataGridViewRow In DataGridView1.Rows
                Dim cell As DataGridViewCell = row.Cells(endColIndex)
                If cell.Value IsNot Nothing AndAlso Not String.IsNullOrEmpty(cell.Value.ToString()) Then
                    Dim t As DateTime
                    If DateTime.TryParseExact(cell.Value.ToString(), "HH:mm", Nothing, Globalization.DateTimeStyles.None, t) Then
                        t = nowTime.Date + t.TimeOfDay
                        If t > nowTime Then
                            nextBreak = t
                            Exit For
                        End If
                    End If
                End If
            Next
        End If

        ' Обновяване на етикетите
        If nextClass.HasValue Then
            TimeTillClass.Text = (nextClass.Value - nowTime).ToString("hh\:mm\:ss")
        Else
            TimeTillClass.Text = "-"
        End If

        If nextBreak.HasValue Then
            TimeTillBreak.Text = (nextBreak.Value - nowTime).ToString("hh\:mm\:ss")
        Else
            TimeTillBreak.Text = "-"
        End If
    End Sub

    ' -----------------------------
    ' Основна функция за звънене
    ' -----------------------------
    Private Sub PlayScheduledBell(colIndex As Integer, currentTime As String, dayName As String)
        If colIndex = -1 Then Return

        Dim basePath As String = Application.StartupPath & "\bells\"
        Dim colName As String = DataGridView1.Columns(colIndex).Name.ToLower()

        For Each row As DataGridViewRow In DataGridView1.Rows
            If row.Cells(colIndex).Value Is Nothing OrElse String.IsNullOrEmpty(row.Cells(colIndex).Value.ToString().Trim()) Then
                Continue For
            End If

            Dim cellValue As String = row.Cells(colIndex).Value.ToString().Trim()
            Dim scheduledTime As DateTime

            If Not DateTime.TryParseExact(cellValue, "HH:mm", Nothing, Globalization.DateTimeStyles.None, scheduledTime) Then
                Continue For
            End If

            Dim soundTime As String = cellValue.Replace(":", "_")

            ' 3. ПРЕДУПРЕДИТЕЛЕН ЗВЪНЕЦ (5 минути преди часа - само за колоните за начало)
            If colName.EndsWith("_col") AndAlso Not colName.Contains("e_col") Then
                Dim warningTime As String = scheduledTime.AddMinutes(-5).ToString("HH:mm")

                If warningTime = currentTime Then
                    If lastWarning <> currentTime Then
                        Dim warnFile As String = basePath & "warning.wav"
                        If System.IO.File.Exists(warnFile) Then
                            My.Computer.Audio.Play(warnFile, AudioPlayMode.Background)
                        Else
                            PlayBell()
                        End If
                        lastWarning = currentTime
                    End If
                    Exit Sub
                End If
            End If

            ' 4. ОСНОВЕН ЗВЪНЕЦ (Точно в часа)
            If cellValue = currentTime Then
                If lastring <> currentTime Then
                    Dim fDay As String = basePath & soundTime & "_" & dayName & ".wav"
                    Dim fDef As String = basePath & soundTime & ".wav"

                    If System.IO.File.Exists(fDay) Then
                        My.Computer.Audio.Play(fDay, AudioPlayMode.Background)
                    ElseIf System.IO.File.Exists(fDef) Then
                        My.Computer.Audio.Play(fDef, AudioPlayMode.Background)
                    Else
                        PlayBell()
                    End If
                    lastring = currentTime
                End If
                Exit Sub
            End If
        Next
    End Sub

    ' Ръчно пускане на стандартен bell.wav
    Sub PlayBell()
        Dim bellPath As String = Application.StartupPath & "\bell.wav"
        If Not System.IO.File.Exists(bellPath) Then
            bellPath = Application.StartupPath & "\bells\bell.wav"
        End If

        If System.IO.File.Exists(bellPath) Then
            Try
                My.Computer.Audio.Play(bellPath, AudioPlayMode.Background)
            Catch ex As Exception
                Debug.WriteLine("Грешка: " & ex.Message)
            End Try
        End If
    End Sub

    ' -----------------------------
    ' запаметяване и зареждане на графика
    ' -----------------------------
    Private Sub SaveSchedule()
        Using sw As New StreamWriter(scheduleFile)
            Dim headers As New List(Of String)
            For Each col As DataGridViewColumn In DataGridView1.Columns
                headers.Add(col.Name)
            Next
            sw.WriteLine(String.Join(",", headers))

            ' Поправена проверка за стойност, съвместима с всички версии
            For Each row As DataGridViewRow In DataGridView1.Rows
                Dim cells As New List(Of String)
                For Each cell As DataGridViewCell In row.Cells
                    If cell.Value IsNot Nothing AndAlso Not String.IsNullOrEmpty(cell.Value.ToString()) Then
                        cells.Add(cell.Value.ToString())
                    Else
                        cells.Add("")
                    End If
                Next
                sw.WriteLine(String.Join(",", cells))
            Next
        End Using
    End Sub

    Private Sub LoadSchedule()
        If Not File.Exists(scheduleFile) Then Return

        DataGridView1.Rows.Clear()
        DataGridView1.Columns.Clear()

        Dim lines() As String = File.ReadAllLines(scheduleFile)
        If lines.Length = 0 Then Return

        Dim headers() As String = lines(0).Split(","c)
        For Each h In headers
            DataGridView1.Columns.Add(h, h)
        Next

        For i As Integer = 1 To lines.Length - 1
            Dim values() As String = lines(i).Split(","c)
            DataGridView1.Rows.Add(values)
        Next
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        SaveSchedule()
        MessageBox.Show("Графикът е запазен успешно!", "Запазване", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Label6_Click(sender As Object, e As EventArgs) Handles Label6.Click
        MessageBox.Show("Тази програма е направена благодарение на ChatGPT и Tomasz (@th03_tlv)")
    End Sub
End Class