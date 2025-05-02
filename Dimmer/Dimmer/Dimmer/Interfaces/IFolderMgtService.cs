﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;
public interface IFolderMgtService : IDisposable
{

    IObservable<IReadOnlyList<FolderModel>> AllFolders { get; }

    
    void AddFolderToPreference(string path);
    void RemoveFolderFromPreference(string path);
    void ClearAllFolders();
    void SetFolderName(string path, string name);
    void SetFolderPath(string path, string newPath);
    void SetFolderSelected(string path, bool isSelected);
    void SetFolderExpanded(string path, bool isExpanded);
    void SetFolderChecked(string path, bool isChecked);
    void OnFileRenamed(RenamedEventArgs e);
    void OnFileChanged(string fullPath);
    void OnFileDeleted(FileSystemEventArgs e);
    void OnFileCreated(FileSystemEventArgs e);
}

public partial class FolderModel:ObservableObject
{
    [ObservableProperty]
    public partial string FolderName { get; set; }
    [ObservableProperty]
    public partial string FolderPath{get;set;}
    [ObservableProperty]
    public partial bool IsSelected{get;set;}
    [ObservableProperty]
    public partial bool IsExpanded{get;set;}
    [ObservableProperty]
    public partial bool IsChecked{get;set;}
}