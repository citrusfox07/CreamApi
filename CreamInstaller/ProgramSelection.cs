﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CreamInstaller;

internal enum DlcType
{
    Default = 0,
    CatalogItem = 1,
    Entitlement = 2
}

internal class ProgramSelection
{
    internal bool Enabled = false;
    internal bool Usable = true;

    internal string Id = "0";
    internal string Name = "Program";

    internal string ProductUrl = null;
    internal string IconUrl = null;
    internal string SubIconUrl = null;

    internal string Publisher = null;

    internal string RootDirectory = null;
    internal List<string> DllDirectories = null;

    internal bool IsSteam = false;

    internal readonly SortedList<string, (DlcType type, string name, string icon)> AllDlc = new();
    internal readonly SortedList<string, (DlcType type, string name, string icon)> SelectedDlc = new();
    internal readonly List<Tuple<string, string, SortedList<string, (DlcType type, string name, string icon)>>> ExtraDlc = new(); // for Paradox Launcher

    internal bool AreDllsLocked
    {
        get
        {
            foreach (string directory in DllDirectories)
            {
                directory.GetCreamApiComponents(out string api, out string api_o, out string api64, out string api64_o, out string cApi);
                if (api.IsFilePathLocked()
                    || api_o.IsFilePathLocked()
                    || api64.IsFilePathLocked()
                    || api64_o.IsFilePathLocked()
                    || cApi.IsFilePathLocked())
                    return true;
                directory.GetScreamApiComponents(out string sdk, out string sdk_o, out string sdk64, out string sdk64_o, out string sApi);
                if (sdk.IsFilePathLocked()
                    || sdk_o.IsFilePathLocked()
                    || sdk64.IsFilePathLocked()
                    || sdk64_o.IsFilePathLocked()
                    || sApi.IsFilePathLocked())
                    return true;
            }
            return false;
        }
    }

    private void Toggle(string dlcAppId, (DlcType type, string name, string icon) dlcApp, bool enabled)
    {
        if (enabled) SelectedDlc[dlcAppId] = dlcApp;
        else SelectedDlc.Remove(dlcAppId);
    }

    internal void ToggleDlc(string dlcId, bool enabled)
    {
        foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in AllDlc)
        {
            string appId = pair.Key;
            (DlcType type, string name, string icon) dlcApp = pair.Value;
            if (appId == dlcId)
            {
                Toggle(appId, dlcApp, enabled);
                break;
            }
        }
        Enabled = SelectedDlc.Any() || ExtraDlc.Any();
    }

    internal ProgramSelection() => All.Add(this);

    internal void Validate()
    {
        if (Program.IsGameBlocked(Name, RootDirectory))
        {
            All.Remove(this);
            return;
        }
        if (!Directory.Exists(RootDirectory))
        {
            All.Remove(this);
            return;
        }
        DllDirectories.RemoveAll(directory => !Directory.Exists(directory));
        if (!DllDirectories.Any()) All.Remove(this);
    }

    internal static void ValidateAll() => AllSafe.ForEach(selection => selection.Validate());

    internal static List<ProgramSelection> All = new();

    internal static List<ProgramSelection> AllSafe => All.ToList();

    internal static List<ProgramSelection> AllUsable => All.FindAll(s => s.Usable);

    internal static List<ProgramSelection> AllUsableEnabled => AllUsable.FindAll(s => s.Enabled);

    internal static ProgramSelection FromId(string gameId) => AllUsable.Find(s => s.Id == gameId);

    internal static (string gameId, (DlcType type, string name, string icon) app)? GetDlcFromId(string dlcId)
    {
        foreach (ProgramSelection selection in AllUsable)
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in selection.AllDlc)
                if (pair.Key == dlcId) return (selection.Id, pair.Value);
        return null;
    }
}
