using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

static class Program
{
    const string DLL = "steam_api64.dll";

    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_InitFlat(IntPtr pOutErrMsg);
    [DllImport(DLL)] static extern void SteamAPI_RunCallbacks();
    [DllImport(DLL)] static extern void SteamAPI_Shutdown();
    [DllImport(DLL)] static extern IntPtr SteamAPI_SteamUGC_v021();
    [DllImport(DLL)] static extern IntPtr SteamAPI_SteamUtils_v010();
    [DllImport(DLL)] static extern uint SteamAPI_ISteamUtils_GetAppID(IntPtr self);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_StartItemUpdate(IntPtr self, uint appId, ulong fileId);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemPreview(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemTags(IntPtr self, ulong handle, ref SteamParamStringArray_t tags);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUGC_SetItemVisibility(IntPtr self, ulong handle, int visibility);
    [DllImport(DLL)] static extern ulong SteamAPI_ISteamUGC_SubmitItemUpdate(IntPtr self, ulong handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string changenote);
    [DllImport(DLL)] static extern int SteamAPI_ISteamUGC_GetItemUpdateProgress(IntPtr self, ulong handle, out ulong processed, out ulong total);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUtils_IsAPICallCompleted(IntPtr self, ulong apiCall, [MarshalAs(UnmanagedType.I1)] out bool failed);
    [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)] static extern bool SteamAPI_ISteamUtils_GetAPICallResult(IntPtr self, ulong apiCall, IntPtr pCallback, int cubCallback, int iCallbackExpected, [MarshalAs(UnmanagedType.I1)] out bool failed);

    [StructLayout(LayoutKind.Sequential)]
    struct SteamParamStringArray_t
    {
        public IntPtr m_ppStrings;
        public int m_nNumStrings;
    }

