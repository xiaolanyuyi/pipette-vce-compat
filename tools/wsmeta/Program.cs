using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

static class Program
{
    const string DLL = "steam_api64.dll";

    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_InitSafe();
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_InitFlat(IntPtr pOutErrMsg);
    [DllImport(DLL)] static extern void SteamAPI_RunCallbacks();
    [DllImport(DLL)] static extern void SteamAPI_Shutdown();
    [DllImport(DLL)] static extern IntPtr SteamAPI_SteamUGC_v021();
    [DllImport(DLL)] static extern IntPtr SteamAPI_SteamUtils_v010();
    [DllImport(DLL)] static extern IntPtr SteamAPI_SteamRemoteStorage_v016();
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamRemoteStorage_GetQuota(IntPtr self, out ulong totalBytes, out ulong availableBytes);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamRemoteStorage_IsCloudEnabledForAccount(IntPtr self);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamRemoteStorage_IsCloudEnabledForApp(IntPtr self);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamRemoteStorage_SetCloudEnabledForApp(IntPtr self, [MarshalAs(UnmanagedType.I1)] bool bEnabled);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamRemoteStorage_FileDelete(IntPtr self, [MarshalAs(UnmanagedType.LPUTF8Str)] string file);
    [DllImport(DLL)] static extern int SteamAPI_ISteamRemoteStorage_GetFileCount(IntPtr self);
    [DllImport(DLL)] static extern IntPtr SteamAPI_ISteamRemoteStorage_GetFileNameAndSize(IntPtr self, int iFile, out int pnFileSizeInBytes);
    [DllImport(DLL)] static extern uint SteamAPI_ISteamUtils_GetAppID(IntPtr self);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_StartItemUpdate(IntPtr self, uint appId, ulong fileId);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_CreateItem(IntPtr self, uint appId, int fileType);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_DeleteItem(IntPtr self, ulong fileId);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemContent(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string contentFolder);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemPreview(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemTags(IntPtr self, ulong handle, ref SteamParamStringArray_t tags);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemVisibility(IntPtr self, ulong handle, int visibility);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemTitle(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemDescription(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string description);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_SubmitItemUpdate(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string changenote);
    [DllImport(DLL)] static extern int SteamAPI_ISteamUGC_GetItemUpdateProgress(IntPtr self, ulong handle, out ulong processed, out ulong total);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUtils_IsAPICallCompleted(IntPtr self, ulong apiCall, [MarshalAs(UnmanagedType.I1)] out bool failed);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUtils_GetAPICallResult(IntPtr self, ulong apiCall, IntPtr pCallback, int cubCallback, int iCallbackExpected, [MarshalAs(UnmanagedType.I1)] out bool failed);

    [StructLayout(LayoutKind.Sequential)]
    struct SteamParamStringArray_t { public IntPtr m_ppStrings; public int m_nNumStrings; }

    const uint APP_ID = 4348910;
    const ulong DEFAULT_ITEM = 3846674523;
    const string DEFAULT_CONTENT = @"E:\knowledge base\pipette-vce-compat\mod";
    const string DEFAULT_TITLE = "移液枪 × 原版化学扩展 兼容补丁 / Pipette × Vanilla Chem Ext Compat";
    const string DEFAULT_DESC = "一键配平兼容补丁。需要 Pen的移液枪 + 原版化学扩展；详见创意工坊页面。";

    static IntPtr _ugc, _utils;

    static int Main(string[] args)
    {
        try { Directory.SetCurrentDirectory(AppContext.BaseDirectory); } catch { }
        try
        {
            var log = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "WsMeta.log"), false, Encoding.UTF8) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, log));
            Console.SetError(new TeeWriter(Console.Error, log));
        }
        catch { }

        ulong targetItem = DEFAULT_ITEM;
        bool cloudInfo = args.Any(a => a == "--cloud-info");
        bool cloudOff = args.Any(a => a == "--cloud-off");
        bool cloudOn = args.Any(a => a == "--cloud-on");
        string deleteItemArg = args.FirstOrDefault(a => a.StartsWith("delete-item=", StringComparison.OrdinalIgnoreCase));
        string deleteCloud = args.FirstOrDefault(a => a.StartsWith("delete-cloud=", StringComparison.OrdinalIgnoreCase));
        bool noPreview = args.Any(a => a == "--no-preview");
        bool doCreate = args.Any(a => a == "--create");
        bool keepPrivate = args.Any(a => a == "--private");
        string preview = Path.Combine(AppContext.BaseDirectory, "preview.png");
        string content = DEFAULT_CONTENT;
        string title = DEFAULT_TITLE;
        string desc = DEFAULT_DESC;
        string changenote = "v2.0.1 add tags and preview";
        var tags = new List<string> { "Mod", "Gameplay", "Simplified Chinese" };

        foreach (var a in args)
        {
            if (a.StartsWith("item=", StringComparison.OrdinalIgnoreCase)) targetItem = ulong.Parse(a.Substring(5));
            else if (a.StartsWith("preview=", StringComparison.OrdinalIgnoreCase)) preview = Path.GetFullPath(a.Substring(8));
            else if (a.StartsWith("content=", StringComparison.OrdinalIgnoreCase)) content = Path.GetFullPath(a.Substring(8));
            else if (a.StartsWith("tags=", StringComparison.OrdinalIgnoreCase))
                tags = a.Substring(5).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            else if (a.StartsWith("title=", StringComparison.OrdinalIgnoreCase)) title = a.Substring(6);
            else if (a.StartsWith("description=", StringComparison.OrdinalIgnoreCase)) desc = a.Substring(12);
            else if (a.StartsWith("changenote=", StringComparison.OrdinalIgnoreCase)) changenote = a.Substring(11);
            else if (!a.StartsWith("--") && File.Exists(a)) preview = Path.GetFullPath(a);
        }

        Console.WriteLine("== WsMeta ==");
        Console.WriteLine("mode    = " + (doCreate ? "CREATE new item" : "UPDATE item " + targetItem));
        Console.WriteLine("preview = " + (noPreview ? "(skipped)" : preview));
        Console.WriteLine("content = " + content + (Directory.Exists(content) ? "" : "  (MISSING)"));
        Console.WriteLine("tags    = " + string.Join(", ", tags));
        Console.WriteLine("visible = " + (keepPrivate ? "private" : "public"));
        Console.WriteLine();

        bool inited = SteamAPI_InitSafe();
        string initErr = "";
        if (!inited)
        {
            IntPtr errBuf = Marshal.AllocHGlobal(2048);
            Marshal.WriteByte(errBuf, 0, 0);
            inited = SteamAPI_InitFlat(errBuf);
            initErr = Marshal.PtrToStringUTF8(errBuf) ?? "";
            Marshal.FreeHGlobal(errBuf);
        }
        if (!inited)
        {
            Console.WriteLine("SteamAPI init failed. Start Steam and log in, and keep steam_appid.txt next to the exe.");
            if (initErr.Length > 0) Console.WriteLine("Steam says: " + initErr);
            return 2;
        }

        try
        {
            _ugc = SteamAPI_SteamUGC_v021();
            _utils = SteamAPI_SteamUtils_v010();
            if (cloudInfo)
            {
                IntPtr rs0 = SteamAPI_SteamRemoteStorage_v016();
                SteamAPI_ISteamRemoteStorage_GetQuota(rs0, out ulong tq, out ulong aq);
                Console.WriteLine("cloud quota: total=" + (tq / 1048576.0).ToString("F1") + " MB, available=" + (aq / 1048576.0).ToString("F1") + " MB");
                int cnt = SteamAPI_ISteamRemoteStorage_GetFileCount(rs0);
                Console.WriteLine("cloud files: " + cnt);
                ulong sum = 0;
                for (int i = 0; i < cnt; i++)
                {
                    IntPtr namePtr = SteamAPI_ISteamRemoteStorage_GetFileNameAndSize(rs0, i, out int fsz);
                    string nm = Marshal.PtrToStringUTF8(namePtr) ?? "";
                    sum += (ulong)fsz;
                    Console.WriteLine("  " + (fsz / 1024.0).ToString("F0").PadLeft(8) + " KB  " + nm);
                }
                Console.WriteLine("sum = " + (sum / 1048576.0).ToString("F1") + " MB");
                return 0;
            }
            Console.WriteLine("connected, appid = " + SteamAPI_ISteamUtils_GetAppID(_utils));
            try
            {
                IntPtr rs = SteamAPI_SteamRemoteStorage_v016();
                bool cloudAcc = SteamAPI_ISteamRemoteStorage_IsCloudEnabledForAccount(rs);
                bool cloudApp = SteamAPI_ISteamRemoteStorage_IsCloudEnabledForApp(rs);
                bool okQ = SteamAPI_ISteamRemoteStorage_GetQuota(rs, out ulong qTotal, out ulong qAvail);
                Console.WriteLine("cloud: account=" + cloudAcc + " app=" + cloudApp + " quota=" + (okQ ? (qAvail / 1048576.0).ToString("F1") + " / " + (qTotal / 1048576.0).ToString("F1") + " MB free" : "n/a"));
            }
            catch (Exception ex) { Console.WriteLine("cloud diag failed: " + ex.Message); }

            if (deleteItemArg != null)
            {
                foreach (var s in deleteItemArg.Substring("delete-item=".Length).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ulong id = ulong.Parse(s);
                    ulong dc = SteamAPI_ISteamUGC_DeleteItem(_ugc, id);
                    bool done = WaitCall(dc, out bool iof, "delete");
                    Console.WriteLine("DeleteItem(" + id + ") call=" + dc + " completed=" + done);
                }
                return 0;
            }
            if (cloudOn || deleteCloud != null)
            {
                IntPtr rs = SteamAPI_SteamRemoteStorage_v016();
                Console.WriteLine("SetCloudEnabledForApp(true) = " + SteamAPI_ISteamRemoteStorage_SetCloudEnabledForApp(rs, true));
            }
            if (deleteCloud != null)
            {
                IntPtr rs = SteamAPI_SteamRemoteStorage_v016();
                SteamAPI_ISteamRemoteStorage_GetQuota(rs, out ulong t0, out ulong a0);
                Console.WriteLine("quota before: total=" + (t0 / 1048576.0).ToString("F1") + "MB avail=" + (a0 / 1048576.0).ToString("F1") + "MB");
                foreach (var name in deleteCloud.Substring("delete-cloud=".Length).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    bool ok = SteamAPI_ISteamRemoteStorage_FileDelete(rs, name);
                    Console.WriteLine("FileDelete(" + name + ") = " + ok);
                }
                SteamAPI_ISteamRemoteStorage_GetQuota(rs, out ulong t1, out ulong a1);
                Console.WriteLine("quota after : total=" + (t1 / 1048576.0).ToString("F1") + "MB avail=" + (a1 / 1048576.0).ToString("F1") + "MB");
            }
            if (cloudOff)
            {
                IntPtr rs = SteamAPI_SteamRemoteStorage_v016();
                Console.WriteLine("SetCloudEnabledForApp(false) = " + SteamAPI_ISteamRemoteStorage_SetCloudEnabledForApp(rs, false));
                Console.WriteLine("cloud: account=" + SteamAPI_ISteamRemoteStorage_IsCloudEnabledForAccount(rs) + " app=" + SteamAPI_ISteamRemoteStorage_IsCloudEnabledForApp(rs));
            }
            ulong itemId = targetItem;
            if (doCreate)
            {
                ulong createCall = SteamAPI_ISteamUGC_CreateItem(_ugc, APP_ID, 0); // 0 = Community
                Console.WriteLine("CreateItem call = " + createCall);
                if (createCall == 0) { Console.WriteLine("CreateItem failed immediately."); return 3; }
                if (!WaitCall(createCall, out bool ioFail1, "create"))
                {
                    Console.WriteLine("CreateItem timed out.");
                    return 3;
                }
                if (!TryGetResult(createCall, (r, legal, fid) => r == 1 && fid > 3_000_000_000UL, out int r1, out bool legal1, out ulong fid1, out int cb1))
                {
                    Console.WriteLine("CreateItem finished but result could not be read.");
                    return 3;
                }
                itemId = fid1;
                Console.WriteLine("CreateItem result: EResult=" + r1 + " item=" + itemId + " legal=" + legal1);
            }

            ulong handle = SteamAPI_ISteamUGC_StartItemUpdate(_ugc, APP_ID, itemId);
            Console.WriteLine("update handle = " + handle);
            if (handle == 0) { Console.WriteLine("StartItemUpdate failed."); return 4; }

            if (!doCreate)
                Console.WriteLine("SetItemContent = " + SteamAPI_ISteamUGC_SetItemContent(_ugc, handle, content));
            else
                Console.WriteLine("SetItemContent = " + SteamAPI_ISteamUGC_SetItemContent(_ugc, handle, content));

            Console.WriteLine("SetItemTitle = " + SteamAPI_ISteamUGC_SetItemTitle(_ugc, handle, title));
            Console.WriteLine("SetItemDescription = " + SteamAPI_ISteamUGC_SetItemDescription(_ugc, handle, desc));
            if (!noPreview)
                Console.WriteLine("SetItemPreview = " + SteamAPI_ISteamUGC_SetItemPreview(_ugc, handle, preview));
            Console.WriteLine("SetItemTags = " + SetTags(handle, tags.ToArray()));
            Console.WriteLine("SetItemVisibility(" + (keepPrivate ? "private" : "public") + ") = " +
                SteamAPI_ISteamUGC_SetItemVisibility(_ugc, handle, keepPrivate ? 2 : 0));

            ulong call = SteamAPI_ISteamUGC_SubmitItemUpdate(_ugc, handle, changenote);
            Console.WriteLine("SubmitItemUpdate call = " + call);
            if (call == 0) { Console.WriteLine("SubmitItemUpdate failed immediately."); return 5; }

            if (!WaitCall(call, out bool ioFail, "submit"))
            {
                Console.WriteLine("SubmitItemUpdate timed out.");
                return 5;
            }

            if (TryGetResult(call, (r, legal, fid) => fid == itemId, out int res, out bool legalAgree, out ulong fid2, out int cbId))
            {
                Console.WriteLine("Submit result: EResult=" + res + " item=" + fid2 + " legalAgreement=" + legalAgree);
                if (legalAgree)
                    Console.WriteLine("!! Accept the Steam Workshop legal agreement in a browser, then run again.");
                if (res != 1) return 6;
            }
            else
            {
                Console.WriteLine("Submit finished; result payload not found (IO failure=" + ioFail + "). Check the item page.");
            }

            Console.WriteLine();
            Console.WriteLine("item id  = " + itemId);
            Console.WriteLine("item url = https://steamcommunity.com/sharedfiles/filedetails/?id=" + itemId);
            Console.WriteLine("Done.");
            return 0;
        }
        finally
        {
            SteamAPI_Shutdown();
        }
    }

    static bool WaitCall(ulong call, out bool ioFailed, string label)
    {
        ioFailed = false;
        var deadline = DateTime.UtcNow.AddMinutes(5);
        int lastStatus = -1;
        while (DateTime.UtcNow < deadline)
        {
            SteamAPI_RunCallbacks();
            if (SteamAPI_ISteamUtils_IsAPICallCompleted(_utils, call, out ioFailed))
                return true;
            Thread.Sleep(300);
        }
        return false;
    }

    static bool TryGetResult(ulong call, Func<int, bool, ulong, bool> accept, out int eResult, out bool legal, out ulong fileId, out int cbId)
    {
        eResult = 0; legal = false; fileId = 0; cbId = -1;
        IntPtr buf = Marshal.AllocHGlobal(64);
        try
        {
            for (int id = 3000; id <= 3600; id++)
            {
                bool failed;
                if (SteamAPI_ISteamUtils_GetAPICallResult(_utils, call, buf, 16, id, out failed))
                {
                    int r = Marshal.ReadInt32(buf, 0);
                    bool lg = Marshal.ReadByte(buf, 4) != 0;
                    ulong fid = (ulong)Marshal.ReadInt64(buf, 8);
                    if (accept(r, lg, fid))
                    {
                        eResult = r; legal = lg; fileId = fid; cbId = id;
                        return true;
                    }
                }
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return false;
    }

    static bool SetTags(ulong handle, string[] tags)
    {
        IntPtr[] ptrs = new IntPtr[tags.Length];
        try
        {
            for (int i = 0; i < tags.Length; i++) ptrs[i] = Marshal.StringToHGlobalAnsi(tags[i]);
            IntPtr arr = Marshal.AllocHGlobal(IntPtr.Size * tags.Length);
            try
            {
                for (int i = 0; i < tags.Length; i++) Marshal.WriteIntPtr(arr, i * IntPtr.Size, ptrs[i]);
                var payload = new SteamParamStringArray_t { m_ppStrings = arr, m_nNumStrings = tags.Length };
                return SteamAPI_ISteamUGC_SetItemTags(_ugc, handle, ref payload);
            }
            finally { Marshal.FreeHGlobal(arr); }
        }
        finally { foreach (var p in ptrs) if (p != IntPtr.Zero) Marshal.FreeHGlobal(p); }
    }
}

sealed class TeeWriter : TextWriter
{
    private readonly TextWriter _a;
    private readonly TextWriter _b;
    public TeeWriter(TextWriter a, TextWriter b) { _a = a; _b = b; }
    public override Encoding Encoding => _a.Encoding;
    public override void Write(char value) { _a.Write(value); _b.Write(value); }
    public override void Write(string value) { _a.Write(value); _b.Write(value); }
    public override void WriteLine(string value) { _a.WriteLine(value); _b.WriteLine(value); }
    public override void Flush() { _a.Flush(); _b.Flush(); }
}