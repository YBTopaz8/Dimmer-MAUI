namespace Dimmer_MAUI.Utilities;
internal class DumpyDumpy
{
}


#region contentdropWindows
//private async void Content_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
//{
//    if (e.DataView != null)
//    {
//        if (e.DataView.Contains(StandardDataFormats.StorageItems))
//        {
//            var items = await e.DataView.GetStorageItemsAsync();
//            if (items.Count > 0)
//            {
//                var storageFile = items[0];/// as StorageFile
//                string filePath = storageFile.Path;

//                Debug.WriteLine($"File dropped: {filePath}");
//            }
//        }
//    }
//    Debug.WriteLine("Dropped");
//}

//private void Content_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
//{
//    Debug.WriteLine("Drag Over");
//}

//private void Content_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
//{
//    Debug.WriteLine("DragLeave");
//}

//private async void Content_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
//{

//    if (e.DataView != null)
//    {
//        if (e.DataView.Contains(StandardDataFormats.StorageItems))
//        {
//            var w = e.DataView;
//            var items = await e.DataView.GetStorageItemsAsync();
//            bool validFiles = true;

//            if (items.Count > 0)
//            {
//                foreach (var item in items)
//                {
//                    if (item is StorageFile file)
//                    {
//                        /// Check file extension
//                        string fileExtension = file.FileType.ToLower();
//                        if (fileExtension != ".mp3" && fileExtension != ".flac" &&
//                            fileExtension != ".wav" && fileExtension != ".m4a")
//                        {
//                            validFiles = false;
//                            break;  // If any invalid file is found, break the loop
//                        }
//                    }
//                }

//                if (validFiles)
//                {
//                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
//                }
//                else
//                {
//                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;  // Deny drop if file types don't match
//                }

//            }
//        }
//    }
//}

#endregion