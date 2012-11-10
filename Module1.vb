Imports Microsoft.VisualBasic
Imports DataAccessLayer.DataAccessLayer
Imports BusinessAccessLayer.BusinessLayer
Imports System.Data.SqlClient
Imports System.IO
Imports Microsoft.Web.Administration
Imports Facebook

Module Module1
    Dim objBAL As New BALSchedulePost
    Dim objLog As New Log

    Sub Main()
        Dim intTotal As Integer = 0
        Dim intSuccessFull As Integer = 0
        Dim intFailed As Integer = 0
        Dim strFailedFanPageId As String = ""

        objLog.WritePlainLog(FormatDateTime(Now.Date, DateFormat.ShortDate))
        objLog.WritePlainLog("==========================================")
        objLog.WriteLog("START")
        schedulepost(intTotal, intSuccessFull, intFailed, strFailedFanPageId)
        objLog.WriteLog("END  ")
        objLog.WritePlainLog("------------------------------------------")
        objLog.WritePlainLog("Total Records : " & intTotal)
        objLog.WritePlainLog("Successfull   : " & intSuccessFull)
        objLog.WritePlainLog("Failed        : " & intFailed)
        objLog.WritePlainLog("------------------------------------------")
        objLog.WritePlainLog("")
    End Sub

#Region "Schedule Post"
  
    Sub schedulepost(ByRef intTotal As Integer, ByRef intSuccessFull As Integer, ByRef intFailed As Integer, ByRef strFailedFanPageId As String)
        Try
            Dim ds As New DataSet
            Dim objBAL As New BALSchedulePost
            ds = objBAL.GetScheduledData()

            intTotal = ds.Tables(0).Rows.Count

            If ds.Tables(0).Rows.Count > 0 Then
                For i As Integer = 0 To ds.Tables(0).Rows.Count - 1
                    Dim strid As String = ds.Tables(0).Rows(i).Item("sm_id").ToString
                    Dim strmessage1 As String = ds.Tables(0).Rows(i).Item("sm_Message").ToString.Replace("<br>", Chr(10))
                    Dim strDate As String = ds.Tables(0).Rows(i).Item("sm_ScheduleDate").ToString
                    Dim strHour As String = ds.Tables(0).Rows(i).Item("sm_ScheduleHour").ToString
                    Dim strMinute As String = ds.Tables(0).Rows(i).Item("sm_ScheduleMinute").ToString
                    Dim strAMPM As String = ds.Tables(0).Rows(i).Item("sm_ScheduleAMPM").ToString
                    Dim strTimeZone As String = ds.Tables(0).Rows(i).Item("sm_ScheduleTimeZone").ToString

                    objBAL.ScheduleId = CInt(strid)
                    Dim dsFanPages As New DataSet
                    dsFanPages = objBAL.GetScheduledDataFanPages()

                    If ds.Tables(0).Rows(i).Item("sm_Image").ToString <> "" Then
                        'Dim photopath As String = Server.MapPath("~/" & "Content/uploads/images/" & ds.Tables(0).Rows(i).Item("sm_Image").ToString)
                        'Dim photopath As String = "\\ace\Projects\tsmaDB2\Project\Content\uploads\images\" & ds.Tables(0).Rows(i).Item("sm_Image").ToString
                        'Dim photopath As String = "C:\HostingSpaces\webuild\tsma2.techsturedevelopment.com\wwwroot\Content\uploads\images\" & ds.Tables(0).Rows(i).Item("sm_Image").ToString 'tsma2.techsturedevelopement.com server
                        Dim photopath As String = "C:\inetpub\wwwroot\release\forrent_20120216220109\Content\uploads\images\" & ds.Tables(0).Rows(i).Item("sm_Image").ToString ''forrent.com production server

                        If dsFanPages.Tables(0).Rows.Count > 0 Then
                            For j As Integer = 0 To dsFanPages.Tables(0).Rows.Count - 1
                                If dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString <> "" Then
                                    Try
                                        Dim path As String = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString & "/photos"
                                        Dim mediaObject As New FacebookMediaObject() With { _
                                            .FileName = photopath, _
                                            .ContentType = "image/jpg" _
                                    }
                                        Dim fileBytes As Byte() = File.ReadAllBytes(mediaObject.FileName)
                                        mediaObject.SetValue(fileBytes)
                                        Dim upload As IDictionary(Of String, Object) = New Dictionary(Of String, Object)()
                                        upload.Add("message", ds.Tables(0).Rows(i).Item("sm_Message").ToString.Replace("<br>", Chr(10)))
                                        upload.Add("image", mediaObject)
                                        Dim fbapp = New FacebookClient(dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString)
                                        fbapp.Post(path, upload)

                                        objBAL.DraftId = CInt(strid)
                                        objBAL.TSMAUserId = ds.Tables(0).Rows(i).Item("sm_TSMAUserId").ToString 'Session("SiteUserId")
                                        objBAL.FBUserId = ds.Tables(0).Rows(i).Item("sm_FBUserId").ToString 'Session("FacebookUserId")
                                        objBAL.UpdateSchdulePostNew()

                                        intSuccessFull = intSuccessFull + 1
                                    Catch ex As Exception
                                        InsertErrorLog(strid, "", dsFanPages.Tables(0).Rows(j).Item("sp_FBUserId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString, ex.Message.ToString())
                                        intFailed = intFailed + 1
                                        strFailedFanPageId = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString
                                        strFailedFanPageId = "," & strFailedFanPageId
                                    End Try
                                Else
                                    InsertErrorLog(strid, "", dsFanPages.Tables(0).Rows(j).Item("sp_FBUserId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString, "Access token does not exists!")
                                    intFailed = intFailed + 1
                                    strFailedFanPageId = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString
                                    strFailedFanPageId = "," & strFailedFanPageId
                                End If
                            Next
                        End If
                    Else
                        Dim args1 = New Dictionary(Of String, Object)()
                        args1.Add("message", strmessage1.ToString.Replace("<br>", Chr(10)))
                        If ds.Tables(0).Rows(i).Item("sm_VideoLink").ToString <> "" Then
                            args1.Add("link", ds.Tables(0).Rows(i).Item("sm_VideoLink").ToString)
                        End If
                        If dsFanPages.Tables(0).Rows.Count > 0 Then
                            For j As Integer = 0 To dsFanPages.Tables(0).Rows.Count - 1
                                If dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString <> "" Then
                                    Try
                                        Dim AccessToken As String = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString
                                        Dim path As String = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString & "/feed" '"151638698269605/feed" '
                                        Dim fbapp = New FacebookClient(AccessToken)
                                        fbapp.Post(path, args1)

                                        objBAL.DraftId = CInt(strid)
                                        objBAL.TSMAUserId = ds.Tables(0).Rows(i).Item("sm_TSMAUserId").ToString 'Session("SiteUserId")
                                        objBAL.FBUserId = ds.Tables(0).Rows(i).Item("sm_FBUserId").ToString 'Session("FacebookUserId")
                                        objBAL.UpdateSchdulePostNew()
                                        intSuccessFull = intSuccessFull + 1
                                    Catch ex As Exception
                                        InsertErrorLog(strid, "", dsFanPages.Tables(0).Rows(j).Item("sp_FBUserId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString, ex.Message.ToString())
                                        intFailed = intFailed + 1
                                        strFailedFanPageId = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString
                                        strFailedFanPageId = "," & strFailedFanPageId
                                    End Try
                                Else
                                    InsertErrorLog(strid, "", dsFanPages.Tables(0).Rows(j).Item("sp_FBUserId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString, dsFanPages.Tables(0).Rows(j).Item("sp_FBPageAccessToken").ToString, "Access token does not exists!")
                                    intFailed = intFailed + 1
                                    strFailedFanPageId = dsFanPages.Tables(0).Rows(j).Item("sp_FBPageId").ToString
                                    strFailedFanPageId = "," & strFailedFanPageId
                                End If
                            Next
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            InsertErrorLog("", "", "", "", "", ex.Message.ToString & "  : At start of getting data ")
            'intFailed = intFailed + 1
        End Try

    End Sub
#End Region

#Region "Error Log"
    Sub InsertErrorLog(AutoPostId As Integer, TSMAUserId As Integer, FBUserID As String, FBPageId As String, AccessToken As String, ErrorDetails As String)
        Try
            Dim dataAccess As New DALDataAccess()
            dataAccess.AddCommand(CommandType.StoredProcedure, "prc_AddSchedulePostErrorDetails")
            dataAccess.AddParam("@sper_AutoPostId", SqlDbType.Int, AutoPostId)
            dataAccess.AddParam("@sper_TSMAUserId", SqlDbType.Int, TSMAUserId)
            dataAccess.AddParam("@sper_FBUserId", SqlDbType.VarChar, FBUserID)
            dataAccess.AddParam("@sper_FBPageId", SqlDbType.VarChar, FBPageId)
            dataAccess.AddParam("@sper_AcessToken", SqlDbType.VarChar, AccessToken)
            dataAccess.AddParam("@sper_Error", SqlDbType.VarChar, ErrorDetails)
            dataAccess.ExecuteNonQuery()
        Catch ex As Exception
            InsertErrorLog("", "", "", "", "", ex.Message.ToString & "  : At the time of inserting records in tbl_scheudlposterror table ")
        End Try
    End Sub
#End Region
End Module