    static int Main(string[] args)
    {
        try { Directory.SetCurrentDirectory(AppContext.BaseDirectory); } catch { }

        try
        {
            var log = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "WsMeta.log"), false, System.Text.Encoding.UTF8) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, log));
            Console.SetError(new TeeWriter(Console.Error, log));
        }
        catch { }

        uint appId = 4348910;
        ulong itemId = 3846674523;
        string preview = Path.Combine(AppContext.BaseDirectory, "preview.png");
        string[] tags = { "Mod", "Gameplay", "Simplified Chinese" };
        string changenote = "v2.0.1 add tags and preview";

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])) preview = Path.GetFullPath(args[0]);
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
            tags = args[1].Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length > 2) changenote = args[2];

        Console.WriteLine("== WsMeta: set workshop tags / preview via Steamworks ==");
        Console.WriteLine("appid   = " + appId);
        Console.WriteLine("itemid  = " + itemId);
        Console.WriteLine("preview = " + preview + (File.Exists(preview) ? "  (exists)" : "  (MISSING)"));
        Console.WriteLine("tags    = " + string.Join(", ", tags));
        Console.WriteLine();

        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "steam_appid.txt")))
            Console.WriteLine("WARN: steam_appid.txt missing next to the exe.");

        IntPtr errBuf = Marshal.AllocHGlobal(2048);
        Marshal.WriteByte(errBuf, 0, 0);
        bool inited;
        try { inited = SteamAPI_InitFlat(errBuf); }
        finally { /* keep buffer until we read it */ }
        string initErr = Marshal.PtrToStringUTF8(errBuf) ?? "";
        Marshal.FreeHGlobal(errBuf);
        if (!inited)
        {
            Console.WriteLine("SteamAPI_Init failed. Make sure the Steam client is running and logged in,");
            Console.WriteLine("and that steam_appid.txt next to this exe contains 4348910.");
            if (initErr.Length > 0) Console.WriteLine("Steam says: " + initErr);
            return 2;
        }

        int exit = 0;
        try
        {
            IntPtr ugc = SteamAPI_SteamUGC_v021();
            IntPtr utils = SteamAPI_SteamUtils_v010();
            Console.WriteLine("connected, appid = " + SteamAPI_ISteamUtils_GetAppID(utils));

            ulong handle = SteamAPI_ISteamUGC_StartItemUpdate(ugc, appId, itemId);
            if (handle == 0)
            {
                Console.WriteLine("StartItemUpdate FAILED (item id / app id / permission?).");
                return 3;
            }
            Console.WriteLine("update handle = " + handle);

            bool okPreview = File.Exists(preview) && SteamAPI_ISteamUGC_SetItemPreview(ugc, handle, preview);
            bool okTags = SetTags(ugc, handle, tags);
            bool okVis = SteamAPI_ISteamUGC_SetItemVisibility(ugc, handle, 0); // 0 = public
            Console.WriteLine("SetItemPreview = " + okPreview + " ; SetItemTags = " + okTags + " ; SetItemVisibility(public) = " + okVis);

            ulong call = SteamAPI_ISteamUGC_SubmitItemUpdate(ugc, handle, changenote);
            Console.WriteLine("SubmitItemUpdate call = " + call);
            if (call == 0)
            {
                Console.WriteLine("SubmitItemUpdate FAILED immediately.");
                return 4;
            }

            bool completed = false;
            bool ioFailed = false;
            var deadline = DateTime.UtcNow.AddMinutes(5);
            while (DateTime.UtcNow < deadline)
            {
                SteamAPI_RunCallbacks();
                SteamAPI_ISteamUtils_IsAPICallCompleted(utils, call, out ioFailed);
                int status = SteamAPI_ISteamUGC_GetItemUpdateProgress(ugc, handle, out ulong doneBytes, out ulong totalBytes);
                Console.WriteLine("  status=" + status + "  " + doneBytes + "/" + totalBytes + " bytes");
                if (SteamAPI_ISteamUtils_IsAPICallCompleted(utils, call, out ioFailed))
                {
                    completed = true;
                    break;
                }
                Thread.Sleep(500);
            }

            Console.WriteLine();
            if (!completed)
            {
                Console.WriteLine("TIMEOUT waiting for the item update to finish.");
                return 5;
            }

            // try to read SubmitItemUpdateResult_t (brute-force the callback id, size 16)
            bool gotResult = false;
            IntPtr buf = Marshal.AllocHGlobal(64);
            try
            {
                for (int id = 3300; id <= 3500 && !gotResult; id++)
                {
                    bool failed;
                    if (SteamAPI_ISteamUtils_GetAPICallResult(utils, call, buf, 16, id, out failed))
                    {
                        int res = Marshal.ReadInt32(buf, 0);
                        bool legal = Marshal.ReadByte(buf, 4) != 0;
                        ulong pfid = (ulong)Marshal.ReadInt64(buf, 8);
                        Console.WriteLine("result: callbackID=" + id + " EResult=" + res + " needsLegalAgreement=" + legal + " item=" + pfid);
                        gotResult = true;
                        exit = res == 1 ? 0 : 6;
                        if (legal) Console.WriteLine("!! Open the item page in a browser and accept the Workshop legal agreement, then run again.");
                    }
                }
                if (!gotResult) Console.WriteLine("(could not read result payload; IO failed = " + ioFailed + ") - check the item page)");
            }
            finally { Marshal.FreeHGlobal(buf); }

            Console.WriteLine();
            Console.WriteLine("Done. Check the workshop page: cover image, tags and visibility should now be set.");
            return exit;
        }
        finally
        {
            SteamAPI_Shutdown();
        }
    }

    static bool SetTags(IntPtr ugc, ulong handle, string[] tags)
    {
        IntPtr[] ptrs = new IntPtr[tags.Length];
        try
        {
            for (int i = 0; i < tags.Length; i++)
                ptrs[i] = Marshal.StringToHGlobalAnsi(tags[i]);
            IntPtr arr = Marshal.AllocHGlobal(IntPtr.Size * tags.Length);
            try
            {
                for (int i = 0; i < tags.Length; i++)
                    Marshal.WriteIntPtr(arr, i * IntPtr.Size, ptrs[i]);
                var payload = new SteamParamStringArray_t { m_ppStrings = arr, m_nNumStrings = tags.Length };
                return SteamAPI_ISteamUGC_SetItemTags(ugc, handle, ref payload);
            }
            finally { Marshal.FreeHGlobal(arr); }
        }
        finally
        {
            foreach (var p in ptrs) if (p != IntPtr.Zero) Marshal.FreeHGlobal(p);
        }
    }
}
sealed class TeeWriter : TextWriter
{
    private readonly TextWriter _a;
    private readonly TextWriter _b;
    public TeeWriter(TextWriter a, TextWriter b) { _a = a; _b = b; }
    public override System.Text.Encoding Encoding => _a.Encoding;
    public override void Write(char value) { _a.Write(value); _b.Write(value); }
    public override void Write(string value) { _a.Write(value); _b.Write(value); }
    public override void WriteLine(string value) { _a.WriteLine(value); _b.WriteLine(value); }
    public override void Flush() { _a.Flush(); _b.Flush(); }
}
