Imports JHSoftware.SDNSRawLog

Module FilterRawLog

  Dim log As RawLog
  Dim outFile As System.IO.StreamWriter

  Sub Main()
    Dim clct As Integer = My.Application.CommandLineArgs.Count
    If clct < 3 Then WriteHelp() : Exit Sub
    If My.Application.CommandLineArgs(0).ToLower = "zone" Then
      If clct <> 4 Then WriteHelp() : Exit Sub
    Else
      If clct <> 3 Then WriteHelp() : Exit Sub
    End If

    log = New RawLog(My.Application.CommandLineArgs(clct - 2))
    outFile = My.Computer.FileSystem.OpenTextFileWriter(My.Application.CommandLineArgs(clct - 1), False)

    Select Case My.Application.CommandLineArgs(0).ToLower
      Case "zone"
        ProcLogByZone()
      Case "qname"
        ProcLogByQName()
      Case "qtype"
        ProcLogByQType()
      Case "fromip"
        ProcLogByFromIP()
      Case "hour"
        ProcLogByHour()
      Case Else
        WriteHelp()
    End Select

    outFile.Close()
  End Sub

  Private Sub WriteHelp()
    Console.WriteLine("DESCRIPTION:")
    Console.WriteLine("   Parses a Simple DNS Plus raw log file (.sdraw), filters and summarizes")
    Console.WriteLine("   the data either by zone, query name (qname), query type (qtype),")
    Console.WriteLine("   request source IP address (fromip), or hour and writes the result to")
    Console.WriteLine("   a text file as comma separated values (csv format).")
    Console.WriteLine("USAGE:")
    Console.WriteLine("   FilterRawLog zone   <simple-dns-plus-database-file> <raw-log-file> <output-file>")
    Console.WriteLine("   FilterRawLog qname  <raw-log-file> <output-file>")
    Console.WriteLine("   FilterRawLog qtype  <raw-log-file> <output-file>")
    Console.WriteLine("   FilterRawLog fromip <raw-log-file> <output-file>")
    Console.WriteLine("   FilterRawLog hour   <raw-log-file> <output-file>")
  End Sub

  Private Sub ProcLogByZone()
    Dim zoneDict As New Dictionary(Of DomainName, Integer)
    Dim DbConn = New System.Data.SQLite.SQLiteConnection("data source=" & My.Application.CommandLineArgs(1) & ";Version=3")
    DbConn.Open()
    Dim cmd = DbConn.CreateCommand()
    cmd.CommandText = "SELECT name FROM zone"
    Dim rdr = cmd.ExecuteReader()
    Dim ct = 0
    While rdr.Read
      zoneDict.Add(DomainName.Parse(CStr(rdr!name)), ct)
      ct += 1
    End While
    rdr.Close()
    DbConn.Close()

    Dim HitCount(zoneDict.Count - 1) As Integer
    Dim qName As DomainName
    Dim segCt As Integer
    Dim i As Integer
    For Each req As RawLog.Request In log
      qName = req.QName
      segCt = qName.SegmentCount
      While segCt > 0
        If zoneDict.TryGetValue(qName, i) Then HitCount(i) += 1 : Exit While
        qName = qName.Parent
        segCt -= 1
      End While
    Next
    For Each kv In zoneDict
      If HitCount(kv.Value) > 0 Then outFile.WriteLine("""" & kv.Key.ToString() & """," & HitCount(kv.Value))
    Next
  End Sub

  Private Sub ProcLogByQName()
    Dim QNames As New Dictionary(Of DomainName, Counter)
    Dim c As Counter = Nothing
    For Each req As RawLog.Request In log
      If QNames.TryGetValue(req.QName, c) Then c.Count += 1 Else QNames.Add(req.QName, New Counter)
    Next
    For Each v As Generic.KeyValuePair(Of DomainName, Counter) In QNames
      outFile.WriteLine("""" & v.Key.ToString & """," & v.Value.Count)
    Next
  End Sub

  Private Sub ProcLogByQType()
    Dim QTypes As New Dictionary(Of String, Counter)
    Dim c As Counter = Nothing
    Dim s As String
    For Each req As RawLog.Request In log
      s = req.QTypeName
      If QTypes.TryGetValue(s, c) Then c.Count += 1 Else QTypes.Add(s, New Counter)
    Next
    For Each v As Generic.KeyValuePair(Of String, Counter) In QTypes
      outFile.WriteLine("""" & v.Key & """," & v.Value.Count)
    Next
  End Sub

  Private Sub ProcLogByFromIP()
    Dim IPs As New Dictionary(Of String, Counter)
    Dim c As Counter = Nothing
    Dim s As String
    For Each req As RawLog.Request In log
      s = req.FromIP.ToString
      If IPs.TryGetValue(s, c) Then c.Count += 1 Else IPs.Add(s, New Counter)
    Next
    For Each v As KeyValuePair(Of String, Counter) In IPs
      outFile.WriteLine("""" & v.Key & """," & v.Value.Count)
    Next
  End Sub

  Private Sub ProcLogByHour()
    Dim HourCt(23) As Integer
    For Each req As RawLog.Request In log
      HourCt(req.Time.Hours) += 1
    Next
    For i As Integer = 0 To 23
      If HourCt(i) > 0 Then outFile.WriteLine(i & "," & HourCt(i))
    Next
  End Sub

  Private Class Counter
    Friend Count As Integer = 1
  End Class
End Module

